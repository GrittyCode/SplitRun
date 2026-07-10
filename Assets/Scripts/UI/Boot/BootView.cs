using TMPro;
using UnityEngine;
using UnityEngine.UI;

using R3;
using VContainer;

using SplitRun.Boot;

namespace SplitRun.UI.Boot
{
    public class BootView : MonoBehaviour
    {
        [SerializeField] private Image    _progressFill;
        [SerializeField] private TMP_Text _statusLabel;

        [Inject] private BootLoader _bootLoader;

        private void Start()
        {
            _bootLoader.Progress
                .Subscribe(progress => _progressFill.fillAmount = progress)
                .AddTo(this);

            _bootLoader.Status
                .Subscribe(status => _statusLabel.text = status)
                .AddTo(this);
        }
    }
}
