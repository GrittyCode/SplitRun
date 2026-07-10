using UnityEngine;

using SplitRun.Character;
using SplitRun.Item;
using SplitRun.Utility;

namespace SplitRun.UI.Game
{
    [CreateAssetMenu(fileName = "HudIconLibrary", menuName = "SplitRun/HUD Icon Library")]
    public class HudIconLibrary : ScriptableObject
    {
        [SerializeField] private EnumKeyedArray<ItemType, Sprite>  _itemIcons  = new EnumKeyedArray<ItemType, Sprite>();
        [SerializeField] private EnumKeyedArray<SkillType, Sprite> _skillIcons = new EnumKeyedArray<SkillType, Sprite>();

        public Sprite IconFor(ItemType type) => _itemIcons[type];

        public Sprite IconFor(SkillType type) => _skillIcons[type];
    }
}
