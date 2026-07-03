using UnityEngine;

using VContainer;
using VContainer.Unity;

using SplitRun.UI.Lobby;

namespace SplitRun.Lobby
{
    public class LobbyLifetimeScope : LifetimeScope
    {
        [Header("Scene Components")]
        [SerializeField] private LobbyView            _lobbyView;
        [SerializeField] private MultiplayerPanelView _multiplayerPanelView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_lobbyView);
            builder.RegisterComponent(_multiplayerPanelView);
        }
    }
}
