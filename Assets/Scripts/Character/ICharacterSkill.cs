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
}
