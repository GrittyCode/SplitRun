using UnityEngine;

using R3;

namespace SplitRun.Character
{
    // Lives on the Shield/Dash prefabs only; Default carries neither this driver nor a VFX child.
    public class CharacterSkillVfxDriver : MonoBehaviour
    {
        [SerializeField] private GameObject _skillVfx;

        private ICharacter _character;

        private void Start()
        {
            _character = GetComponent<ICharacter>();

            _character.SkillStateReactive
                .Subscribe(state => SetVfxActive(state == SkillState.Active))
                .AddTo(this);
        }

        private void SetVfxActive(bool isActive)
        {
            if (_skillVfx)
                _skillVfx.SetActive(isActive);
        }
    }
}
