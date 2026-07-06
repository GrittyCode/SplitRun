using UnityEngine;

using R3;
using VContainer;

using SplitRun.Character;
using SplitRun.Data;

namespace SplitRun.UI.Lobby
{
    // Renders the persisted selection; the shop temporarily overrides it for a try-on.
    public class CharacterStageView : MonoBehaviour
    {
        [SerializeField] private Transform _stageRoot;

        [Inject] private PlayerDataService _playerData;
        [Inject] private ShopCatalog       _catalog;

        private CharacterModel _current;

        private CharacterType? _characterOverride;
        private HatType?       _hatOverride;

        private void Start()
        {
            _playerData.SelectedCharacter.Subscribe(OnCharacterSelected).AddTo(this);
            _playerData.SelectedHat.Subscribe(OnHatSelected).AddTo(this);
        }

        public void PreviewCharacter(CharacterType type)
        {
            _characterOverride = type;
            RebuildCharacter(type);
        }

        public void PreviewHat(HatType type)
        {
            _hatOverride = type;
            AttachHat(type);
        }

        /// <summary>Drops any try-on override and reverts the stage to the persisted selection.</summary>
        public void ClearPreview()
        {
            // The shop's OnDisable during scene teardown may arrive after the stage is already gone.
            if (!this || !_stageRoot)
                return;

            bool hadCharacterOverride = _characterOverride.HasValue;
            bool hadHatOverride       = _hatOverride.HasValue;

            _characterOverride = null;
            _hatOverride       = null;

            if (hadCharacterOverride)
                RebuildCharacter(_playerData.SelectedCharacter.CurrentValue);
            else if (hadHatOverride)
                AttachHat(_playerData.SelectedHat.CurrentValue);
        }

        private void OnCharacterSelected(CharacterType type)
        {
            if (_characterOverride == null)
                RebuildCharacter(type);
        }

        private void OnHatSelected(HatType type)
        {
            if (_hatOverride == null)
                AttachHat(type);
        }

        private void RebuildCharacter(CharacterType type)
        {
            if (_current)
                Destroy(_current.gameObject);

            ShopCharacterEntry entry = _catalog.FindCharacter(type);
            if (entry == null || !entry.ModelPrefab)
            {
                Debug.LogError($"[CharacterStageView] ShopCatalog has no model prefab for {type}.");
                _current = null;
                return;
            }

            _current = Instantiate(entry.ModelPrefab, _stageRoot);
            AttachHat(_hatOverride ?? _playerData.SelectedHat.CurrentValue);
        }

        private void AttachHat(HatType type)
        {
            if (!_current)
                return;

            ShopHatEntry entry = _catalog.FindHat(type);
            _current.AttachHat(entry?.HatPrefab);
        }
    }
}
