using System;

using UnityEngine;

using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using Unity.Netcode;

using SplitRun.Constants;

namespace SplitRun.Character
{
    public class ServerCharacter : NetworkBehaviour, ICharacter
    {
        private readonly NetworkVariable<int> _currentLane = new NetworkVariable<int>(
            GameConstants.k_LaneCenter,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<int> _hp = new NetworkVariable<int>(
            GameConstants.k_MaxHp,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<float> _distance = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<float> _speed = new NetworkVariable<float>(
            GameConstants.k_BaseRunSpeed,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<SkillState> _skillState = new NetworkVariable<SkillState>(
            SkillState.Ready,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<CharacterType> _charType = new NetworkVariable<CharacterType>(
            CharacterType.Default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<VerticalState> _verticalState = new NetworkVariable<VerticalState>(
            VerticalState.Ground,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly ReactiveProperty<int>           _laneReactive          = new ReactiveProperty<int>(GameConstants.k_LaneCenter);
        private readonly ReactiveProperty<int>           _hpReactive            = new ReactiveProperty<int>(GameConstants.k_MaxHp);
        private readonly ReactiveProperty<SkillState>    _skillStateReactive    = new ReactiveProperty<SkillState>(SkillState.Ready);
        private readonly ReactiveProperty<VerticalState> _verticalStateReactive = new ReactiveProperty<VerticalState>(VerticalState.Ground);
        private readonly ReactiveProperty<float>         _distanceReactive      = new ReactiveProperty<float>(0f);
        private readonly ReactiveProperty<float>         _speedReactive         = new ReactiveProperty<float>(GameConstants.k_BaseRunSpeed);
        private readonly Subject<Unit>                   _onHit                 = new Subject<Unit>();

        public ReadOnlyReactiveProperty<int>           LaneReactive          => _laneReactive;
        public ReadOnlyReactiveProperty<int>           HpReactive            => _hpReactive;
        public ReadOnlyReactiveProperty<SkillState>    SkillStateReactive    => _skillStateReactive;
        public ReadOnlyReactiveProperty<VerticalState> VerticalStateReactive => _verticalStateReactive;
        public ReadOnlyReactiveProperty<float>         DistanceReactive      => _distanceReactive;
        public ReadOnlyReactiveProperty<float>         SpeedReactive         => _speedReactive;
        public Observable<Unit>                        OnHit                 => _onHit;
        public Transform                               CharacterTransform    => transform;

        private float _lastCollisionTime = float.NegativeInfinity;
        private float _preHitSpeed;
        private Tween _hitStunTween;
        private bool  _isRunning;
        private bool  _isHitStunActive;

        public override void OnNetworkSpawn()
        {
            _hp.OnValueChanged            += OnHpChanged;
            _currentLane.OnValueChanged   += OnLaneChanged;
            _skillState.OnValueChanged    += OnSkillStateChanged;
            _verticalState.OnValueChanged += OnVerticalStateChanged;
            _distance.OnValueChanged      += OnDistanceChanged;
            _speed.OnValueChanged         += OnSpeedChanged;

            // OnValueChanged does not fire for the initial value — manual sync required.
            _hpReactive.Value            = _hp.Value;
            _laneReactive.Value          = _currentLane.Value;
            _skillStateReactive.Value    = _skillState.Value;
            _verticalStateReactive.Value = _verticalState.Value;
            _distanceReactive.Value      = _distance.Value;
            _speedReactive.Value         = _speed.Value;

            Debug.Log($"[ServerCharacter] Spawned — IsServer: {IsServer}, IsOwner: {IsOwner}");

            CharacterEvents.NotifySpawned(this);
        }

        public override void OnNetworkDespawn()
        {
            CharacterEvents.NotifyDespawned(this);

            _hitStunTween?.Kill();

            _hp.OnValueChanged            -= OnHpChanged;
            _currentLane.OnValueChanged   -= OnLaneChanged;
            _skillState.OnValueChanged    -= OnSkillStateChanged;
            _verticalState.OnValueChanged -= OnVerticalStateChanged;
            _distance.OnValueChanged      -= OnDistanceChanged;
            _speed.OnValueChanged         -= OnSpeedChanged;

            _laneReactive.Dispose();
            _hpReactive.Dispose();
            _skillStateReactive.Dispose();
            _verticalStateReactive.Dispose();
            _distanceReactive.Dispose();
            _speedReactive.Dispose();
            _onHit.Dispose();
        }

        private void Update()
        {
            if (!IsServer || !_isRunning) return;

            _distance.Value += _speed.Value * Time.deltaTime;

            if (!_isHitStunActive)
                _speed.Value = Mathf.Min(_speed.Value + GameConstants.k_SpeedAcceleration * Time.deltaTime, GameConstants.k_MaxRunSpeed);
        }

        public void RequestLaneChange(int direction) => ChangeLaneServerRpc(direction);
        public void RequestJump()                    => JumpServerRpc();
        public void RequestSlide()                   => SlideServerRpc();
        public void SetRunning(bool isRunning)       => _isRunning = isRunning;
        public void ReportCollision()                => ReportCollisionServerRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ReportCollisionServerRpc()
        {
            if (Time.time - _lastCollisionTime < GameConstants.k_CollisionDebounceDuration) return;
            _lastCollisionTime = Time.time;

            _onHit.OnNext(Unit.Default);

            // TODO(skill): route to SkillProcessor.ProcessCollision(this) before decrementing — skip decrement entirely if the skill blocks the hit
            _hp.Value = Mathf.Max(0, _hp.Value - 1);

            if (_hp.Value > 0) ApplyHitStun();

            Debug.Log($"[ServerCharacter] Collision reported — HP: {_hp.Value}");
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void TriggerSkillEffectClientRpc(SkillType skillType)
        {
            // TODO(skill): forward to CharacterAnimationDriver.PlaySkillEffect(skillType)
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void TriggerZoneTransitionClientRpc(int zoneIndex)
        {
            // TODO(chunk): forward to CharacterAnimationDriver.PlayZoneTransition(zoneIndex)
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ChangeLaneServerRpc(int direction)
        {
            _currentLane.Value = Mathf.Clamp(
                _currentLane.Value + direction,
                GameConstants.k_LaneLeft,
                GameConstants.k_LaneRight
            );
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JumpServerRpc()
        {
            if (_verticalState.Value != VerticalState.Ground) return;
            SetVerticalStateAsync(VerticalState.Jumping, GameConstants.k_JumpDuration);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SlideServerRpc()
        {
            if (_verticalState.Value != VerticalState.Ground) return;
            SetVerticalStateAsync(VerticalState.Sliding, GameConstants.k_SlideDuration);
        }

        private async UniTaskVoid SetVerticalStateAsync(VerticalState state, float duration)
        {
            _verticalState.Value = state;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _verticalState.Value = VerticalState.Ground;
        }

        private void ApplyHitStun()
        {
            _hitStunTween?.Kill();

            _preHitSpeed     = _speed.Value;
            _speed.Value     = 0f;
            _isHitStunActive = true;

            _hitStunTween = DOTween
                .To(() => _speed.Value, v => _speed.Value = v, _preHitSpeed, GameConstants.k_HitStunDuration)
                .SetEase(Ease.OutQuad)
                .SetDelay(ObstacleConstants.k_ImpactDuration)
                .OnComplete(() => _isHitStunActive = false);
        }

        private void OnHpChanged(int prev, int next)                                => _hpReactive.Value = next;
        private void OnLaneChanged(int prev, int next)                              => _laneReactive.Value = next;
        private void OnSkillStateChanged(SkillState prev, SkillState next)          => _skillStateReactive.Value = next;
        private void OnVerticalStateChanged(VerticalState prev, VerticalState next) => _verticalStateReactive.Value = next;
        private void OnDistanceChanged(float prev, float next)                      => _distanceReactive.Value = next;
        private void OnSpeedChanged(float prev, float next)                         => _speedReactive.Value = next;
    }
}
