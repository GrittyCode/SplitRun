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

        // Fires when a collision clears debounce, before HP propagates — for immediate visual response.
        Observable<Unit> OnHit { get; }

        Transform CharacterTransform { get; }

        /// <summary>The skill this character was created with. SkillType.None for Default.</summary>
        SkillType ActiveSkill { get; }

        /// <summary>The cosmetic hat this character spawned with. Carried in the spawn payload.</summary>
        HatType Hat { get; }

        /// <summary>Instantiates the hat prefab on the model's hat socket. Pass null to remove the hat.</summary>
        void AttachHat(GameObject hatPrefab);

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
