namespace SplitRun.Character
{
    public enum CharacterType
    {
        Default,
        Shield,
        Dash,
    }

    // Values mirror Mg3D_Hats asset names — adding a hat is one value here plus one ShopCatalog entry.
    public enum HatType
    {
        None,
        MinerHat,
        Crown,
        MagicianHat,
    }

    public enum SkillType
    {
        None,
        Shield,
        Dash,
    }

    public enum SkillState
    {
        Ready,
        Active,
        Cooldown,
    }

    public enum VerticalState
    {
        Ground,
        Jumping,
        Sliding,
    }
}
