using System;
using System.Threading;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;
using R3;
using VContainer;

using SplitRun.Constants;
using SplitRun.Network;

namespace SplitRun.UI.Lobby
{
    public class MultiplayerPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _joinCodeField;
        [SerializeField] private Button         _okButton;
        [SerializeField] private Button         _createButton;
        [SerializeField] private TMP_Text       _statusText;
        [SerializeField] private TMP_Text       _joinCodeText;
        [SerializeField] private GameObject     _textBorder;

        [Inject] private NetworkService _networkService;

        private void Start()
        {
            BindNetworkState();
            BindButtons();
        }

        private void BindNetworkState()
        {
            _networkService.ConnectionState
                .CombineLatest(_networkService.IsSessionReady, ToStatusText)
                .Subscribe(text => _statusText.text = text)
                .AddTo(this);

            _networkService.ConnectionState
                .Subscribe(ApplyConnectionState)
                .AddTo(this);

            _networkService.JoinCode
                .Subscribe(code => _joinCodeText.text = code)
                .AddTo(this);

            // Only the host drives the scene change — NGO scene sync carries the client along.
            _networkService.ConnectionState
                .CombineLatest(_networkService.IsSessionReady, IsReadyHost)
                .Where(isReadyHost => isReadyHost)
                .Subscribe(_ => _networkService.LoadGameScene())
                .AddTo(this);

            _networkService.ConnectionState
                .Where(state => state == NetworkConnectionState.Failed)
                .Subscribe(_ => DismissFailureAsync(this.GetCancellationTokenOnDestroy()).Forget())
                .AddTo(this);
        }

        private void BindButtons()
        {
            _createButton.OnClickAsObservable()
                .Subscribe(_ => _networkService.CreateRoomAsync(this.GetCancellationTokenOnDestroy()).Forget())
                .AddTo(this);

            _okButton.OnClickAsObservable()
                .Subscribe(_ => JoinWithInputCode())
                .AddTo(this);
        }

        private void JoinWithInputCode()
        {
            string code = _joinCodeField.text.Trim().ToUpperInvariant();
            _networkService.JoinRoomAsync(code, this.GetCancellationTokenOnDestroy()).Forget();
        }

        // Failed is a dead end for input — auto-return to Offline so the panel is immediately reusable.
        private async UniTaskVoid DismissFailureAsync(CancellationToken ct)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(NetworkConstants.k_FailedStateDisplaySeconds), cancellationToken: ct);

            if (_networkService.ConnectionState.CurrentValue != NetworkConnectionState.Failed) return;

            _joinCodeField.text = string.Empty;
            _networkService.Disconnect();
        }

        private void ApplyConnectionState(NetworkConnectionState state)
        {
            bool canStart = state is NetworkConnectionState.Offline or NetworkConnectionState.Failed;

            _createButton.interactable  = canStart;
            _okButton.interactable      = canStart;
            _joinCodeField.interactable = canStart;

            // The border frames status/code text — an empty frame in Offline looks broken.
            _textBorder.SetActive(state != NetworkConnectionState.Offline);
        }

        private static bool IsReadyHost(NetworkConnectionState state, bool isSessionReady)
            => state == NetworkConnectionState.Hosting && isSessionReady;

        private static string ToStatusText(NetworkConnectionState state, bool isSessionReady)
        {
            if (isSessionReady)
                return "2 / 2 connected!";

            return state switch
            {
                NetworkConnectionState.Connecting => "Connecting...",
                NetworkConnectionState.Hosting    => "1 / 2 — share the code",
                NetworkConnectionState.Joined     => "Connected!",
                NetworkConnectionState.Failed     => "Connection failed — try again",
                _                                 => string.Empty,
            };
        }
    }
}
