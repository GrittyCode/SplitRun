using R3;

namespace SplitRun.Character
{
    // Implemented by ServerCharacter (networked) and LocalCharacter (local prototype).
    public interface ICharacter
    {
        ReadOnlyReactiveProperty<int>           LaneReactive          { get; }
        ReadOnlyReactiveProperty<int>           HpReactive            { get; }
        ReadOnlyReactiveProperty<SkillState>    SkillStateReactive    { get; }
        ReadOnlyReactiveProperty<VerticalState> VerticalStateReactive { get; }

        /// <summary>Requests a lane change. direction: -1 = left, +1 = right.</summary>
        void RequestLaneChange(int direction);

        /// <summary>Requests a jump action.</summary>
        void RequestJump();

        /// <summary>Requests a slide action.</summary>
        void RequestSlide();
    }
}
