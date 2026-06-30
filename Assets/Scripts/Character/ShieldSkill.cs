using SplitRun.Constants;

namespace SplitRun.Character
{
    public sealed class ShieldSkill : ICharacterSkill
    {
        private readonly CharacterCore _core;

        private SkillState _state = SkillState.Ready;
        private float      _cooldownRemaining;

        public ShieldSkill(CharacterCore core) => _core = core;

        public SkillType  Type  => SkillType.Shield;
        public SkillState State => _state;

        public void Activate()
        {
            if (_state != SkillState.Ready) return;

            _state = SkillState.Active;
            _core.SetInvincible(true);
        }

        public void Tick(float deltaTime)
        {
            if (_state != SkillState.Cooldown) return;

            _cooldownRemaining -= deltaTime;
            if (_cooldownRemaining <= 0f)
                _state = SkillState.Ready;
        }

        public void OnDamageBlocked()
        {
            if (_state != SkillState.Active) return;

            _core.SetInvincible(false);
            _state             = SkillState.Cooldown;
            _cooldownRemaining = SkillConstants.k_ShieldCooldownDuration;
        }
    }
}
