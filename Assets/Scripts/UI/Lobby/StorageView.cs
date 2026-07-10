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

        private readonly List<(CharacterType Type, ShopCharacterEntry Entry)> _visibleCharacters =
            new List<(CharacterType, ShopCharacterEntry)>();

        private readonly List<(HatType Type, ShopHatEntry Entry)> _visibleHats =
            new List<(HatType, ShopHatEntry)>();

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
            foreach ((CharacterType type, ShopCharacterEntry entry) in _catalog.Characters)
            {
                if (entry == null || !_playerData.IsCharacterUnlocked(type)) continue;

                _visibleCharacters.Add((type, entry));
            }

            SetCardCount(_visibleCharacters.Count);
            for (int i = 0; i < _visibleCharacters.Count; i++)
            {
                (CharacterType type, ShopCharacterEntry entry) = _visibleCharacters[i];
                SetupCard(CardAt(i), entry.Icon, entry.DisplayName, _playerData.SelectedCharacter.CurrentValue == type);
            }
        }

        // HatType.None is enum index 0, so the take-off slot always leads the grid.
        private void RebuildHats()
        {
            _visibleHats.Clear();
            foreach ((HatType type, ShopHatEntry entry) in _catalog.Hats)
            {
                if (entry == null || !_playerData.IsHatUnlocked(type)) continue;

                _visibleHats.Add((type, entry));
            }

            SetCardCount(_visibleHats.Count);
            for (int i = 0; i < _visibleHats.Count; i++)
            {
                (HatType type, ShopHatEntry entry) = _visibleHats[i];
                SetupCard(CardAt(i), entry.Icon, entry.DisplayName, _playerData.SelectedHat.CurrentValue == type);
            }
        }

        private static void SetupCard(CustomizationCardView card, Sprite icon, string displayName, bool isEquipped)
        {
            if (isEquipped)
                card.SetupEquipped(icon, displayName);
            else
                card.SetupOwned(icon, displayName);
        }
    }
}
