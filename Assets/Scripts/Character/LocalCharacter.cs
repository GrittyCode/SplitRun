using UnityEngine;

using R3;

using SplitRun.Constants;

namespace SplitRun.Character
{
    // Local-only character for testing without Netcode — swap ServerCharacter to disable networking.
    public class LocalCharacter : MonoBehaviour, ICharacter, ICharacterState
    {
        [SerializeField] private CharacterType _characterType = CharacterType.Default;

        private readonly ReactiveProperty<int>           _lane          = new ReactiveProperty<int>(GameConstants.k_LaneCenter);
        private readonly ReactiveProperty<int>           _hp            = new ReactiveProperty<int>(GameConstants.k_MaxHp);
        private readonly ReactiveProperty<SkillState>    _skillState    = new ReactiveProperty<SkillState>(SkillState.Ready);
        private readonly ReactiveProperty<VerticalState> _verticalState = new ReactiveProperty<VerticalState>(VerticalState.Ground);
        private readonly ReactiveProperty<float>         _distance      = new ReactiveProperty<float>(0f);
        private readonly ReactiveProperty<float>         _speed         = new ReactiveProperty<float>(GameConstants.k_BaseRunSpeed);

        private CharacterCore _core;

        public ReadOnlyReactiveProperty<int>           LaneReactive          => _lane;
        public ReadOnlyReactiveProperty<int>           HpReactive            => _hp;
        public ReadOnlyReactiveProperty<SkillState>    SkillStateReactive    => _skillState;
        public ReadOnlyReactiveProperty<VerticalState> VerticalStateReactive => _verticalState;
        public ReadOnlyReactiveProperty<float>         DistanceReactive      => _distance;
        public ReadOnlyReactiveProperty<float>         SpeedReactive         => _speed;
        public Observable<Unit>                        OnHit                 => _core.OnHit;
        public Transform                               CharacterTransform    => transform;
        public SkillType                               ActiveSkill           => _core != null ? _core.ActiveSkill : SkillType.None;

        int ICharacterState.Lane               { get => _lane.Value;          set => _lane.Value = value; }
        int ICharacterState.Hp                 { get => _hp.Value;            set => _hp.Value = value; }
        float ICharacterState.Distance         { get => _distance.Value;      set => _distance.Value = value; }
        float ICharacterState.Speed            { get => _speed.Value;         set => _speed.Value = value; }
        SkillState ICharacterState.Skill       { get => _skillState.Value;    set => _skillState.Value = value; }
        VerticalState ICharacterState.Vertical { get => _verticalState.Value; set => _verticalState.Value = value; }

        private void Awake()
        {
            _core = new CharacterCore(this, _characterType, destroyCancellationToken);
        }

        private void Start() => CharacterEvents.NotifySpawned(this);

        private void Update() => _core.Tick(Time.deltaTime);

        private void OnDestroy()
        {
            CharacterEvents.NotifyDespawned(this);

            _core.Dispose();

            _lane.Dispose();
            _hp.Dispose();
            _skillState.Dispose();
            _verticalState.Dispose();
            _distance.Dispose();
            _speed.Dispose();
        }

        public void RequestLaneChange(int direction) => _core.ChangeLane(direction);
        public void RequestJump()                    => _core.Jump();
        public void RequestSlide()                   => _core.Slide();
        public void ActivateSkill()                  => _core.ActivateSkill();
        public void ReportCollision()                => _core.ReportCollision();
        public void SetRunning(bool isRunning)       => _core.SetRunning(isRunning);
    }
}
