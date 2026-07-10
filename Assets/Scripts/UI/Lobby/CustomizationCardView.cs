using System.Globalization;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

using R3;

namespace SplitRun.UI.Lobby
{
    public class CustomizationCardView : MonoBehaviour
    {
        private const string k_EquippedLabel = "EQUIPPED";
        private const string k_OwnedLabel    = "OWNED";

        [Header("Content")]
        [SerializeField] private Button   _button;
        [SerializeField] private Image    _icon;
        [SerializeField] private TMP_Text _nameText;

        [Header("State")]
        [SerializeField] private GameObject _priceRoot;
        [SerializeField] private TMP_Text   _priceText;
        [SerializeField] private TMP_Text   _stateText;
        [SerializeField] private GameObject _equippedFrame;

        [Header("Price Colors")]
        [SerializeField] private Color _affordableColor = Color.white;
        [SerializeField] private Color _shortfallColor  = new Color(1f, 0.35f, 0.35f);

        public Observable<Unit> OnClicked => _button.OnClickWithSfx();

        public void SetupPurchasable(Sprite icon, string displayName, int price, bool isAffordable)
        {
            SetContent(icon, displayName);
            _priceRoot.SetActive(true);
            _stateText.gameObject.SetActive(false);
            _equippedFrame.SetActive(false);
            _priceText.text  = price.ToString("N0", CultureInfo.InvariantCulture);
            _priceText.color = isAffordable ? _affordableColor : _shortfallColor;
        }

        public void SetupOwned(Sprite icon, string displayName)
        {
            SetContent(icon, displayName);
            _priceRoot.SetActive(false);
            _stateText.gameObject.SetActive(true);
            _stateText.text = k_OwnedLabel;
            _equippedFrame.SetActive(false);
        }

        public void SetupEquipped(Sprite icon, string displayName)
        {
            SetContent(icon, displayName);
            _priceRoot.SetActive(false);
            _stateText.gameObject.SetActive(true);
            _stateText.text = k_EquippedLabel;
            _equippedFrame.SetActive(true);
        }

        private void SetContent(Sprite icon, string displayName)
        {
            _icon.sprite   = icon;
            _icon.enabled  = icon;
            _nameText.text = displayName;
        }
    }
}
