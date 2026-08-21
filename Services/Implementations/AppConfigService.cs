using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.Services.Implementations
{
    public class AppConfigService : IAppConfigService
    {
        public WbsSettings WbsSettings { get; }

        public AppConfigService()
        {
            WbsSettings = CarregarWbsSettings();
        }

        private static WbsSettings CarregarWbsSettings()
        {
            try
            {
                string jsonContent = string.Empty;
                var assembly = Assembly.GetExecutingAssembly();

                // 1. Tenta carregar o JSON embutido diretamente na memória do executável (EmbeddedResource)
                foreach (string resourceName in assembly.GetManifestResourceNames())
                {
                    if (resourceName.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream != null)
                        {
                            using var reader = new StreamReader(stream);
                            jsonContent = reader.ReadToEnd();
                            break;
                        }
                    }
                }

                // 2. Fallback: Se não estiver embutido, tenta ler do AppData local
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    string appDataConfig = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "OmniDesk",
                        "appsettings.json"
                    );

                    if (File.Exists(appDataConfig))
                    {
                        jsonContent = File.ReadAllText(appDataConfig);
                    }
                }

                // 3. Fallback: Tenta ler da pasta atual
                if (string.IsNullOrWhiteSpace(jsonContent) && File.Exists("appsettings.json"))
                {
                    jsonContent = File.ReadAllText("appsettings.json");
                }

                if (!string.IsNullOrWhiteSpace(jsonContent))
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                    if (doc.RootElement.TryGetProperty("WbsSettings", out var wbsProp))
                    {
                        return JsonSerializer.Deserialize<WbsSettings>(wbsProp.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new WbsSettings();
                    }
                }
            }
            catch
            {
                // Silencioso, retorna objeto padrão
            }

            return new WbsSettings();
        }
    }
}
