using UnityEngine;

using R3;
using VContainer;

using SplitRun.Character;
using SplitRun.Constants;
using SplitRun.Game;

namespace SplitRun.UI.Game
{
    // Bottom-right skill gauge. The fill always reads as remaining time, draining through both the
    // active window and the cooldown. Skill remaining is reconstructed client-side from the
    // server-authoritative SkillState transitions, so no per-frame value is networked.
    public class SkillGaugeView : MonoBehaviour
    {
        [SerializeField] private TimedIndicator _indicatorPrefab;

        [SerializeField] private Color _readyColor    = new Color(0.39f, 0.60f, 0.13f);
        [SerializeField] private Color _activeColor   = new Color(0.94f, 0.62f, 0.15f);
        [SerializeField] private Color _cooldownColor = new Color(0.53f, 0.53f, 0.50f);
        [SerializeField] private Color _shieldColor   = new Color(0.11f, 0.62f, 0.46f);

        [Inject] private GameService    _gameService;
        [Inject] private HudIconLibrary _icons;

        private TimedIndicator _indicator;
        private SkillType      _skill;
        private float          _remaining;
        private float          _duration;
        private bool           _isCounting;

        private void Start()
        {
            _gameService.ActiveSkill
                .CombineLatest(_gameService.CurrentSkillState, (skill, state) => (skill, state))
                .Subscribe(pair => OnStateChanged(pair.skill, pair.state))
                .AddTo(this);
        }

        private void Update()
        {
            if (!_isCounting) return;

            _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
            _indicator.SetFill(_duration > 0f ? _remaining / _duration : 0f);

            if (_remaining <= 0f) _isCounting = false;
        }

        private void OnStateChanged(SkillType skill, SkillState state)
        {
            _skill = skill;

            if (_skill == SkillType.None)
            {
                if (_indicator) _indicator.SetVisible(false);
                return;
            }

            EnsureIndicator();
            _indicator.SetVisible(true);
            _indicator.SetIcon(_icons.IconFor(_skill));

            Configure(state);
        }

        private void Configure(SkillState state)
        {
            switch (state)
            {
                case SkillState.Ready:
                    SetStatic(_readyColor);
                    break;
                case SkillState.Active:
                    ConfigureActive();
                    break;
                case SkillState.Cooldown:
                    StartCountdown(_cooldownColor, CooldownDuration());
                    break;
            }
        }

        // Shield's active phase lasts until a hit is absorbed — a full steady ring, distinct from
        // Dash's timed burst.
        private void ConfigureActive()
        {
            if (_skill == SkillType.Shield)
            {
                SetStatic(_shieldColor);
                return;
            }

            StartCountdown(_activeColor, SkillConstants.k_DashDuration);
        }

        private void StartCountdown(Color color, float duration)
        {
            _indicator.SetColor(color);
            _indicator.SetFill(1f);

            _duration   = duration;
            _remaining  = duration;
            _isCounting = true;
        }

        private void SetStatic(Color color)
        {
            _isCounting = false;
            _indicator.SetColor(color);
            _indicator.SetFill(1f);
        }

        private void EnsureIndicator()
        {
            if (!_indicator)
                _indicator = Instantiate(_indicatorPrefab, transform);
        }

        private float CooldownDuration() =>
            _skill == SkillType.Dash
                ? SkillConstants.k_DashCooldownDuration
                : SkillConstants.k_ShieldCooldownDuration;
    }
}
