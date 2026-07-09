using SplitRun.Constants;

namespace SplitRun.Character
{
    public interface ICharacterSkill
    {
        SkillType  Type  { get; }
        SkillState State { get; }

        /// <summary>Triggers the skill if Ready; otherwise a no-op.</summary>
        void Activate();

        /// <summary>Advances active/cooldown timers and any per-frame effect.</summary>
        void Tick(float deltaTime);

        /// <summary>Called when the character's invincibility absorbs a hit. Most skills no-op.</summary>
        void OnDamageBlocked();
    }

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
            _cooldownRemaining = CharacterConstants.k_ShieldCooldownDuration;
        }
    }

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
            _timeRemaining = CharacterConstants.k_DashDuration;
            _core.SetInvincible(true);
            _core.SetSpeedMultiplier(CharacterConstants.k_DashSpeedMultiplier);
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
                    _timeRemaining = CharacterConstants.k_DashCooldownDuration;
                    break;
                case SkillState.Cooldown:
                    _state = SkillState.Ready;
                    break;
            }
        }
    }

    // Removes the null branch from CharacterCore's per-frame Tick.
    public sealed class NullSkill : ICharacterSkill
    {
        public SkillType  Type  => SkillType.None;
        public SkillState State => SkillState.Ready;

        public void Activate() { }

        public void Tick(float deltaTime) { }

        public void OnDamageBlocked() { }
    }
}
