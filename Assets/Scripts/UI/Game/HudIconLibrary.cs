using System;

using UnityEngine;

using SplitRun.Character;
using SplitRun.Item;

namespace SplitRun.UI.Game
{
    // Sprite source for HUD indicators, keyed by domain enum. Adding an item or skill icon is a
    // data edit here, never a view change.
    [CreateAssetMenu(fileName = "HudIconLibrary", menuName = "SplitRun/HUD Icon Library")]
    public class HudIconLibrary : ScriptableObject
    {
        [SerializeField] private ItemIcon[]  _itemIcons  = Array.Empty<ItemIcon>();
        [SerializeField] private SkillIcon[] _skillIcons = Array.Empty<SkillIcon>();

        public Sprite IconFor(ItemType type)
        {
            foreach (ItemIcon entry in _itemIcons)
            {
                if (entry.Type == type) return entry.Sprite;
            }

            return null;
        }

        public Sprite IconFor(SkillType type)
        {
            foreach (SkillIcon entry in _skillIcons)
            {
                if (entry.Type == type) return entry.Sprite;
            }

            return null;
        }

        [Serializable]
        private struct ItemIcon
        {
            [SerializeField] private ItemType _type;
            [SerializeField] private Sprite   _sprite;

            public ItemType Type   => _type;
            public Sprite   Sprite => _sprite;
        }

        [Serializable]
        private struct SkillIcon
        {
            [SerializeField] private SkillType _type;
            [SerializeField] private Sprite    _sprite;

            public SkillType Type   => _type;
            public Sprite    Sprite => _sprite;
        }
    }
}
