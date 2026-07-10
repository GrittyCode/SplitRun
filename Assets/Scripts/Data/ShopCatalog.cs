using System;

using UnityEngine;

using SplitRun.Character;
using SplitRun.Utility;

namespace SplitRun.Data
{
    // Single product database — one authoring point per character (shop data, lobby model, network
    // prefab) and per hat. The array index is the enum value, so no entry carries its own type.
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "SplitRun/Shop Catalog")]
    public sealed class ShopCatalog : ScriptableObject
    {
        public const string k_CharactersField = "_characters";
        public const string k_HatsField       = "_hats";

        [SerializeField] private EnumKeyedArray<CharacterType, ShopCharacterEntry> _characters =
            new EnumKeyedArray<CharacterType, ShopCharacterEntry>();

        [SerializeField] private EnumKeyedArray<HatType, ShopHatEntry> _hats =
            new EnumKeyedArray<HatType, ShopHatEntry>();

        public EnumKeyedArray<CharacterType, ShopCharacterEntry> Characters => _characters;
        public EnumKeyedArray<HatType, ShopHatEntry>             Hats       => _hats;

        public ShopCharacterEntry FindCharacter(CharacterType type) => _characters[type];

        public ShopHatEntry FindHat(HatType type) => _hats[type];
    }

    [Serializable]
    public sealed class ShopCharacterEntry
    {
        public const string k_IconField        = "_icon";
        public const string k_ModelPrefabField = "_modelPrefab";

        [SerializeField] private string          _displayName;
        [SerializeField] private int             _price;
        [SerializeField] private Sprite          _icon;
        [SerializeField] private CharacterModel  _modelPrefab;
        [SerializeField] private ServerCharacter _gamePrefab;

        public string          DisplayName => _displayName;
        public int             Price       => _price;
        public Sprite          Icon        => _icon;
        public CharacterModel  ModelPrefab => _modelPrefab;
        public ServerCharacter GamePrefab  => _gamePrefab;
    }

    [Serializable]
    public sealed class ShopHatEntry
    {
        public const string k_IconField      = "_icon";
        public const string k_HatPrefabField = "_hatPrefab";

        [SerializeField] private string     _displayName;
        [SerializeField] private int        _price;
        [SerializeField] private Sprite     _icon;
        [SerializeField] private GameObject _hatPrefab;

        public string     DisplayName => _displayName;
        public int        Price       => _price;
        public Sprite     Icon        => _icon;
        public GameObject HatPrefab   => _hatPrefab;
    }
}
