using UnityEngine;

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
        ReadOnlyReactiveProperty<float>         DistanceReactive      { get; }
        ReadOnlyReactiveProperty<float>         SpeedReactive         { get; }

        // Exposed so CameraFollow can track world position without depending on
        // ServerCharacter/LocalCharacter directly — same dependency-inversion reason
        // CharacterAnimationDriver resolves against this interface instead of a concrete type.
        Transform CharacterTransform { get; }

        /// <summary>Requests a lane change. direction: -1 = left, +1 = right.</summary>
        void RequestLaneChange(int direction);

        /// <summary>Requests a jump action.</summary>
        void RequestJump();

        /// <summary>Requests a slide action.</summary>
        void RequestSlide();

        /// <summary>Reports a physics collision with an obstacle. Called by CollisionReporter, never by player input.</summary>
        void ReportCollision();

        /// <summary>Starts or stops forward distance accumulation. Called by GameService on run start/end.</summary>
        void SetRunning(bool isRunning);
    }
}
