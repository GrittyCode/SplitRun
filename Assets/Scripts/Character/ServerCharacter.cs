using UnityEngine;

using R3;
using Unity.Netcode;

using SplitRun.Constants;

namespace SplitRun.Character
{
    public class ServerCharacter : NetworkBehaviour, ICharacter, ICharacterState
    {
        // Prefab identity is the type sync — NGO instantiates the same registered prefab on every client.
        [SerializeField] private CharacterType _characterType = CharacterType.Default;

        // Read Everyone / write Server are the NGO defaults — stated nowhere, relied on everywhere.
        private readonly NetworkVariable<int>           _currentLane   = new NetworkVariable<int>(GameConstants.k_LaneCenter);
        private readonly NetworkVariable<int>           _hp            = new NetworkVariable<int>(GameConstants.k_MaxHp);
        private readonly NetworkVariable<float>         _distance      = new NetworkVariable<float>(0f);
        private readonly NetworkVariable<float>         _speed         = new NetworkVariable<float>(GameConstants.k_BaseRunSpeed);
        private readonly NetworkVariable<SkillState>    _skillState    = new NetworkVariable<SkillState>(SkillState.Ready);
        private readonly NetworkVariable<VerticalState> _verticalState = new NetworkVariable<VerticalState>(VerticalState.Ground);

        private readonly ReactiveProperty<int>           _laneReactive          = new ReactiveProperty<int>(GameConstants.k_LaneCenter);
        private readonly ReactiveProperty<int>           _hpReactive            = new ReactiveProperty<int>(GameConstants.k_MaxHp);
        private readonly ReactiveProperty<SkillState>    _skillStateReactive    = new ReactiveProperty<SkillState>(SkillState.Ready);
        private readonly ReactiveProperty<VerticalState> _verticalStateReactive = new ReactiveProperty<VerticalState>(VerticalState.Ground);
        private readonly ReactiveProperty<float>         _distanceReactive      = new ReactiveProperty<float>(0f);

        private CharacterCore _core;

        public ReadOnlyReactiveProperty<int>           LaneReactive          => _laneReactive;
        public ReadOnlyReactiveProperty<int>           HpReactive            => _hpReactive;
        public ReadOnlyReactiveProperty<SkillState>    SkillStateReactive    => _skillStateReactive;
        public ReadOnlyReactiveProperty<VerticalState> VerticalStateReactive => _verticalStateReactive;
        public ReadOnlyReactiveProperty<float>         DistanceReactive      => _distanceReactive;
        public Observable<Unit>                        OnHit                 => _core.OnHit;
        public Transform                               CharacterTransform    => transform;
        public SkillType                               ActiveSkill           => _core != null ? _core.ActiveSkill : SkillType.None;

        int ICharacterState.Lane               { get => _currentLane.Value;   set => _currentLane.Value = value; }
        int ICharacterState.Hp                 { get => _hp.Value;            set => _hp.Value = value; }
        float ICharacterState.Distance         { get => _distance.Value;      set => _distance.Value = value; }
        float ICharacterState.Speed            { get => _speed.Value;         set => _speed.Value = value; }
        SkillState ICharacterState.Skill       { get => _skillState.Value;    set => _skillState.Value = value; }
        VerticalState ICharacterState.Vertical { get => _verticalState.Value; set => _verticalState.Value = value; }

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

            _core = new CharacterCore(this, _characterType, destroyCancellationToken);

            CharacterEvents.NotifySpawned(this);
        }

        public override void OnNetworkDespawn()
        {
            CharacterEvents.NotifyDespawned(this);

            _core?.Dispose();

            _hp.OnValueChanged            -= OnHpChanged;
            _currentLane.OnValueChanged   -= OnLaneChanged;
            _skillState.OnValueChanged    -= OnSkillStateChanged;
            _verticalState.OnValueChanged -= OnVerticalStateChanged;
            _distance.OnValueChanged      -= OnDistanceChanged;

            _laneReactive.Dispose();
            _hpReactive.Dispose();
            _skillStateReactive.Dispose();
            _verticalStateReactive.Dispose();
            _distanceReactive.Dispose();
        }

        private void Update()
        {
            if (!IsServer) return;
            _core.Tick(Time.deltaTime);
        }

        public void RequestLaneChange(int direction) => ChangeLaneServerRpc(direction);
        public void RequestJump()                    => JumpServerRpc();
        public void RequestSlide()                   => SlideServerRpc();
        public void ActivateSkill()                  => ActivateSkillServerRpc();
        public void ReportCollision()                => ReportCollisionServerRpc();
        public void SetRunning(bool isRunning)       => _core?.SetRunning(isRunning);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ReportCollisionServerRpc() => _core.ReportCollision();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ChangeLaneServerRpc(int direction) => _core.ChangeLane(direction);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JumpServerRpc() => _core.Jump();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SlideServerRpc() => _core.Slide();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ActivateSkillServerRpc() => _core.ActivateSkill();

        private void OnHpChanged(int prev, int next)                                => _hpReactive.Value = next;
        private void OnLaneChanged(int prev, int next)                              => _laneReactive.Value = next;
        private void OnSkillStateChanged(SkillState prev, SkillState next)          => _skillStateReactive.Value = next;
        private void OnVerticalStateChanged(VerticalState prev, VerticalState next) => _verticalStateReactive.Value = next;
        private void OnDistanceChanged(float prev, float next)                      => _distanceReactive.Value = next;
    }
}
