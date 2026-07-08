using System.Collections.Generic;

using UnityEngine;

using R3;
using VContainer;

using SplitRun.Data;

namespace SplitRun.UI.Lobby
{
    // Renders the daily mission set and routes claims. Rebuilds when the panel opens and after a claim;
    // progress itself only changes during runs, when this view is not present.
    public class MissionView : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private Transform      _rowContainer;
        [SerializeField] private MissionRowView _rowPrefab;

        [Inject] private MissionService _missionService;

        private readonly List<MissionRowView> _rows = new List<MissionRowView>();

        private void OnEnable()
        {
            _missionService.RefreshIfNewDay();
            Rebuild();
        }

        private void Rebuild()
        {
            IReadOnlyList<MissionState> missions = _missionService.Missions;
            SetRowCount(missions.Count);

            for (int i = 0; i < missions.Count; i++)
                _rows[i].Bind(missions[i]);
        }

        // Grows the pool as needed, activates the first count rows, and hides the rest.
        private void SetRowCount(int count)
        {
            while (_rows.Count < count)
            {
                int index = _rows.Count;
                MissionRowView row = Instantiate(_rowPrefab, _rowContainer);
                row.OnClaimClicked.Subscribe(_ => OnClaim(index)).AddTo(this);
                _rows.Add(row);
            }

            for (int i = 0; i < _rows.Count; i++)
                _rows[i].gameObject.SetActive(i < count);
        }

        private void OnClaim(int index)
        {
            IReadOnlyList<MissionState> missions = _missionService.Missions;
            if (index >= missions.Count)
                return;

            if (_missionService.TryClaim(missions[index].Definition.Id))
                Rebuild();
        }
    }
}
