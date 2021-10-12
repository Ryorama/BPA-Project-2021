using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Items/New Item", order = 1)]
public class Item : ScriptableObject
{
    public static Item EMPTY = null;

    public Sprite icon;
    public int stack;
    public string itemName;
    public string tooltip;
}
