using TMPro;
using UnityEngine;
using UnityEngine.UI;

using R3;
using VContainer;

using SplitRun.Game;
using SplitRun.Item;

namespace SplitRun.UI.Game
{
    public class GameHUDView : MonoBehaviour
    {
        [SerializeField] private TMP_Text  _distanceLabel;
        [SerializeField] private TMP_Text  _coinLabel;
        [SerializeField] private HPBarView _hpBar;
        [SerializeField] private Button    _pauseButton;

        [Inject] private GameService _gameService;
        [Inject] private ItemService _itemService;

        private void Start()
        {
            BindObservables();
        }

        private void BindObservables()
        {
            // Whole-meter gating keeps the label from allocating a string every frame.
            _gameService.CurrentDistance
                .Select(distance => (int)distance)
                .DistinctUntilChanged()
                .Subscribe(distance => _distanceLabel.text = $"{distance}m")
                .AddTo(this);

            _gameService.CurrentHp
                .Subscribe(hp => _hpBar.Refresh(hp))
                .AddTo(this);

            _itemService.Coins
                .Subscribe(coins => _coinLabel.text = $"{coins}")
                .AddTo(this);

            _pauseButton.OnClickAsObservable()
                .Subscribe(_ => _gameService.RequestPause())
                .AddTo(this);
        }
    }
}
