using System.Globalization;

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        [Header("Popups")]
        [SerializeField] private GameObject _dimOverlay;
        [SerializeField] private GameObject _playPopup;
        [SerializeField] private GameObject _multiplayerPanel;

        [Header("Buttons")]
        [SerializeField] private Button _stageTapButton;
        [SerializeField] private Button _soloButton;
        [SerializeField] private Button _multiButton;
        [SerializeField] private Button _playCloseButton;
        [SerializeField] private Button _multiCloseButton;

        [Inject] private PlayerDataService _playerDataService;
        [Inject] private NetworkService    _networkService;

        private void Start()
        {
            BindTopBar();
            BindButtons();
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
            _stageTapButton.OnClickAsObservable().Subscribe(_ => OpenPlayPopup()).AddTo(this);
            _multiButton.OnClickAsObservable().Subscribe(_ => OpenMultiplayerPanel()).AddTo(this);
            _playCloseButton.OnClickAsObservable().Subscribe(_ => CloseAllPopups()).AddTo(this);
            _multiCloseButton.OnClickAsObservable().Subscribe(_ => CloseAllPopups()).AddTo(this);

            _soloButton.OnClickAsObservable()
                .Subscribe(_ => SceneManager.LoadScene(SceneConstants.k_GameSceneName))
                .AddTo(this);
        }

        private void OpenPlayPopup()
        {
            _dimOverlay.SetActive(true);
            _playPopup.SetActive(true);
        }

        // The popup morphs in place — same position, contents swapped.
        private void OpenMultiplayerPanel()
        {
            _playPopup.SetActive(false);
            _multiplayerPanel.SetActive(true);
        }

        private void CloseAllPopups()
        {
            // Closing the multiplayer panel abandons any session being created or joined.
            _networkService.Disconnect();

            _multiplayerPanel.SetActive(false);
            _playPopup.SetActive(false);
            _dimOverlay.SetActive(false);
        }
    }
}
