namespace SplitRun.Character
{
    public enum CharacterType
    {
        Default = 0,
        Shield  = 1,
        Dash    = 2,
    }

    public enum HatType
    {
        None        = 0,
        MinerHat    = 1,
        Crown       = 2,
        MagicianHat = 3,
    }

    public enum SkillType
    {
        None   = 0,
        Shield = 1,
        Dash   = 2,
    }

    public enum SkillState
    {
        Ready    = 0,
        Active   = 1,
        Cooldown = 2,
    }

    public enum VerticalState
    {
        Ground  = 0,
        Jumping = 1,
        Sliding = 2,
    }
}
