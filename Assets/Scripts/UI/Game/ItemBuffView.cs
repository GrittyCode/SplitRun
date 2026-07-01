using UnityEngine;

using R3;
using VContainer;

using SplitRun.Constants;
using SplitRun.Item;

namespace SplitRun.UI.Game
{
    // Bottom-left active-item buffs. Instantiates an indicator under itself on first use and reuses
    // it, so the scene holds an empty layout container and adding buffs needs no re-authoring.
    public class ItemBuffView : MonoBehaviour
    {
        [SerializeField] private TimedIndicator _indicatorPrefab;

        [Inject] private ItemService    _itemService;
        [Inject] private HudIconLibrary _icons;

        private TimedIndicator _magnetIndicator;

        private void Start()
        {
            _itemService.MagnetRemaining
                .Subscribe(remaining => OnMagnetChanged(remaining))
                .AddTo(this);
        }

        private void OnMagnetChanged(float remaining)
        {
            if (remaining <= 0f)
            {
                if (_magnetIndicator) _magnetIndicator.SetVisible(false);
                return;
            }

            if (!_magnetIndicator)
                _magnetIndicator = CreateIndicator(_icons.IconFor(ItemType.Magnet));

            _magnetIndicator.SetVisible(true);
            _magnetIndicator.SetFill(remaining / ItemConstants.k_MagnetDuration);
        }

        private TimedIndicator CreateIndicator(Sprite icon)
        {
            TimedIndicator indicator = Instantiate(_indicatorPrefab, transform);
            indicator.SetIcon(icon);
            return indicator;
        }
    }
}
