using OmniDesk.Models;
using OmniDesk.Services.Abstractions;
using System.Reflection;

namespace OmniDesk.Services.Implementations
{
    public class SapService : ISapService
    {
        private static bool _configRegistrada = false;
        private static object? _sapLogonConfig = null;
        private static readonly object _lock = new();

        private static Type? _rfcConfigParametersType;
        private static Type? _rfcDestinationManagerType;
        private static Type? _sapLogonConfigType;

        public SapService()
        {
            // Inicialização sob demanda (Lazy)
        }

        private static void GarantirInicializacaoConfig()
        {
            if (!_configRegistrada)
            {
                lock (_lock)
                {
                    if (!_configRegistrada)
                    {
                        var asm = SapLoadContext.Instance.GetSapAssembly();
                        _rfcConfigParametersType = asm.GetType("SAP.Middleware.Connector.RfcConfigParameters")!;
                        _rfcDestinationManagerType = asm.GetType("SAP.Middleware.Connector.RfcDestinationManager")!;
                        _sapLogonConfigType = asm.GetType("SAP.Middleware.Connector.SapLogonIniConfiguration")!;

                        try
                        {
                            var createMethod = _sapLogonConfigType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
                            _sapLogonConfig = createMethod!.Invoke(null, null);

                            var regMethod = _rfcDestinationManagerType.GetMethod("RegisterDestinationConfiguration", BindingFlags.Public | BindingFlags.Static);
                            regMethod!.Invoke(null, new[] { _sapLogonConfig });
                            _configRegistrada = true;
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException?.GetType().Name == "RfcInvalidStateException" && ex.InnerException.Message.Contains("instantiated only once"))
                        {
                            // Já inicializado no processo
                            _configRegistrada = true;
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException?.GetType().Name == "RfcInvalidStateException" && ex.InnerException.Message.Contains("Unable to find saplogon file"))
                        {
                            // saplogon.ini / SAPUILandscape.xml não existe nesta máquina
                            _sapLogonConfig = null;
                            _configRegistrada = true;
                        }
                        catch (Exception ex)
                        {
                            var inner = ex.InnerException ?? ex;
                            throw new InvalidOperationException($"Falha ao inicializar o conector SAP: {inner.Message}", inner);
                        }
                    }
                }
            }
        }

        public IEnumerable<string> ObterAmbientesDisponiveis()
        {
            GarantirInicializacaoConfig();
            if (_sapLogonConfig != null && _sapLogonConfigType != null)
            {
                var getEntriesMethod = _sapLogonConfigType.GetMethod("GetEntries", BindingFlags.Public | BindingFlags.Instance);
                if (getEntriesMethod?.Invoke(_sapLogonConfig, null) is string[] entries && entries.Length > 0)
                {
                    return entries.OrderBy(e => e).ToList();
                }
            }

            // Fallback padrão quando o saplogon.ini / Landscape não for encontrado na máquina
            return new List<string>
            {
                "EP0 - ECC Produção",
                "-- Nenhuma conexão local encontrada --"
            };
        }

        public async Task<List<string>> ObterAmbientesDisponiveisAsync()
        {
            return await Task.Run(() => ObterAmbientesDisponiveis().ToList());
        }

        public async Task<List<Grupos>> ObterPerfisDoUsuarioAsync(string nomeUsuario, SapUserSession sessao)
        {
            if (string.IsNullOrWhiteSpace(nomeUsuario))
                throw new ArgumentException("O nome do usuário não pode ser vazio.", nameof(nomeUsuario));

            if (sessao == null)
                throw new ArgumentNullException(nameof(sessao), "A sessão do SAP não foi informada.");

            return await Task.Run(() =>
            {
                GarantirInicializacaoConfig();

                if (_sapLogonConfig == null || _sapLogonConfigType == null || _rfcDestinationManagerType == null || _rfcConfigParametersType == null)
                    throw new InvalidOperationException("O conector SAP não está inicializado corretamente.");

                // Cria e popula os parâmetros de credenciais adicionais
                var parms = Activator.CreateInstance(_rfcConfigParametersType)!;
                var indexer = _rfcConfigParametersType.GetProperty("Item", new[] { typeof(string) });

                indexer?.SetValue(parms, sessao.Client, new object[] { "CLIENT" });
                indexer?.SetValue(parms, sessao.User, new object[] { "USER" });
                indexer?.SetValue(parms, sessao.Password, new object[] { "PASSWD" });
                indexer?.SetValue(parms, sessao.Language, new object[] { "LANG" });

                var setAddtlMethod = _sapLogonConfigType.GetMethod("SetAdditionalParameters", new[] { typeof(string), _rfcConfigParametersType });
                setAddtlMethod?.Invoke(_sapLogonConfig, new[] { sessao.DestinationName, parms });

                try
                {
                    var getDestMethod = _rfcDestinationManagerType.GetMethod("GetDestination", new[] { typeof(string) });
                    var destination = getDestMethod!.Invoke(null, new object[] { sessao.DestinationName });

                    var destType = destination!.GetType();
                    var repoProp = destType.GetProperty("Repository");
                    var repository = repoProp!.GetValue(destination);

                    var repoType = repository!.GetType();
                    var createFuncMethod = repoType.GetMethod("CreateFunction", new[] { typeof(string) });
                    var bapi = createFuncMethod!.Invoke(repository, new object[] { "BAPI_USER_GET_DETAIL" });

                    var bapiType = bapi!.GetType();
                    var setValueMethod = bapiType.GetMethod("SetValue", new[] { typeof(string), typeof(string) });
                    setValueMethod!.Invoke(bapi, new object[] { "USERNAME", nomeUsuario.Trim().ToUpper() });

                    var invokeMethod = bapiType.GetMethod("Invoke", new[] { destType });
                    invokeMethod!.Invoke(bapi, new[] { destination });

                    var getTableMethod = bapiType.GetMethod("GetTable", new[] { typeof(string) });
                    var activityGroups = getTableMethod!.Invoke(bapi, new object[] { "ACTIVITYGROUPS" });

                    var tableType = activityGroups!.GetType();
                    var rowCountProp = tableType.GetProperty("RowCount");
                    int rowCount = (int)rowCountProp!.GetValue(activityGroups)!;

                    var currentIndexProp = tableType.GetProperty("CurrentIndex");
                    var getStringMethod = tableType.GetMethod("GetString", new[] { typeof(string) });

                    var lista = new List<Grupos>();
                    for (int i = 0; i < rowCount; i++)
                    {
                        currentIndexProp!.SetValue(activityGroups, i);
                        string agrName = (string)getStringMethod!.Invoke(activityGroups, new object[] { "AGR_NAME" })!;
                        string agrText = (string)getStringMethod!.Invoke(activityGroups, new object[] { "AGR_TEXT" })!;

                        lista.Add(new Grupos
                        {
                            Nome = agrName,
                            Descricao = agrText,
                            Origem = "SAP"
                        });
                    }

                    return lista.OrderBy(g => g.Nome).ToList();
                }
                catch (TargetInvocationException ex)
                {
                    var inner = ex.InnerException ?? ex;
                    string exTypeName = inner.GetType().Name;

                    if (exTypeName.Contains("RfcLogonException"))
                    {
                        throw new UnauthorizedAccessException(
                            $"Falha de autenticação no SAP (Ambiente: '{sessao.DestinationName}', Mandante: '{sessao.Client}'). Verifique se o usuário ou a senha estão corretos.\nDetalhes: {inner.Message}", inner);
                    }
                    if (exTypeName.Contains("RfcCommunicationException"))
                    {
                        throw new InvalidOperationException(
                            $"Falha de comunicação com o servidor SAP '{sessao.DestinationName}'. Verifique a conexão de rede ou VPN.\nDetalhes: {inner.Message}", inner);
                    }
                    if (exTypeName.Contains("RfcAbapException"))
                    {
                        var keyProp = inner.GetType().GetProperty("Key");
                        string key = (string?)keyProp?.GetValue(inner) ?? string.Empty;
                        if (key.Equals("USER_NOT_FOUND", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new KeyNotFoundException($"O usuário '{nomeUsuario}' não foi encontrado no SAP.", inner);
                        }
                        throw new InvalidOperationException($"Erro retornado pela BAPI do SAP ({key}): {inner.Message}", inner);
                    }

                    throw new InvalidOperationException($"Erro no SAP: {inner.Message}", inner);
                }
            });
        }
    }
}