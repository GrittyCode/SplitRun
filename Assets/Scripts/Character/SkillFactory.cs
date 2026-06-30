namespace SplitRun.Character
{
    public static class SkillFactory
    {
        public static ICharacterSkill Create(CharacterType characterType, CharacterCore core)
        {
            return characterType switch
            {
                CharacterType.Shield => new ShieldSkill(core),
                CharacterType.Dash   => new DashSkill(core),
                _                    => new NullSkill(),
            };
        }
    }
}
