using Unity.Netcode;
using VContainer.Unity;

namespace SplitRun.Game
{
    public class GameEntryPoint : IStartable
    {
        public void Start()
        {
            // Guard allows LocalCharacter to be used without NetworkManager in the scene.
            // TODO(netcode): use NetworkService.CreateRoomAsync() instead of StartHost
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.StartHost();
        }
    }
}
