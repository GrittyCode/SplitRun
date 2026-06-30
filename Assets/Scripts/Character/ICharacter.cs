using UnityEngine;

using R3;

namespace SplitRun.Character
{
    public interface ICharacter
    {
        ReadOnlyReactiveProperty<int>           LaneReactive          { get; }
        ReadOnlyReactiveProperty<int>           HpReactive            { get; }
        ReadOnlyReactiveProperty<SkillState>    SkillStateReactive    { get; }
        ReadOnlyReactiveProperty<VerticalState> VerticalStateReactive { get; }
        ReadOnlyReactiveProperty<float>         DistanceReactive      { get; }
        ReadOnlyReactiveProperty<float>         SpeedReactive         { get; }

        // Fires the moment a collision clears debounce — before HP changes propagate.
        // Subscribers needing immediate visual response (knockback, flash) use this.
        Observable<Unit> OnHit { get; }

        Transform CharacterTransform { get; }

        /// <summary>Requests a lane change. direction: -1 = left, +1 = right.</summary>
        void RequestLaneChange(int direction);

        /// <summary>Requests a jump action.</summary>
        void RequestJump();

        /// <summary>Requests a slide action.</summary>
        void RequestSlide();

        /// <summary>Requests skill activation. No-op unless the skill is Ready.</summary>
        void ActivateSkill();

        /// <summary>Reports a physics collision with an obstacle. Called by CollisionReporter, never by player input.</summary>
        void ReportCollision();

        /// <summary>Starts or stops forward distance accumulation. Called by GameService on run start/end.</summary>
        void SetRunning(bool isRunning);
    }
}
