using UnityEngine;
using UnityEngine.UI;

namespace SplitRun.UI.Game
{
    // Presentation only: an icon and a 0..1 fill with a color. Bar vs radial is an Inspector Image
    // setting, so one component backs both the buff bar and the skill gauge. SetVisible deactivates
    // this object — keep the driving view on a separate always-active parent.
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
