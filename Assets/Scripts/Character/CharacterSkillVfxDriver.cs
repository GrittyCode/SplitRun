using UnityEngine;

using R3;

namespace SplitRun.Character
{
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
