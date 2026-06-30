namespace SplitRun.Character
{
    // Write surface CharacterCore mutates, so the core stays agnostic to whether state
    // lives in a NetworkVariable (ServerCharacter) or a ReactiveProperty (LocalCharacter).
    public interface ICharacterState
    {
        int           Lane     { get; set; }
        int           Hp       { get; set; }
        float         Distance { get; set; }
        float         Speed    { get; set; }
        SkillState    Skill    { get; set; }
        VerticalState Vertical { get; set; }
    }
}
