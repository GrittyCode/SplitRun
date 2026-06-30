namespace SplitRun.Character
{
    public sealed class NullSkill : ICharacterSkill
    {
        public SkillType  Type  => SkillType.None;
        public SkillState State => SkillState.Ready;

        public void Activate() { }

        public void Tick(float deltaTime) { }

        public void OnDamageBlocked() { }
    }
}
