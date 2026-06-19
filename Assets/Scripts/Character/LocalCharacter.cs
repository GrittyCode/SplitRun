using System;

using UnityEngine;

using Cysharp.Threading.Tasks;
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

        // No multi-client duplicate-report risk locally, but kept in lockstep with
        // ServerCharacter so both ICharacter implementations behave identically.
        private float _lastCollisionTime = float.NegativeInfinity;
        private bool  _isRunning;

        public ReadOnlyReactiveProperty<int>           LaneReactive          => _lane;
        public ReadOnlyReactiveProperty<int>           HpReactive            => _hp;
        public ReadOnlyReactiveProperty<SkillState>    SkillStateReactive    => _skillState;
        public ReadOnlyReactiveProperty<VerticalState> VerticalStateReactive => _verticalState;
        public ReadOnlyReactiveProperty<float>         DistanceReactive      => _distance;
        public ReadOnlyReactiveProperty<float>         SpeedReactive         => _speed;
        public Transform                               CharacterTransform   => transform;

        private void Start()
        {
            CharacterEvents.NotifySpawned(this);
        }

        private void Update()
        {
            if (!_isRunning) return;
            _distance.Value += _speed.CurrentValue * Time.deltaTime;
        }

        private void OnDestroy()
        {
            CharacterEvents.NotifyDespawned(this);
            _lane.Dispose();
            _hp.Dispose();
            _skillState.Dispose();
            _verticalState.Dispose();
            _distance.Dispose();
            _speed.Dispose();
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

            // TODO(skill): route to SkillProcessor.ProcessCollision(this) before decrementing — skip decrement entirely if the skill blocks the hit
            _hp.Value = Mathf.Max(0, _hp.Value - 1);

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
    }
}
