using System.Globalization;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

using R3;

using SplitRun.Data;

namespace SplitRun.UI.Lobby
{
    // Presentation-only row; the owning MissionView binds state and handles the claim.
    public class MissionRowView : MonoBehaviour
    {
        private const string k_RewardFormat   = "+{0}";
        private const string k_ProgressFormat = "{0} / {1}";

        [Header("Content")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private Image    _progressFill;
        [SerializeField] private TMP_Text _rewardText;

        [Header("State")]
        [SerializeField] private Button     _claimButton;
        [SerializeField] private GameObject _dim;

        public Observable<Unit> OnClaimClicked => _claimButton.OnClickAsObservable();

        public void Bind(MissionState mission)
        {
            _nameText.text = mission.Definition.DisplayName;
            _progressText.text = string.Format(
                CultureInfo.InvariantCulture, k_ProgressFormat, mission.Progress, mission.Definition.Target);
            _progressFill.fillAmount = mission.NormalizedProgress;
            _rewardText.text = string.Format(
                CultureInfo.InvariantCulture, k_RewardFormat, mission.Definition.RewardCoins);

            // The button stays visible so the reward is always previewed; only a claimable mission can act.
            _claimButton.interactable = mission.IsClaimable;

            // A claimed mission is greyed out to read as spent for the rest of the day.
            _dim.SetActive(mission.Claimed);
        }
    }
}
