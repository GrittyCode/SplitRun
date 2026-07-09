using System;

namespace SplitRun.Character
{
    // Bridges runtime-spawned characters (outside the DI graph) to injected services.
    public static class CharacterEvents
    {
        private static Action<ICharacter> s_onSpawned;
        private static Action<ICharacter> s_onDespawned;

        public static ICharacter Current { get; private set; }

        // A subscriber arriving after the NGO-driven spawn must still receive the character.
        public static event Action<ICharacter> OnSpawned
        {
            add
            {
                s_onSpawned += value;
                if (Current != null) value(Current);
            }
            remove { s_onSpawned -= value; }
        }

        public static event Action<ICharacter> OnDespawned
        {
            add    { s_onDespawned += value; }
            remove { s_onDespawned -= value; }
        }

        public static void NotifySpawned(ICharacter character)
        {
            Current = character;
            s_onSpawned?.Invoke(character);
        }

        public static void NotifyDespawned(ICharacter character)
        {
            if (Current == character)
                Current = null;

            s_onDespawned?.Invoke(character);
        }
    }
}
