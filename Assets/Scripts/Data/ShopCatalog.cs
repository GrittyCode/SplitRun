using System;

using UnityEngine;

using SplitRun.Character;

namespace SplitRun.Data
{
    // Single product database — the one authoring point per character (shop data, lobby model,
    // network prefab) and per hat. Adding either is one enum value, one entry, no code branch.
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "SplitRun/Shop Catalog")]
    public sealed class ShopCatalog : ScriptableObject
    {
        [SerializeField] private ShopCharacterEntry[] _characters;
        [SerializeField] private ShopHatEntry[]       _hats;

        public ShopCharacterEntry[] Characters => _characters;
        public ShopHatEntry[]       Hats       => _hats;

        public ShopCharacterEntry FindCharacter(CharacterType type)
        {
            foreach (ShopCharacterEntry entry in _characters)
            {
                if (entry.Type == type)
                    return entry;
            }

            return null;
        }

        public ShopHatEntry FindHat(HatType type)
        {
            foreach (ShopHatEntry entry in _hats)
            {
                if (entry.Type == type)
                    return entry;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class ShopCharacterEntry
    {
        [SerializeField] private CharacterType   _type;
        [SerializeField] private string          _displayName;
        [SerializeField] private int             _price;
        [SerializeField] private Sprite          _icon;
        [SerializeField] private CharacterModel  _modelPrefab;
        [SerializeField] private ServerCharacter _gamePrefab;

        public CharacterType   Type        => _type;
        public string          DisplayName => _displayName;
        public int             Price       => _price;
        public Sprite          Icon        => _icon;
        public CharacterModel  ModelPrefab => _modelPrefab;
        public ServerCharacter GamePrefab  => _gamePrefab;
    }

    [Serializable]
    public sealed class ShopHatEntry
    {
        [SerializeField] private HatType    _type;
        [SerializeField] private string     _displayName;
        [SerializeField] private int        _price;
        [SerializeField] private Sprite     _icon;
        [SerializeField] private GameObject _hatPrefab;

        public HatType    Type        => _type;
        public string     DisplayName => _displayName;
        public int        Price       => _price;
        public Sprite     Icon        => _icon;
        public GameObject HatPrefab   => _hatPrefab;
    }
}
