using UnityEngine;

using SplitRun.Constants;

namespace SplitRun.UI.Lobby
{
    // Runs the lobby stage model on a dedicated idle controller, roaring on each character/hat change.
    public class LobbyStageIdleAnimator : MonoBehaviour
    {
        [SerializeField] private RuntimeAnimatorController _stageController;

        private readonly int _roarHash = Animator.StringToHash(AnimatorConstants.k_TriggerRoar);

        private Animator _animator;

        public void Bind(Animator animator)
        {
            _animator = animator;
            if (!_animator)
                return;

            _animator.runtimeAnimatorController = _stageController;
            Roar();
        }

        public void Roar()
        {
            if (_animator)
                _animator.SetTrigger(_roarHash);
        }
    }
}
