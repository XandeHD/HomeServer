using System.Text.Json;
using System.Text.Json.Nodes;

namespace HomeServer.Classes.Services
{
    /// <summary>
    /// Serviço Scoped (por utilizador/circuit) que gere as traduções.
    /// Carrega o ficheiro JSON da língua ativa e expõe T() para obter strings.
    /// </summary>
    public class LocalizationService
    {
        private JsonNode? _translations;
        private string _currentLanguage = "pt";

        public string CurrentLanguage => _currentLanguage;

        public event Action? OnLanguageChanged;

        private readonly IWebHostEnvironment _env;

        public LocalizationService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Define a língua e carrega as traduções correspondentes.
        /// Chamado no arranque (MainLayout) com a língua gravada na BD.
        /// </summary>
        public void SetLanguage(string languageCode)
        {
            var supported = new[] { "pt", "en", "es" };
            _currentLanguage = supported.Contains(languageCode) ? languageCode : "pt";
            LoadTranslations();
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// Obtém uma string traduzida por caminho de pontos.
        /// Ex: T("finance.stocks.title") → "Ativos & Investimentos"
        /// </summary>
        public string T(string path, string fallback = "")
        {
            if (_translations == null) LoadTranslations();
            if (_translations == null) return fallback != "" ? fallback : path;

            var parts = path.Split('.');
            JsonNode? node = _translations;

            foreach (var part in parts)
            {
                node = node?[part];
                if (node == null) return fallback != "" ? fallback : path;
            }

            return node?.GetValue<string>() ?? (fallback != "" ? fallback : path);
        }

        private void LoadTranslations()
        {
            try
            {
                var filePath = Path.Combine(_env.ContentRootPath, "Resources", $"{_currentLanguage}.json");

                if (!File.Exists(filePath))
                {
                    // Fallback para inglês se o ficheiro não existir
                    filePath = Path.Combine(_env.ContentRootPath, "Resources", "en.json");
                }

                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    _translations = JsonNode.Parse(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalizationService] Failed to load translations: {ex.Message}");
                _translations = null;
            }
        }
    }
}
