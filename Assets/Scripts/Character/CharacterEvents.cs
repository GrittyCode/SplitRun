using System;

namespace SplitRun.Character
{
    // Static event hub that bridges dynamically spawned character instances
    // into the VContainer dependency graph.
    // ICharacter implementations call Notify* — services subscribe to On*.
    public static class CharacterEvents
    {
        public static event Action<ICharacter> OnSpawned;
        public static event Action<ICharacter> OnDespawned;

        public static void NotifySpawned(ICharacter character)   => OnSpawned?.Invoke(character);
        public static void NotifyDespawned(ICharacter character) => OnDespawned?.Invoke(character);
    }
}
