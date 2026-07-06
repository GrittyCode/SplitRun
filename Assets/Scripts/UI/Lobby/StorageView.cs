using System.Collections.Generic;

using UnityEngine;

using VContainer;

using SplitRun.Character;
using SplitRun.Data;

namespace SplitRun.UI.Lobby
{
    public class StorageView : CustomizationGridView
    {
        [Inject] private PlayerDataService _playerData;
        [Inject] private ShopCatalog       _catalog;

        private readonly List<ShopCharacterEntry> _visibleCharacters = new List<ShopCharacterEntry>();
        private readonly List<ShopHatEntry>       _visibleHats       = new List<ShopHatEntry>();

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
            {
                if (index >= _visibleCharacters.Count)
                    return;

                _playerData.SelectCharacter(_visibleCharacters[index].Type);
            }
            else
            {
                if (index >= _visibleHats.Count)
                    return;

                _playerData.SelectHat(_visibleHats[index].Type);
            }

            Rebuild();
        }

        private void RebuildCharacters()
        {
            _visibleCharacters.Clear();
            foreach (ShopCharacterEntry entry in _catalog.Characters)
            {
                if (_playerData.IsCharacterUnlocked(entry.Type))
                    _visibleCharacters.Add(entry);
            }

            SetCardCount(_visibleCharacters.Count);
            for (int i = 0; i < _visibleCharacters.Count; i++)
                SetupCharacterCard(CardAt(i), _visibleCharacters[i]);
        }

        // The None entry leads the hat grid so the worn hat can always be taken off.
        private void RebuildHats()
        {
            _visibleHats.Clear();

            ShopHatEntry noneEntry = _catalog.FindHat(HatType.None);
            if (noneEntry != null)
                _visibleHats.Add(noneEntry);
            else
                Debug.LogWarning("[StorageView] ShopCatalog has no None hat entry — unequipping is unavailable.");

            foreach (ShopHatEntry entry in _catalog.Hats)
            {
                if (entry.Type != HatType.None && _playerData.IsHatUnlocked(entry.Type))
                    _visibleHats.Add(entry);
            }

            SetCardCount(_visibleHats.Count);
            for (int i = 0; i < _visibleHats.Count; i++)
                SetupHatCard(CardAt(i), _visibleHats[i]);
        }

        private void SetupCharacterCard(CustomizationCardView card, ShopCharacterEntry entry)
        {
            if (_playerData.SelectedCharacter.CurrentValue == entry.Type)
                card.SetupEquipped(entry.Icon, entry.DisplayName);
            else
                card.SetupOwned(entry.Icon, entry.DisplayName);
        }

        private void SetupHatCard(CustomizationCardView card, ShopHatEntry entry)
        {
            if (_playerData.SelectedHat.CurrentValue == entry.Type)
                card.SetupEquipped(entry.Icon, entry.DisplayName);
            else
                card.SetupOwned(entry.Icon, entry.DisplayName);
        }
    }
}
