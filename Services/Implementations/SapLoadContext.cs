using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace OmniDesk.Services.Implementations
{
    public class SapLoadContext : AssemblyLoadContext
    {
        private static SapLoadContext? _instance;
        private static readonly object _lock = new();
        private Assembly? _sapAssembly;

        public static SapLoadContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new SapLoadContext();
                    }
                }
                return _instance;
            }
        }

        public SapLoadContext() : base("SapContext", isCollectible: false) { }

        public Assembly GetSapAssembly()
        {
            if (_sapAssembly == null)
            {
                lock (_lock)
                {
                    if (_sapAssembly == null)
                    {
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        string sapncoPath = Path.Combine(baseDir, "sapnco.dll");
                        string utilsPath = Path.Combine(baseDir, "sapnco_utils.dll");

                        if (!File.Exists(sapncoPath))
                        {
                            // Tenta no diretório atual de trabalho se não encontrado no BaseDirectory
                            string cwdSapnco = Path.Combine(Environment.CurrentDirectory, "sapnco.dll");
                            if (File.Exists(cwdSapnco))
                            {
                                sapncoPath = cwdSapnco;
                                utilsPath = Path.Combine(Environment.CurrentDirectory, "sapnco_utils.dll");
                            }
                        }

                        if (File.Exists(utilsPath))
                        {
                            var utilsAsm = LoadFromAssemblyPath(utilsPath);
                            ConfigurarLogsSap(utilsAsm);
                        }

                        _sapAssembly = LoadFromAssemblyPath(sapncoPath);
                    }
                }
            }
            return _sapAssembly;
        }

        private static void ConfigurarLogsSap(Assembly utilsAsm)
        {
            try
            {
                // Define a pasta de logs em %LOCALAPPDATA%\OmniDesk\Logs
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logsDir = Path.Combine(appData, "OmniDesk", "Logs");
                Directory.CreateDirectory(logsDir);

                var traceType = utilsAsm.GetType("SAP.Middleware.Connector.RfcTrace");
                if (traceType != null)
                {
                    // Redireciona o diretório de trace do SAP NCo para a pasta de Logs dedicada
                    var traceDirProp = traceType.GetProperty("TraceDirectory", BindingFlags.Public | BindingFlags.Static);
                    traceDirProp?.SetValue(null, logsDir);
                }

                // Remove qualquer log residual que tenha ficado na pasta do executável
                string appDirLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dev_nco_rfc.log");
                if (File.Exists(appDirLog))
                {
                    try { File.Delete(appDirLog); } catch { }
                }

                string cwdLog = Path.Combine(Environment.CurrentDirectory, "dev_nco_rfc.log");
                if (File.Exists(cwdLog) && !cwdLog.Equals(appDirLog, StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(cwdLog); } catch { }
                }
            }
            catch
            {
                // Silencioso se não for possível configurar o trace
            }
        }

        protected override Assembly? Load(AssemblyName requestedAn)
        {
            string shortName = requestedAn.Name ?? string.Empty;

            if (shortName == "System.ServiceModel")
            {
                string code = @"
                    namespace System.ServiceModel
                    {
                        public interface IExtension<T> { }
                        public interface IExtensionCollection<T>
                        {
                            E Find<E>();
                        }
                        public class ServiceHostBase
                        {
                            public virtual IExtensionCollection<ServiceHostBase> Extensions => null;
                        }
                        public class OperationContext
                        {
                            public static OperationContext Current => null;
                            public ServiceHostBase Host => null;
                            public IExtensionCollection<OperationContext> Extensions => null;
                        }
                    }
                    namespace System.ServiceModel.Activation
                    {
                        public class VirtualPathExtension : IExtension<ServiceHostBase>
                        {
                            public string VirtualPath => null;
                        }
                    }";
                return CompileToAssembly(shortName, code);
            }
            else if (shortName == "System.Web")
            {
                string code = @"
                    namespace System.Web
                    {
                        public class HttpContext
                        {
                            public static HttpContext Current => null;
                            public SessionState.HttpSessionState Session => null;
                            public HttpApplication ApplicationInstance => null;
                        }
                        public class HttpApplication { }
                        namespace SessionState
                        {
                            public class HttpSessionState { }
                        }
                        namespace Configuration
                        {
                            public class WebConfigurationManager
                            {
                                public static System.Configuration.Configuration OpenWebConfiguration(string path) => null;
                            }
                        }
                    }";
                return CompileToAssembly(shortName, code);
            }
            else if (shortName == "System.Management.Instrumentation")
            {
                string code = @"
                    namespace System.Management.Instrumentation
                    {
                        public class InstrumentationManager { }
                        public class ManagementQualifierAttribute : System.Attribute { }
                    }";
                return CompileToAssembly(shortName, code);
            }

            return null;
        }

        private Assembly CompileToAssembly(string assemblyName, string sourceCode)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Configuration.Configuration).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
            };

            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    cryptoKeyFile: null));

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);
            if (!emitResult.Success)
            {
                string errors = string.Join("\n", emitResult.Diagnostics.Select(d => d.ToString()));
                throw new InvalidOperationException($"Erro ao compilar stub de compatibilidade do {assemblyName}:\n{errors}");
            }

            ms.Position = 0;
            return LoadFromStream(ms);
        }
    }
}
