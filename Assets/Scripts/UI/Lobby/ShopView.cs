using System.Collections.Generic;
using System.Globalization;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

using R3;
using VContainer;

using SplitRun.Character;
using SplitRun.Data;

namespace SplitRun.UI.Lobby
{
    // Sells only unowned items; owned content is equipped from Storage.
    public class ShopView : CustomizationGridView
    {
        [Header("Purchase Popup")]
        [SerializeField] private GameObject _purchasePopup;
        [SerializeField] private TMP_Text   _popupItemText;
        [SerializeField] private GameObject _insufficientLabel;
        [SerializeField] private Button     _buyButton;
        [SerializeField] private Button     _popupCloseButton;

        [Inject] private PlayerDataService  _playerData;
        [Inject] private ShopCatalog        _catalog;
        [Inject] private CharacterStageView _stage;

        private readonly List<(CharacterType Type, ShopCharacterEntry Entry)> _visibleCharacters =
            new List<(CharacterType, ShopCharacterEntry)>();

        private readonly List<(HatType Type, ShopHatEntry Entry)> _visibleHats =
            new List<(HatType, ShopHatEntry)>();

        private int _pendingIndex = -1;

        protected override void Start()
        {
            base.Start();

            _buyButton.OnClickAsObservable().Subscribe(_ => Purchase()).AddTo(this);

            // Closing the popup without buying reverts the try-on to the persisted selection.
            _popupCloseButton.OnClickAsObservable().Subscribe(_ => ClearTryOn()).AddTo(this);
        }

        private void OnDisable() => ClearTryOn();

        protected override void OnTabChanged() => ClearTryOn();

        protected override void Rebuild()
        {
            if (Tab == CustomizationTab.Character)
                RebuildCharacters();
            else
                RebuildHats();
        }

        protected override void OnCardClicked(int index)
        {
            if (Tab == CustomizationTab.Character)
                HandleCharacterClick(index);
            else
                HandleHatClick(index);
        }

        private void RebuildCharacters()
        {
            _visibleCharacters.Clear();
            foreach ((CharacterType type, ShopCharacterEntry entry) in _catalog.Characters)
            {
                if (entry == null || _playerData.IsCharacterUnlocked(type)) continue;

                _visibleCharacters.Add((type, entry));
            }

            SetCardCount(_visibleCharacters.Count);
            for (int i = 0; i < _visibleCharacters.Count; i++)
            {
                ShopCharacterEntry entry = _visibleCharacters[i].Entry;
                CardAt(i).SetupPurchasable(entry.Icon, entry.DisplayName, entry.Price, IsAffordable(entry.Price));
            }
        }

        // IsHatUnlocked returns true for HatType.None, so the bare-head slot never reaches the shop grid.
        private void RebuildHats()
        {
            _visibleHats.Clear();
            foreach ((HatType type, ShopHatEntry entry) in _catalog.Hats)
            {
                if (entry == null || _playerData.IsHatUnlocked(type)) continue;

                _visibleHats.Add((type, entry));
            }

            SetCardCount(_visibleHats.Count);
            for (int i = 0; i < _visibleHats.Count; i++)
            {
                ShopHatEntry entry = _visibleHats[i].Entry;
                CardAt(i).SetupPurchasable(entry.Icon, entry.DisplayName, entry.Price, IsAffordable(entry.Price));
            }
        }

        private void HandleCharacterClick(int index)
        {
            if (index >= _visibleCharacters.Count)
                return;

            (CharacterType type, ShopCharacterEntry entry) = _visibleCharacters[index];
            _stage.PreviewCharacter(type);
            ShowPurchasePopup(index, entry.DisplayName, entry.Price);
        }

        private void HandleHatClick(int index)
        {
            if (index >= _visibleHats.Count)
                return;

            (HatType type, ShopHatEntry entry) = _visibleHats[index];
            _stage.PreviewHat(type);
            ShowPurchasePopup(index, entry.DisplayName, entry.Price);
        }

        private void ClearTryOn()
        {
            _stage.ClearPreview();
            _pendingIndex = -1;
            _purchasePopup.SetActive(false);
        }

        private void ShowPurchasePopup(int index, string displayName, int price)
        {
            _pendingIndex = index;

            bool isAffordable = IsAffordable(price);
            _popupItemText.text = $"{displayName} — {price.ToString("N0", CultureInfo.InvariantCulture)}";
            _insufficientLabel.SetActive(!isAffordable);
            _buyButton.gameObject.SetActive(isAffordable);
            _purchasePopup.SetActive(true);
        }

        private void Purchase()
        {
            if (_pendingIndex < 0)
                return;

            bool isPurchased = Tab == CustomizationTab.Character
                ? PurchaseCharacter(_pendingIndex)
                : PurchaseHat(_pendingIndex);

            if (!isPurchased)
                return;

            // A bought item leaves the shop grid — clear the try-on, then rebuild without it.
            ClearTryOn();
            Rebuild();
        }

        private bool PurchaseCharacter(int index)
        {
            (CharacterType type, ShopCharacterEntry entry) = _visibleCharacters[index];
            return _playerData.TryPurchaseCharacter(type, entry.Price);
        }

        private bool PurchaseHat(int index)
        {
            (HatType type, ShopHatEntry entry) = _visibleHats[index];
            return _playerData.TryPurchaseHat(type, entry.Price);
        }

        private bool IsAffordable(int price) => _playerData.Coins.CurrentValue >= price;
    }
}
