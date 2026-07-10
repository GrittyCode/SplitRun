using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using R3;

namespace SplitRun.UI.Lobby
{
    public enum CustomizationTab
    {
        Character,
        Hat,
    }

    public abstract class CustomizationGridView : MonoBehaviour
    {
        [Header("Type Tabs")]
        [SerializeField] private Button _characterTabButton;
        [SerializeField] private Button _hatTabButton;

        [Header("Grid")]
        [SerializeField] private Transform             _cardContainer;
        [SerializeField] private CustomizationCardView _cardPrefab;

        private readonly List<CustomizationCardView> _cards = new List<CustomizationCardView>();

        private CustomizationTab _tab = CustomizationTab.Character;

        protected CustomizationTab Tab => _tab;

        protected virtual void Start()
        {
            _characterTabButton.OnClickAsObservable().Subscribe(_ => ShowTab(CustomizationTab.Character)).AddTo(this);
            _hatTabButton.OnClickAsObservable().Subscribe(_ => ShowTab(CustomizationTab.Hat)).AddTo(this);
        }

        protected virtual void OnEnable() => ShowTab(_tab);

        protected abstract void Rebuild();

        protected abstract void OnCardClicked(int index);

        protected virtual void OnTabChanged() { }

        protected void ShowTab(CustomizationTab tab)
        {
            _tab = tab;
            _characterTabButton.interactable = tab != CustomizationTab.Character;
            _hatTabButton.interactable       = tab != CustomizationTab.Hat;

            OnTabChanged();
            Rebuild();
        }

        protected void SetCardCount(int count)
        {
            while (_cards.Count < count)
            {
                int index = _cards.Count;
                CustomizationCardView card = Instantiate(_cardPrefab, _cardContainer);
                card.OnClicked.Subscribe(_ => OnCardClicked(index)).AddTo(this);
                _cards.Add(card);
            }

            for (int i = 0; i < _cards.Count; i++)
                _cards[i].gameObject.SetActive(i < count);
        }

        protected CustomizationCardView CardAt(int index) => _cards[index];
    }
}
