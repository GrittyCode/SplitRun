using UnityEngine;

namespace SplitRun.Character
{
    // Single point that hands character prefabs into the DI graph, so no scene object holds them.
    [CreateAssetMenu(fileName = "CHR_Catalog", menuName = "SplitRun/Character Catalog")]
    public sealed class CharacterCatalog : ScriptableObject
    {
        [SerializeField] private ServerCharacter _defaultPrefab;
        [SerializeField] private ServerCharacter _shieldPrefab;
        [SerializeField] private ServerCharacter _dashPrefab;

        public ServerCharacter Resolve(CharacterType type) => type switch
        {
            CharacterType.Shield => _shieldPrefab,
            CharacterType.Dash   => _dashPrefab,
            _                    => _defaultPrefab,
        };
    }
}
