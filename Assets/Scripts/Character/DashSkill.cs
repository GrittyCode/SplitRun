using SplitRun.Constants;

namespace SplitRun.Character
{
    public sealed class DashSkill : ICharacterSkill
    {
        private readonly CharacterCore _core;

        private SkillState _state = SkillState.Ready;
        private float      _timeRemaining;

        public DashSkill(CharacterCore core) => _core = core;

        public SkillType  Type  => SkillType.Dash;
        public SkillState State => _state;

        public void Activate()
        {
            if (_state != SkillState.Ready) return;

            _state         = SkillState.Active;
            _timeRemaining = SkillConstants.k_DashDuration;
            _core.SetInvincible(true);
            _core.SetSpeedMultiplier(SkillConstants.k_DashSpeedMultiplier);
        }

        public void Tick(float deltaTime)
        {
            if (_state == SkillState.Ready) return;

            _timeRemaining -= deltaTime;
            if (_timeRemaining <= 0f)
                AdvanceTimedState();
        }

        public void OnDamageBlocked() { }

        private void AdvanceTimedState()
        {
            switch (_state)
            {
                case SkillState.Active:
                    _core.SetInvincible(false);
                    _core.SetSpeedMultiplier(1f);
                    _state         = SkillState.Cooldown;
                    _timeRemaining = SkillConstants.k_DashCooldownDuration;
                    break;
                case SkillState.Cooldown:
                    _state = SkillState.Ready;
                    break;
            }
        }
    }
}
