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
    // the pause overlay, and the multiplayer control-guide intro. Child panels toggle; this root does not.
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

        private void Start()
        {
            _pausePanel.SetActive(false);
            HideGuides();

            BindReadouts();
            BindItemBuff();
            BindSkillGauge();
            BindPauseOverlay();
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

        private void OnDestroy() => CancelCountdown();

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

            StartSkillCountdown(_activeColor, SkillConstants.k_DashDuration);
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
                ? SkillConstants.k_DashCooldownDuration
                : SkillConstants.k_ShieldCooldownDuration;

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
