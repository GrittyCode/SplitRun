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

        public ReadOnlyReactiveProperty<int>           LaneReactive          => _lane;
        public ReadOnlyReactiveProperty<int>           HpReactive            => _hp;
        public ReadOnlyReactiveProperty<SkillState>    SkillStateReactive    => _skillState;
        public ReadOnlyReactiveProperty<VerticalState> VerticalStateReactive => _verticalState;

        private void Start()
        {
            CharacterEvents.NotifySpawned(this);
        }

        private void OnDestroy()
        {
            CharacterEvents.NotifyDespawned(this);
            _lane.Dispose();
            _hp.Dispose();
            _skillState.Dispose();
            _verticalState.Dispose();
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
