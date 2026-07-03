using System;
using System.Threading;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;
using R3;
using Unity.Netcode;
using VContainer;

using SplitRun.Constants;
using SplitRun.Game;

namespace SplitRun.UI.Game
{
    public class PauseOverlayView : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text   _centerText;
        [SerializeField] private Button     _resumeButton;

        [Inject] private GameSession _gameSession;

        private CancellationTokenSource _countdownCts;

        private void Start()
        {
            _panelRoot.SetActive(false);

            _gameSession.PauseStateReactive
                .Subscribe(ApplyPauseState)
                .AddTo(this);

            _resumeButton.OnClickAsObservable()
                .Subscribe(_ => _gameSession.RequestResume())
                .AddTo(this);
        }

        private void OnDestroy() => CancelCountdown();

        private void ApplyPauseState(PauseState state)
        {
            CancelCountdown();

            switch (state)
            {
                case PauseState.Paused:
                    ShowPaused();
                    break;
                case PauseState.Countdown:
                    ShowCountdown();
                    break;
                default:
                    _panelRoot.SetActive(false);
                    break;
            }
        }

        private void ShowPaused()
        {
            _panelRoot.SetActive(true);
            _centerText.text = GameConstants.k_PausedLabel;
            _resumeButton.gameObject.SetActive(IsLocalPauser());
        }

        private void ShowCountdown()
        {
            _panelRoot.SetActive(true);
            _resumeButton.gameObject.SetActive(false);

            _countdownCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            RunCountdownAsync(_countdownCts.Token).Forget();
        }

        // Rendered locally from the state transition; the server unpauses on its own timer.
        private async UniTaskVoid RunCountdownAsync(CancellationToken ct)
        {
            int remaining = Mathf.CeilToInt(GameConstants.k_ResumeCountdownSeconds);

            try
            {
                while (remaining > 0)
                {
                    _centerText.text = remaining.ToString();
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(GameConstants.k_ResumeCountdownStepSeconds), cancellationToken: ct);
                    remaining--;
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        // The pauser owns resume — only their device shows the button.
        private bool IsLocalPauser()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager && networkManager.LocalClientId == _gameSession.PausedByReactive.CurrentValue;
        }

        private void CancelCountdown()
        {
            _countdownCts?.Cancel();
            _countdownCts?.Dispose();
            _countdownCts = null;
        }

    }
}
