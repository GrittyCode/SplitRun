using System;

namespace SplitRun.Character
{
    // Bridges runtime-spawned character instances (outside the DI graph) to injected services.
    public static class CharacterEvents
    {
        public static event Action<ICharacter> OnSpawned;
        public static event Action<ICharacter> OnDespawned;

        public static void NotifySpawned(ICharacter character)   => OnSpawned?.Invoke(character);
        public static void NotifyDespawned(ICharacter character) => OnDespawned?.Invoke(character);
    }
}
