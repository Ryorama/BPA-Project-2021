using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Game/Items/New Item", order = 1)]
    public class Item : ScriptableObject
    {
        public static Item EMPTY = null;

        public Sprite icon;
        public ItemType itemType;
        public int stack;
        public string itemName;
        public string tooltip;
    }
}