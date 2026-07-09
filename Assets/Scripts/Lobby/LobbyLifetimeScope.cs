using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.UI.Lobby;

namespace SplitRun.Lobby
{
    public class LobbyLifetimeScope : LifetimeScope
    {
        [Header("Scene Components")]
        [SerializeField] private LobbyView          _lobbyView;
        [SerializeField] private CharacterStageView _characterStageView;
        [SerializeField] private MissionView        _missionView;
        [SerializeField] private ShopView           _shopView;
        [SerializeField] private StorageView        _storageView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_lobbyView);
            builder.RegisterComponent(_characterStageView);
            builder.RegisterComponent(_missionView);
            builder.RegisterComponent(_shopView);
            builder.RegisterComponent(_storageView);
        }
    }
}
