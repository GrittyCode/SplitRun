using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.Data;
using SplitRun.UI.Lobby;

namespace SplitRun.Lobby
{
    public class LobbyLifetimeScope : LifetimeScope
    {
        [Header("Scene Components")]
        [SerializeField] private LobbyView            _lobbyView;
        [SerializeField] private MultiplayerPanelView _multiplayerPanelView;
        [SerializeField] private CharacterStageView   _characterStageView;
        [SerializeField] private ShopView             _shopView;
        [SerializeField] private StorageView          _storageView;

        [Header("Data Assets")]
        [SerializeField] private ShopCatalog _shopCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_shopCatalog);

            builder.RegisterComponent(_lobbyView);
            builder.RegisterComponent(_multiplayerPanelView);
            builder.RegisterComponent(_characterStageView);
            builder.RegisterComponent(_shopView);
            builder.RegisterComponent(_storageView);
        }
    }
}
