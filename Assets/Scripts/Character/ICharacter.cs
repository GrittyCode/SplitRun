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

        /// <summary>The authoritative synced distance. DistanceReactive is a per-client smoothed mirror of it.</summary>
        float Distance { get; }

        /// <summary>True once the run goes live. A local per-client mirror of the run phase, never networked.</summary>
        ReadOnlyReactiveProperty<bool> RunningReactive { get; }

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

        /// <summary>Reports a physics collision with an obstacle. Called by CharacterHitBox, never by player input.</summary>
        void ReportCollision();

        /// <summary>Starts or stops forward distance accumulation. Called by GameService on run start/end.</summary>
        void SetRunning(bool isRunning);
    }

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
