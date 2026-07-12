using UnityEngine;

using R3;
using Unity.Netcode;

using SplitRun.Constants;

namespace SplitRun.Character
{
    public class NetworkCharacter : NetworkBehaviour, ICharacter, ICharacterState
    {
        // Prefab identity is the type sync — NGO instantiates the same registered prefab on every client.
        [SerializeField] private CharacterType _characterType = CharacterType.Default;

        // Read Everyone / write Server are the NGO defaults — stated nowhere, relied on everywhere.
        private readonly NetworkVariable<int>           _currentLane   = new NetworkVariable<int>(GameConstants.k_LaneCenter);

        // Declared before _hp so its delta applies first — EndRun reads the final distance on the hp-zero callback.
        private readonly NetworkVariable<float>         _distance      = new NetworkVariable<float>(0f);
        private readonly NetworkVariable<int>           _hp            = new NetworkVariable<int>(GameConstants.k_MaxHp);
        private readonly NetworkVariable<float>         _speed         = new NetworkVariable<float>(CharacterConstants.k_BaseRunSpeed);
        private readonly NetworkVariable<SkillState>    _skillState    = new NetworkVariable<SkillState>(SkillState.Ready);
        private readonly NetworkVariable<VerticalState> _verticalState = new NetworkVariable<VerticalState>(VerticalState.Ground);
        private readonly NetworkVariable<HatType>       _hat           = new NetworkVariable<HatType>(HatType.None);

        private readonly ReactiveProperty<int>           _laneReactive          = new ReactiveProperty<int>(GameConstants.k_LaneCenter);
        private readonly ReactiveProperty<int>           _hpReactive            = new ReactiveProperty<int>(GameConstants.k_MaxHp);
        private readonly ReactiveProperty<SkillState>    _skillStateReactive    = new ReactiveProperty<SkillState>(SkillState.Ready);
        private readonly ReactiveProperty<VerticalState> _verticalStateReactive = new ReactiveProperty<VerticalState>(VerticalState.Ground);
        private readonly ReactiveProperty<float>         _distanceReactive      = new ReactiveProperty<float>(0f);

        // Local presentation state derived per client from the run phase — never networked.
        private readonly ReactiveProperty<bool> _runningReactive = new ReactiveProperty<bool>(false);

        private CharacterRules _rules;
        private CharacterModel _model;
        private double         _lastDistanceTickTime;

        public ReadOnlyReactiveProperty<int>           LaneReactive          => _laneReactive;
        public ReadOnlyReactiveProperty<int>           HpReactive            => _hpReactive;
        public ReadOnlyReactiveProperty<SkillState>    SkillStateReactive    => _skillStateReactive;
        public ReadOnlyReactiveProperty<VerticalState> VerticalStateReactive => _verticalStateReactive;
        public ReadOnlyReactiveProperty<float>         DistanceReactive      => _distanceReactive;
        public ReadOnlyReactiveProperty<bool>          RunningReactive       => _runningReactive;
        public Transform                               CharacterTransform    => transform;
        public SkillType                               ActiveSkill           => _rules != null ? _rules.ActiveSkill : SkillType.None;

        // The synced value, free of client smoothing — the run record and the result screen read this.
        public float Distance => _distance.Value;

        // Set once before Spawn and never written again — one synchronous reader, so no reactive mirror.
        public HatType Hat => _hat.Value;

        int ICharacterState.Lane               { get => _currentLane.Value;   set => _currentLane.Value = value; }
        int ICharacterState.Hp                 { get => _hp.Value;            set => _hp.Value = value; }
        float ICharacterState.Distance         { get => _distance.Value;      set => _distance.Value = value; }
        float ICharacterState.Speed            { get => _speed.Value;         set => _speed.Value = value; }
        SkillState ICharacterState.Skill       { get => _skillState.Value;    set => _skillState.Value = value; }
        VerticalState ICharacterState.Vertical { get => _verticalState.Value; set => _verticalState.Value = value; }

        private void Awake() => _model = GetComponentInChildren<CharacterModel>();

        public override void OnNetworkSpawn()
        {
            _hp.OnValueChanged            += OnHpChanged;
            _currentLane.OnValueChanged   += OnLaneChanged;
            _skillState.OnValueChanged    += OnSkillStateChanged;
            _verticalState.OnValueChanged += OnVerticalStateChanged;
            _distance.OnValueChanged      += OnDistanceChanged;

            // OnValueChanged does not fire for the initial value — manual sync required.
            _hpReactive.Value            = _hp.Value;
            _laneReactive.Value          = _currentLane.Value;
            _skillStateReactive.Value    = _skillState.Value;
            _verticalStateReactive.Value = _verticalState.Value;
            _distanceReactive.Value      = _distance.Value;
            _lastDistanceTickTime        = NetworkManager.LocalTime.Time;

            _rules = new CharacterRules(this, _characterType, destroyCancellationToken);

            CharacterEvents.NotifySpawned(this);
        }

        public override void OnNetworkDespawn()
        {
            CharacterEvents.NotifyDespawned(this);

            _rules?.Dispose();
            _rules = null;

            _hp.OnValueChanged            -= OnHpChanged;
            _currentLane.OnValueChanged   -= OnLaneChanged;
            _skillState.OnValueChanged    -= OnSkillStateChanged;
            _verticalState.OnValueChanged -= OnVerticalStateChanged;
            _distance.OnValueChanged      -= OnDistanceChanged;
        }

        // Despawn and destroy are separate NGO events — sources outlive despawn and die with the object.
        public override void OnDestroy()
        {
            _laneReactive.Dispose();
            _hpReactive.Dispose();
            _skillStateReactive.Dispose();
            _verticalStateReactive.Dispose();
            _distanceReactive.Dispose();
            _runningReactive.Dispose();

            base.OnDestroy();
        }

        private void Update()
        {
            // A frame can land between Instantiate and OnNetworkSpawn — NetworkManager and the rules are both unreachable there.
            if (!IsSpawned) return;

            if (!IsServer)
            {
                InterpolateDistance(Time.deltaTime);
                return;
            }

            if (_rules == null) return;

            _rules.Tick(Time.deltaTime);
        }

        public void RequestLaneChange(int direction) => ChangeLaneServerRpc(direction);
        public void RequestJump()                    => JumpServerRpc();
        public void RequestSlide()                   => SlideServerRpc();
        public void ActivateSkill()                  => ActivateSkillServerRpc();

        public void SetRunning(bool isRunning)
        {
            _rules?.SetRunning(isRunning);
            _runningReactive.Value = isRunning;
        }

        public void SetHat(HatType hat) => _hat.Value = hat;

        public void AttachHat(GameObject hatPrefab)
        {
            if (!_model)
            {
                Debug.LogWarning("[NetworkCharacter] Prefab has no CharacterModel child — hat skipped.");
                return;
            }

            _model.AttachHat(hatPrefab);
        }

        // Only the server's own trigger may damage; a client report would land after hit-stun and double the same hit.
        public void ReportCollision()
        {
            if (!IsServer) return;

            _rules?.ReportCollision();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ChangeLaneServerRpc(int direction) => _rules?.ChangeLane(direction);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JumpServerRpc() => _rules?.Jump();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SlideServerRpc() => _rules?.Slide();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ActivateSkillServerRpc() => _rules?.ActivateSkill();

        // NetworkVariable ticks arrive stepped — the client chases them so everything downstream reads a smooth distance.
        private void InterpolateDistance(float deltaTime)
        {
            float target  = ExtrapolatedTarget();
            float current = _distanceReactive.Value;

            if (Mathf.Abs(target - current) > CharacterConstants.k_DistanceSnapThreshold)
            {
                _distanceReactive.Value = target;
                return;
            }

            // Hit-stun zeroes the synced speed — without a floor the client stalls short of the obstacle and desyncs the impact.
            float chaseSpeed = Mathf.Max(_speed.Value, CharacterConstants.k_BaseRunSpeed);
            float maxStep    = chaseSpeed * CharacterConstants.k_DistanceCatchUpMultiplier * deltaTime;

            _distanceReactive.Value = Mathf.MoveTowards(current, target, maxStep);
        }

        // Every tick is already one tick old on arrival; leading it by its own age cancels that lag.
        private float ExtrapolatedTarget()
        {
            if (!_runningReactive.Value) return _distance.Value;

            float age = (float)(NetworkManager.LocalTime.Time - _lastDistanceTickTime);
            return _distance.Value + _speed.Value * Mathf.Min(age, CharacterConstants.k_DistanceExtrapolationCap);
        }

        private void OnHpChanged(int prev, int next)                                => _hpReactive.Value = next;
        private void OnLaneChanged(int prev, int next)                              => _laneReactive.Value = next;
        private void OnSkillStateChanged(SkillState prev, SkillState next)          => _skillStateReactive.Value = next;
        private void OnVerticalStateChanged(VerticalState prev, VerticalState next) => _verticalStateReactive.Value = next;

        private void OnDistanceChanged(float prev, float next)
        {
            // Clients smooth distance in Update — mirroring raw ticks here would reintroduce stepping.
            if (IsServer)
            {
                _distanceReactive.Value = next;
                return;
            }

            _lastDistanceTickTime = NetworkManager.LocalTime.Time;
        }
    }
}
