using UnityEngine;
using UnityEngine.UI;

namespace SplitRun.UI.Game
{
    public class HPBarView : MonoBehaviour
    {
        [SerializeField] private Image[] _hearts;
        [SerializeField] private Sprite _fullHeart;
        [SerializeField] private Sprite _emptyHeart;

        /// <summary>Sets each heart full or empty so the filled count matches the given HP.</summary>
        public void Refresh(int hp)
        {
            for (int i = 0; i < _hearts.Length; i++)
                _hearts[i].sprite = i < hp ? _fullHeart : _emptyHeart;
        }
    }
}
