using System;
using System.Globalization;
using System.Threading;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;
using R3;
using VContainer;

using SplitRun.Constants;
using SplitRun.Data;
using SplitRun.Network;

namespace SplitRun.UI.Lobby
{
    public class LobbyView : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text _coinText;
        [SerializeField] private TMP_Text _bestText;

        [Header("Tabs")]
        [SerializeField] private Button     _playTabButton;
        [SerializeField] private Button     _missionTabButton;
        [SerializeField] private Button     _storageTabButton;
        [SerializeField] private Button     _shopTabButton;
        [SerializeField] private GameObject _playPanel;
        [SerializeField] private GameObject _missionPanel;
        [SerializeField] private GameObject _storagePanel;
        [SerializeField] private GameObject _shopPanel;

        [Header("Play Panel")]
        [SerializeField] private GameObject _playMenu;
        [SerializeField] private GameObject _multiplayerPanel;
        [SerializeField] private Button     _stageTapButton;
        [SerializeField] private Button     _soloButton;
        [SerializeField] private Button     _multiButton;
        [SerializeField] private Button     _multiBackButton;

        [Header("Multiplayer Panel")]
        [SerializeField] private TMP_InputField _joinCodeField;
        [SerializeField] private Button         _okButton;
        [SerializeField] private Button         _createButton;
        [SerializeField] private TMP_Text       _statusText;
        [SerializeField] private TMP_Text       _joinCodeText;
        [SerializeField] private GameObject     _textBorder;

        [Inject] private PlayerDataService _playerDataService;
        [Inject] private NetworkService    _networkService;

        private void Start()
        {
            BindTopBar();
            BindButtons();
            BindMultiplayer();

            // Deterministic initial state regardless of which panels were left active in the scene.
            SelectTab(_playPanel);
        }

        private void BindTopBar()
        {
            _playerDataService.Coins
                .Subscribe(coins => _coinText.text = coins.ToString("N0", CultureInfo.InvariantCulture))
                .AddTo(this);

            _playerDataService.BestDistance
                .Subscribe(best => _bestText.text = $"{best.ToString("N0", CultureInfo.InvariantCulture)}m")
                .AddTo(this);
        }

        private void BindButtons()
        {
            _playTabButton.OnClickAsObservable().Subscribe(_ => SelectTab(_playPanel)).AddTo(this);
            _missionTabButton.OnClickAsObservable().Subscribe(_ => SelectTab(_missionPanel)).AddTo(this);
            _storageTabButton.OnClickAsObservable().Subscribe(_ => SelectTab(_storagePanel)).AddTo(this);
            _shopTabButton.OnClickAsObservable().Subscribe(_ => SelectTab(_shopPanel)).AddTo(this);

            _stageTapButton.OnClickAsObservable().Subscribe(_ => SelectTab(_playPanel)).AddTo(this);

            _multiButton.OnClickAsObservable().Subscribe(_ => ShowMultiplayerFlow()).AddTo(this);
            _multiBackButton.OnClickAsObservable().Subscribe(_ => ExitMultiplayerFlow()).AddTo(this);

            // Starting a run is session policy — the view only forwards the intent.
            _soloButton.OnClickAsObservable().Subscribe(_ => _networkService.StartSolo()).AddTo(this);
        }

        // Exactly one tab is active at all times; the active tab button is disabled,
        // which doubles as the selection highlight and makes a re-tap impossible.
        private void SelectTab(GameObject panel)
        {
            if (panel.activeSelf)
                return;

            // Leaving the play sheet abandons any session being created or joined; no-op when offline.
            if (_playPanel.activeSelf)
                _networkService.Disconnect();

            _playPanel.SetActive(panel == _playPanel);
            _missionPanel.SetActive(panel == _missionPanel);
            _storagePanel.SetActive(panel == _storagePanel);
            _shopPanel.SetActive(panel == _shopPanel);

            _playTabButton.interactable    = panel != _playPanel;
            _missionTabButton.interactable = panel != _missionPanel;
            _storageTabButton.interactable = panel != _storagePanel;
            _shopTabButton.interactable    = panel != _shopPanel;

            if (panel == _playPanel)
                ShowPlayMenu();
        }

        // The sheet morphs in place — same panel, contents swapped.
        private void ShowMultiplayerFlow()
        {
            _playMenu.SetActive(false);
            _multiplayerPanel.SetActive(true);
        }

        private void ExitMultiplayerFlow()
        {
            _networkService.Disconnect();
            ShowPlayMenu();
        }

        private void ShowPlayMenu()
        {
            _multiplayerPanel.SetActive(false);
            _playMenu.SetActive(true);
        }

        private void BindMultiplayer()
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

            _networkService.ConnectionState
                .Where(state => state == NetworkConnectionState.Failed)
                .Subscribe(_ => DismissFailureAsync(this.GetCancellationTokenOnDestroy()).Forget())
                .AddTo(this);

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
