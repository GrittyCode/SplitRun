using TMPro;
using UnityEngine;

using R3;
using VContainer;
using SplitRun.Game;

namespace SplitRun.UI.Game
{
    public class GameHUDView : MonoBehaviour
    {
        [SerializeField] private TMP_Text  _distanceLabel;
        [SerializeField] private HPBarView _hpBar;

        [Inject] private GameService _gameService;

        private void Start()
        {
            BindObservables();
        }

        private void BindObservables()
        {
            _gameService.CurrentDistance
                .Select(distance => $"{distance:F0}m")
                .Subscribe(text => _distanceLabel.text = text)
                .AddTo(this);

            _gameService.CurrentHp
                .Subscribe(hp => _hpBar.Refresh(hp))
                .AddTo(this);
        }
    }
}
