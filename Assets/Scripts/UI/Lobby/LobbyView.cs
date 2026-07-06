using System.Globalization;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

using R3;
using VContainer;

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
        [SerializeField] private Button     _storageTabButton;
        [SerializeField] private Button     _shopTabButton;
        [SerializeField] private GameObject _playPanel;
        [SerializeField] private GameObject _storagePanel;
        [SerializeField] private GameObject _shopPanel;

        [Header("Play Panel")]
        [SerializeField] private GameObject _playMenu;
        [SerializeField] private GameObject _multiplayerPanel;
        [SerializeField] private Button     _stageTapButton;
        [SerializeField] private Button     _soloButton;
        [SerializeField] private Button     _multiButton;
        [SerializeField] private Button     _multiBackButton;

        [Inject] private PlayerDataService _playerDataService;
        [Inject] private NetworkService    _networkService;

        private void Start()
        {
            BindTopBar();
            BindButtons();

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
            _storagePanel.SetActive(panel == _storagePanel);
            _shopPanel.SetActive(panel == _shopPanel);

            _playTabButton.interactable    = panel != _playPanel;
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
    }
}
