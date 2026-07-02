using System;
using System.Threading;

using UnityEngine;

using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;

using SplitRun.Constants;

namespace SplitRun.Character
{
    // Storage-agnostic core: adapters supply an ICharacterState (NetworkVariable- or ReactiveProperty-backed).
    public sealed class CharacterCore : IDisposable
    {
        private readonly ICharacterState   _state;
        private readonly CancellationToken _cancellationToken;
        private readonly ICharacterSkill   _skill;

        private readonly Subject<Unit> _onHit = new Subject<Unit>();

        // Kept separate from _state.Speed so the dash multiplier never compounds into acceleration.
        private float _baseSpeed = GameConstants.k_BaseRunSpeed;
        private float _speedMultiplier = 1f;

        private float      _lastCollisionTime = float.NegativeInfinity;
        private float      _preHitSpeed;
        private Tween      _hitStunTween;
        private SkillState _lastSkillState = SkillState.Ready;
        private bool       _isRunning;
        private bool       _isHitStunActive;
        private bool       _isInvincible;

        public CharacterCore(ICharacterState state, CharacterType characterType, CancellationToken cancellationToken)
        {
            _state             = state;
            _cancellationToken = cancellationToken;
            _skill             = characterType switch
            {
                CharacterType.Shield => new ShieldSkill(this),
                CharacterType.Dash   => new DashSkill(this),
                _                    => new NullSkill(),
            };
        }

        public Observable<Unit> OnHit       => _onHit;
        public SkillType        ActiveSkill => _skill.Type;

        public void Tick(float deltaTime)
        {
            if (!_isRunning) return;

            _skill.Tick(deltaTime);
            MirrorSkillState();

            _state.Distance += _state.Speed * deltaTime;

            if (_isHitStunActive) return;

            _baseSpeed   = Mathf.Min(_baseSpeed + GameConstants.k_SpeedAcceleration * deltaTime, GameConstants.k_MaxRunSpeed);
            _state.Speed = _baseSpeed * _speedMultiplier;
        }

        public void SetRunning(bool isRunning) => _isRunning = isRunning;

        public void ChangeLane(int direction)
        {
            _state.Lane = Mathf.Clamp(
                _state.Lane + direction,
                GameConstants.k_LaneLeft,
                GameConstants.k_LaneRight
            );
        }

        public void Jump()
        {
            if (_state.Vertical != VerticalState.Ground) return;
            SetVerticalStateAsync(VerticalState.Jumping, GameConstants.k_JumpDuration).Forget();
        }

        public void Slide()
        {
            if (_state.Vertical != VerticalState.Ground) return;
            SetVerticalStateAsync(VerticalState.Sliding, GameConstants.k_SlideDuration).Forget();
        }

        public void ActivateSkill() => _skill.Activate();

        public void ReportCollision()
        {
            if (Time.time - _lastCollisionTime < GameConstants.k_CollisionDebounceDuration) return;
            _lastCollisionTime = Time.time;

            if (_isInvincible)
            {
                _skill.OnDamageBlocked();
                return;
            }

            _onHit.OnNext(Unit.Default);

            _state.Hp = Mathf.Max(0, _state.Hp - 1);

            if (_state.Hp > 0) ApplyHitStun();
        }

        public void SetInvincible(bool isInvincible) => _isInvincible = isInvincible;

        public void SetSpeedMultiplier(float multiplier) => _speedMultiplier = multiplier;

        public void Dispose()
        {
            _hitStunTween?.Kill();
            _onHit.Dispose();
        }

        private void MirrorSkillState()
        {
            if (_skill.State == _lastSkillState) return;

            _lastSkillState = _skill.State;
            _state.Skill    = _skill.State;
        }

        private async UniTaskVoid SetVerticalStateAsync(VerticalState state, float duration)
        {
            _state.Vertical = state;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: _cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _state.Vertical = VerticalState.Ground;
        }

        private void ApplyHitStun()
        {
            _hitStunTween?.Kill();

            _preHitSpeed     = _state.Speed;
            _state.Speed     = 0f;
            _isHitStunActive = true;

            _hitStunTween = DOTween
                .To(() => _state.Speed, v => _state.Speed = v, _preHitSpeed, GameConstants.k_HitStunDuration)
                .SetEase(Ease.OutQuad)
                .SetDelay(ObstacleConstants.k_ImpactDuration)
                .OnComplete(() => _isHitStunActive = false);
        }
    }
}
