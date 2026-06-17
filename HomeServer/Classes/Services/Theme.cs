using System;

namespace HomeServer.Classes.Services
{
    public class Theme
    {
        // O tema padrão é o "default" (vazio, usa o :root do CSS)
        public string CurrentThemeClass { get; private set; } = "";

        public event Action? OnThemeChanged;

        public void SetTheme(string themeName)
        {
            CurrentThemeClass = (string.IsNullOrWhiteSpace(themeName) || themeName == "default") ? "" : $"theme-{themeName}";

            // Avisa os componentes para se atualizarem
            OnThemeChanged?.Invoke();
        }
    }
}
