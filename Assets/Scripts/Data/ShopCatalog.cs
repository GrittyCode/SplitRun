using System;

using UnityEngine;

using SplitRun.Character;
using SplitRun.Utility;

namespace SplitRun.Data
{
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
    public abstract class ShopEntry
    {
        public const string k_IconField = "_icon";

        [SerializeField] private string _displayName;
        [SerializeField] private int    _price;
        [SerializeField] private Sprite _icon;

        public string DisplayName => _displayName;
        public int    Price       => _price;
        public Sprite Icon        => _icon;
    }

    [Serializable]
    public sealed class ShopCharacterEntry : ShopEntry
    {
        public const string k_ModelPrefabField = "_modelPrefab";

        [SerializeField] private CharacterModel   _modelPrefab;
        [SerializeField] private NetworkCharacter _gamePrefab;

        public CharacterModel   ModelPrefab => _modelPrefab;
        public NetworkCharacter GamePrefab  => _gamePrefab;
    }

    [Serializable]
    public sealed class ShopHatEntry : ShopEntry
    {
        public const string k_HatPrefabField = "_hatPrefab";

        [SerializeField] private GameObject _hatPrefab;

        public GameObject HatPrefab => _hatPrefab;
    }
}
