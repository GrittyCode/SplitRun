using System;

using UnityEngine;

using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;

using SplitRun.Constants;

namespace SplitRun.Character
{
    // Local-only character for testing without Netcode — swap ServerCharacter to disable networking.
    public class LocalCharacter : MonoBehaviour, ICharacter
    {
        private readonly ReactiveProperty<int>           _lane          = new ReactiveProperty<int>(GameConstants.k_LaneCenter);
        private readonly ReactiveProperty<int>           _hp            = new ReactiveProperty<int>(GameConstants.k_MaxHp);
        private readonly ReactiveProperty<SkillState>    _skillState    = new ReactiveProperty<SkillState>(SkillState.Ready);
        private readonly ReactiveProperty<VerticalState> _verticalState = new ReactiveProperty<VerticalState>(VerticalState.Ground);
        private readonly ReactiveProperty<float>         _distance      = new ReactiveProperty<float>(0f);
        private readonly ReactiveProperty<float>         _speed         = new ReactiveProperty<float>(GameConstants.k_BaseRunSpeed);
        private readonly Subject<Unit>                   _onHit         = new Subject<Unit>();

        public ReadOnlyReactiveProperty<int>           LaneReactive          => _lane;
        public ReadOnlyReactiveProperty<int>           HpReactive            => _hp;
        public ReadOnlyReactiveProperty<SkillState>    SkillStateReactive    => _skillState;
        public ReadOnlyReactiveProperty<VerticalState> VerticalStateReactive => _verticalState;
        public ReadOnlyReactiveProperty<float>         DistanceReactive      => _distance;
        public ReadOnlyReactiveProperty<float>         SpeedReactive         => _speed;
        public Observable<Unit>                        OnHit                 => _onHit;
        public Transform                               CharacterTransform    => transform;

        private float _lastCollisionTime = float.NegativeInfinity;
        private float _preHitSpeed;
        private Tween _hitStunTween;
        private bool  _isRunning;
        private bool  _isHitStunActive;

        private void Start()  => CharacterEvents.NotifySpawned(this);

        private void Update()
        {
            if (!_isRunning) return;

            _distance.Value += _speed.CurrentValue * Time.deltaTime;

            if (!_isHitStunActive)
                _speed.Value = Mathf.Min(_speed.Value + GameConstants.k_SpeedAcceleration * Time.deltaTime, GameConstants.k_MaxRunSpeed);
        }

        private void OnDestroy()
        {
            CharacterEvents.NotifyDespawned(this);

            _hitStunTween?.Kill();

            _lane.Dispose();
            _hp.Dispose();
            _skillState.Dispose();
            _verticalState.Dispose();
            _distance.Dispose();
            _speed.Dispose();
            _onHit.Dispose();
        }

        public void RequestLaneChange(int direction)
        {
            _lane.Value = Mathf.Clamp(
                _lane.Value + direction,
                GameConstants.k_LaneLeft,
                GameConstants.k_LaneRight
            );
        }

        public void RequestJump()
        {
            if (_verticalState.Value != VerticalState.Ground) return;
            SetVerticalStateAsync(VerticalState.Jumping, GameConstants.k_JumpDuration);
        }

        public void RequestSlide()
        {
            if (_verticalState.Value != VerticalState.Ground) return;
            SetVerticalStateAsync(VerticalState.Sliding, GameConstants.k_SlideDuration);
        }

        public void SetRunning(bool isRunning) => _isRunning = isRunning;

        public void ReportCollision()
        {
            if (Time.time - _lastCollisionTime < GameConstants.k_CollisionDebounceDuration) return;
            _lastCollisionTime = Time.time;

            _onHit.OnNext(Unit.Default);

            // TODO(skill): route to SkillProcessor.ProcessCollision(this) before decrementing — skip decrement entirely if the skill blocks the hit
            _hp.Value = Mathf.Max(0, _hp.Value - 1);

            if (_hp.Value > 0) ApplyHitStun();

            Debug.Log($"[LocalCharacter] Collision reported — HP: {_hp.Value}");
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
    }
}
