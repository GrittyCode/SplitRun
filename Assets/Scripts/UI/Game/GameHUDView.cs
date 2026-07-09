using System;
using System.Threading;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;
using R3;
using Unity.Netcode;
using VContainer;

using SplitRun.Character;
using SplitRun.Constants;
using SplitRun.Game;
using SplitRun.Item;
using SplitRun.Network;

namespace SplitRun.UI.Game
{
    // The whole in-game HUD on one always-active canvas: readouts, item/skill indicators,
    // the pause overlay, the game-over result, and the multiplayer control-guide intro.
    // Child panels toggle; this root does not.
    public class GameHUDView : MonoBehaviour
    {
        [Header("Readouts")]
        [SerializeField] private TMP_Text _distanceLabel;
        [SerializeField] private TMP_Text _coinLabel;
        [SerializeField] private Button   _pauseButton;

        [Header("HP")]
        [SerializeField] private Image[] _hearts;
        [SerializeField] private Sprite  _fullHeart;
        [SerializeField] private Sprite  _emptyHeart;

        [Header("Item Buff")]
        [SerializeField] private Transform      _itemBuffRoot;
        [SerializeField] private TimedIndicator _itemIndicatorPrefab;

        [Header("Skill Gauge")]
        [SerializeField] private Transform      _skillGaugeRoot;
        [SerializeField] private TimedIndicator _skillIndicatorPrefab;
        [SerializeField] private Color _readyColor    = new Color(0.39f, 0.60f, 0.13f);
        [SerializeField] private Color _activeColor   = new Color(0.94f, 0.62f, 0.15f);
        [SerializeField] private Color _cooldownColor = new Color(0.53f, 0.53f, 0.50f);
        [SerializeField] private Color _shieldColor   = new Color(0.11f, 0.62f, 0.46f);

        [Header("Pause Overlay")]
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private TMP_Text   _pauseCenterText;
        [SerializeField] private Button     _resumeButton;

        [Header("Game Over")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TMP_Text   _resultDistanceLabel;
        [SerializeField] private TMP_Text   _resultGoldLabel;
        [SerializeField] private GameObject _resultBestBadge;
        [SerializeField] private Button     _quitButton;

        [Header("Intro Guide")]
        [SerializeField] private GameObject _laneGuide;
        [SerializeField] private GameObject _verticalGuide;

        [Inject] private GameService    _gameService;
        [Inject] private GameSession    _gameSession;
        [Inject] private ItemService    _itemService;
        [Inject] private HudIconLibrary _icons;

        private TimedIndicator _magnetIndicator;

        private TimedIndicator _skillIndicator;
        private SkillType      _skill;
        private float          _skillRemaining;
        private float          _skillDuration;
        private bool           _isSkillCounting;

        private CancellationTokenSource _countdownCts;
        private CancellationTokenSource _resultRollCts;

        private void Start()
        {
            _pausePanel.SetActive(false);
            _gameOverPanel.SetActive(false);
            _resultBestBadge.SetActive(false);
            HideGuides();

            BindReadouts();
            BindItemBuff();
            BindSkillGauge();
            BindPauseOverlay();
            BindGameOver();
            BindIntroGuide();
        }

        private void Update()
        {
            if (!_isSkillCounting) return;

            // Server-side skill timers freeze outside Running (pause) — hold the fill in step with them.
            if (_gameService.Phase.CurrentValue != GamePhase.Running) return;

            _skillRemaining = Mathf.Max(0f, _skillRemaining - Time.deltaTime);
            _skillIndicator.SetFill(_skillDuration > 0f ? _skillRemaining / _skillDuration : 0f);

            if (_skillRemaining <= 0f) _isSkillCounting = false;
        }

        private void OnDestroy()
        {
            CancelCountdown();
            CancelResultRoll();
        }

        private void BindReadouts()
        {
            // Whole-meter gating keeps the label from allocating a string every frame.
            _gameService.CurrentDistance
                .Select(distance => (int)distance)
                .DistinctUntilChanged()
                .Subscribe(distance => _distanceLabel.text = $"{distance}m")
                .AddTo(this);

            _gameService.CurrentHp
                .Subscribe(RefreshHearts)
                .AddTo(this);

            _itemService.Coins
                .Subscribe(coins => _coinLabel.text = $"{coins}")
                .AddTo(this);

            _pauseButton.OnClickAsObservable()
                .Subscribe(_ => _gameService.RequestPause())
                .AddTo(this);
        }

        private void RefreshHearts(int hp)
        {
            for (int i = 0; i < _hearts.Length; i++)
                _hearts[i].sprite = i < hp ? _fullHeart : _emptyHeart;
        }

        // ---- Item buff (bottom-left) ----

        private void BindItemBuff()
        {
            _itemService.MagnetRemaining
                .Subscribe(OnMagnetChanged)
                .AddTo(this);
        }

        private void OnMagnetChanged(float remaining)
        {
            if (remaining <= 0f)
            {
                if (_magnetIndicator) _magnetIndicator.SetVisible(false);
                return;
            }

            if (!_magnetIndicator)
            {
                _magnetIndicator = Instantiate(_itemIndicatorPrefab, _itemBuffRoot);
                _magnetIndicator.SetIcon(_icons.IconFor(ItemType.Magnet));
            }

            _magnetIndicator.SetVisible(true);
            _magnetIndicator.SetFill(remaining / ItemConstants.k_MagnetDuration);
        }

        // ---- Skill gauge (bottom-right) ----

        private void BindSkillGauge()
        {
            _gameService.ActiveSkill
                .CombineLatest(_gameService.CurrentSkillState, (skill, state) => (skill, state))
                .Subscribe(pair => OnSkillStateChanged(pair.skill, pair.state))
                .AddTo(this);
        }

        private void OnSkillStateChanged(SkillType skill, SkillState state)
        {
            _skill = skill;

            if (_skill == SkillType.None)
            {
                if (_skillIndicator) _skillIndicator.SetVisible(false);
                return;
            }

            EnsureSkillIndicator();
            _skillIndicator.SetVisible(true);
            _skillIndicator.SetIcon(_icons.IconFor(_skill));

            ConfigureSkill(state);
        }

        private void ConfigureSkill(SkillState state)
        {
            switch (state)
            {
                case SkillState.Ready:
                    SetSkillStatic(_readyColor);
                    break;
                case SkillState.Active:
                    ConfigureSkillActive();
                    break;
                case SkillState.Cooldown:
                    StartSkillCountdown(_cooldownColor, CooldownDuration());
                    break;
            }
        }

        // Shield's active phase lasts until a hit is absorbed — a full steady ring, distinct from
        // Dash's timed burst.
        private void ConfigureSkillActive()
        {
            if (_skill == SkillType.Shield)
            {
                SetSkillStatic(_shieldColor);
                return;
            }

            StartSkillCountdown(_activeColor, CharacterConstants.k_DashDuration);
        }

        private void StartSkillCountdown(Color color, float duration)
        {
            _skillIndicator.SetColor(color);
            _skillIndicator.SetFill(1f);

            _skillDuration   = duration;
            _skillRemaining  = duration;
            _isSkillCounting = true;
        }

        private void SetSkillStatic(Color color)
        {
            _isSkillCounting = false;
            _skillIndicator.SetColor(color);
            _skillIndicator.SetFill(1f);
        }

        private void EnsureSkillIndicator()
        {
            if (!_skillIndicator)
                _skillIndicator = Instantiate(_skillIndicatorPrefab, _skillGaugeRoot);
        }

        private float CooldownDuration() =>
            _skill == SkillType.Dash
                ? CharacterConstants.k_DashCooldownDuration
                : CharacterConstants.k_ShieldCooldownDuration;

        // ---- Pause overlay ----

        private void BindPauseOverlay()
        {
            _gameSession.PauseStateReactive
                .Subscribe(ApplyPauseState)
                .AddTo(this);

            _resumeButton.OnClickAsObservable()
                .Subscribe(_ => _gameSession.RequestResume())
                .AddTo(this);
        }

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
                    _pausePanel.SetActive(false);
                    break;
            }
        }

        private void ShowPaused()
        {
            _pausePanel.SetActive(true);
            _pauseCenterText.text = GameConstants.k_PausedLabel;
            _resumeButton.gameObject.SetActive(IsLocalPauser());
        }

        private void ShowCountdown()
        {
            _pausePanel.SetActive(true);
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
                    _pauseCenterText.text = remaining.ToString();
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
            return networkManager && networkManager.LocalClientId == _gameSession.PausedBy;
        }

        private void CancelCountdown()
        {
            _countdownCts?.Cancel();
            _countdownCts?.Dispose();
            _countdownCts = null;
        }

        // ---- Game over result ----

        private void BindGameOver()
        {
            _gameService.Phase
                .Where(phase => phase == GamePhase.GameOver)
                .Subscribe(_ => ShowResult())
                .AddTo(this);

            // Routed through the service so GameEntryPoint stays the sole owner of session teardown.
            _quitButton.OnClickAsObservable()
                .Subscribe(_ => _gameService.RequestEndSession())
                .AddTo(this);
        }

        private void ShowResult()
        {
            _gameOverPanel.SetActive(true);

            // The run is over and both values are frozen, so one synchronous read captures the finals.
            int  finalDistance = (int)_gameService.CurrentDistance.CurrentValue;
            int  finalGold     = _itemService.Coins.CurrentValue;
            bool isNewBest     = _gameService.IsNewBestDistance;

            CancelResultRoll();
            _resultRollCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            RollResultAsync(finalDistance, finalGold, isNewBest, _resultRollCts.Token).Forget();
        }

        // Distance and gold climb together on one eased timeline; the quit button stays locked until
        // they land so a mid-roll tap can't cut the payoff short.
        private async UniTaskVoid RollResultAsync(int finalDistance, int finalGold, bool isNewBest, CancellationToken ct)
        {
            _quitButton.interactable = false;
            _resultBestBadge.SetActive(false);

            float elapsed       = 0f;
            int   shownDistance = -1;
            int   shownGold     = -1;

            try
            {
                while (elapsed < GameConstants.k_ResultRollSeconds)
                {
                    elapsed += Time.deltaTime;
                    float t = EaseOutCubic(Mathf.Clamp01(elapsed / GameConstants.k_ResultRollSeconds));

                    int distance = Mathf.RoundToInt(finalDistance * t);
                    int gold     = Mathf.RoundToInt(finalGold * t);

                    // Whole-unit gating keeps the roll from allocating a string every frame.
                    if (distance != shownDistance) { shownDistance = distance; _resultDistanceLabel.text = $"{distance}m"; }
                    if (gold     != shownGold)     { shownGold     = gold;     _resultGoldLabel.text     = $"+{gold}"; }

                    await UniTask.Yield(ct);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Land exactly on the finals — rounding mid-roll can leave the last tick a unit short.
            _resultDistanceLabel.text = $"{finalDistance}m";
            _resultGoldLabel.text     = $"+{finalGold}";

            _quitButton.interactable = true;

            if (!isNewBest)
                return;

            // New record — leave the badge blinking until the same token cancels it on quit/teardown.
            await BlinkBestAsync(ct);
        }

        // Runs until the result screen is torn down; UniTask.Delay drives it off the player loop, so
        // toggling the badge inactive never stalls the loop the way a coroutine would.
        private async UniTask BlinkBestAsync(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    _resultBestBadge.SetActive(true);
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(GameConstants.k_BestBlinkOnSeconds), cancellationToken: ct);

                    _resultBestBadge.SetActive(false);
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(GameConstants.k_BestBlinkOffSeconds), cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private void CancelResultRoll()
        {
            _resultRollCts?.Cancel();
            _resultRollCts?.Dispose();
            _resultRollCts = null;
        }

        // ---- Intro guide (multiplayer only) ----

        private void BindIntroGuide()
        {
            _gameSession.RunStartReactive
                .Subscribe(ApplyRunStartState)
                .AddTo(this);
        }

        private void ApplyRunStartState(RunStartState state)
        {
            if (state != RunStartState.Intro)
            {
                HideGuides();
                return;
            }

            SessionRole role = SessionRoleResolver.Resolve();

            _laneGuide.SetActive(role != SessionRole.VerticalOnly);
            _verticalGuide.SetActive(role != SessionRole.LaneOnly);
        }

        private void HideGuides()
        {
            _laneGuide.SetActive(false);
            _verticalGuide.SetActive(false);
        }
    }
}
