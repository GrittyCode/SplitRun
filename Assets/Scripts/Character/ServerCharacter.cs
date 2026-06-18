using System;

using UnityEngine;

using Cysharp.Threading.Tasks;
using R3;
using Unity.Netcode;

using SplitRun.Constants;

namespace SplitRun.Character
{
    // Network state container only — delegates visuals to CharacterAnimationDriver and physics to CollisionReporter.
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

        // Services and UI subscribe to these, never to NetworkVariables directly.
        private readonly ReactiveProperty<int>           _laneReactive          = new ReactiveProperty<int>(GameConstants.k_LaneCenter);
        private readonly ReactiveProperty<int>           _hpReactive            = new ReactiveProperty<int>(GameConstants.k_MaxHp);
        private readonly ReactiveProperty<SkillState>    _skillStateReactive    = new ReactiveProperty<SkillState>(SkillState.Ready);
        private readonly ReactiveProperty<VerticalState> _verticalStateReactive = new ReactiveProperty<VerticalState>(VerticalState.Ground);

        public ReadOnlyReactiveProperty<int>           LaneReactive          => _laneReactive;
        public ReadOnlyReactiveProperty<int>           HpReactive            => _hpReactive;
        public ReadOnlyReactiveProperty<SkillState>    SkillStateReactive    => _skillStateReactive;
        public ReadOnlyReactiveProperty<VerticalState> VerticalStateReactive => _verticalStateReactive;

        public override void OnNetworkSpawn()
        {
            _hp.OnValueChanged            += OnHpChanged;
            _currentLane.OnValueChanged   += OnLaneChanged;
            _skillState.OnValueChanged    += OnSkillStateChanged;
            _verticalState.OnValueChanged += OnVerticalStateChanged;

            // OnValueChanged does not fire for the initial value — manual sync required.
            _hpReactive.Value            = _hp.Value;
            _laneReactive.Value          = _currentLane.Value;
            _skillStateReactive.Value    = _skillState.Value;
            _verticalStateReactive.Value = _verticalState.Value;

            Debug.Log($"[ServerCharacter] Spawned — IsServer: {IsServer}, IsOwner: {IsOwner}");

            CharacterEvents.NotifySpawned(this);
        }

        public override void OnNetworkDespawn()
        {
            CharacterEvents.NotifyDespawned(this);

            _hp.OnValueChanged            -= OnHpChanged;
            _currentLane.OnValueChanged   -= OnLaneChanged;
            _skillState.OnValueChanged    -= OnSkillStateChanged;
            _verticalState.OnValueChanged -= OnVerticalStateChanged;

            _laneReactive.Dispose();
            _hpReactive.Dispose();
            _skillStateReactive.Dispose();
            _verticalStateReactive.Dispose();
        }

        public void RequestLaneChange(int direction) => ChangeLaneServerRpc(direction);
        public void RequestJump()                    => JumpServerRpc();
        public void RequestSlide()                   => SlideServerRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ReportCollisionServerRpc()
        {
            // TODO(collision): route to SkillProcessor.ProcessCollision(this), then decrement _hp.Value if skill did not block
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
            int next = Mathf.Clamp(
                _currentLane.Value + direction,
                GameConstants.k_LaneLeft,
                GameConstants.k_LaneRight
            );
            _currentLane.Value = next;
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

        private void OnHpChanged(int prev, int next)                              => _hpReactive.Value = next;
        private void OnLaneChanged(int prev, int next)                            => _laneReactive.Value = next;
        private void OnSkillStateChanged(SkillState prev, SkillState next)        => _skillStateReactive.Value = next;
        private void OnVerticalStateChanged(VerticalState prev, VerticalState next) => _verticalStateReactive.Value = next;
    }
}
