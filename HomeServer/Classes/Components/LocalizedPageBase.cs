using HomeServer.Classes.Services;
using Microsoft.AspNetCore.Components;

namespace HomeServer.Classes.Components
{
    public abstract class LocalizedPageBase : ComponentBase, IDisposable
    {
        [Inject]
        private LocalizationService _loc { get; set; } = default!;

        protected override void OnInitialized()
        {
            _loc.OnLanguageChanged += OnLangChanged;
        }

        private void OnLangChanged() => InvokeAsync(StateHasChanged);

        public virtual void Dispose()
        {
            _loc.OnLanguageChanged -= OnLangChanged;
        }
    }
}
