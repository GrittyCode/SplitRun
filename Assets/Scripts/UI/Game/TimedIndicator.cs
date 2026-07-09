using UnityEngine;
using UnityEngine.UI;

namespace SplitRun.UI.Game
{
    // SetVisible deactivates this GameObject, so the driving view must sit on an always-active parent.
    public class TimedIndicator : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _fill;

        public void SetVisible(bool isVisible) => gameObject.SetActive(isVisible);

        public void SetIcon(Sprite sprite)
        {
            if (sprite) _icon.sprite = sprite;
        }

        public void SetColor(Color color) => _fill.color = color;

        public void SetFill(float ratio) => _fill.fillAmount = Mathf.Clamp01(ratio);
    }
}
