using UnityEngine;

using R3;
using VContainer;

using SplitRun.Character;
using SplitRun.Constants;
using SplitRun.Data;

namespace SplitRun.UI.Lobby
{
    // Renders the persisted selection; the shop temporarily overrides it for a try-on.
    // The stage runs a presentation-only controller, never the gameplay AC_Character.
    public class CharacterStageView : MonoBehaviour
    {
        [SerializeField] private Transform                 _stageRoot;
        [SerializeField] private RuntimeAnimatorController _stageController;

        [Inject] private PlayerDataService _playerData;
        [Inject] private ShopCatalog       _catalog;

        private static readonly int s_roarHash = Animator.StringToHash(CharacterConstants.k_TriggerRoar);

        private CharacterModel _current;
        private Animator       _animator;

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
            Roar();
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
            {
                RebuildCharacter(_playerData.SelectedCharacter.CurrentValue);
            }
            else if (hadHatOverride)
            {
                AttachHat(_playerData.SelectedHat.CurrentValue);
                Roar();
            }
        }

        private void OnCharacterSelected(CharacterType type)
        {
            if (_characterOverride == null)
                RebuildCharacter(type);
        }

        private void OnHatSelected(HatType type)
        {
            if (_hatOverride != null)
                return;

            AttachHat(type);
            Roar();
        }

        private void RebuildCharacter(CharacterType type)
        {
            if (_current)
                Destroy(_current.gameObject);

            ShopCharacterEntry entry = _catalog.FindCharacter(type);
            if (entry == null || !entry.ModelPrefab)
            {
                Debug.LogError($"[CharacterStageView] ShopCatalog has no model prefab for {type}.");
                _current  = null;
                _animator = null;
                return;
            }

            _current  = Instantiate(entry.ModelPrefab, _stageRoot);
            _animator = _current.GetComponentInChildren<Animator>();

            if (_animator)
                _animator.runtimeAnimatorController = _stageController;

            AttachHat(_hatOverride ?? _playerData.SelectedHat.CurrentValue);
            Roar();
        }

        private void AttachHat(HatType type)
        {
            if (!_current)
                return;

            ShopHatEntry entry = _catalog.FindHat(type);
            _current.AttachHat(entry?.HatPrefab);
        }

        private void Roar()
        {
            if (_animator)
                _animator.SetTrigger(s_roarHash);
        }
    }
}
