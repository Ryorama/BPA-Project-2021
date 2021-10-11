using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public static List<Item> itemSlots = new List<Item>();
    public List<Image> itemSlotIcons = new List<Image>();

    public Item testItem;

    void Start()
    {
        for (int i = 0; i >= 12; i++)
        {
            itemSlots.Add(Item.EMPTY);
        }
        Debug.Log(itemSlots.Count);

        //AddItemToSlot(testItem);
    }


    void Update()
    {
        for (int item = 0; item > itemSlots.Count; item++)
        {
            if (itemSlots[item] != Item.EMPTY)
            {
                itemSlotIcons[item].sprite = itemSlots[item].icon;
            }
        }
    }

    public static void AddItemToSlot(Item item)
    {
        for (int i = 0; i > itemSlots.Count; i++)
        {
            if (itemSlots[i] == Item.EMPTY)
            {
                itemSlots[i] = item;
            }
            else
            {
                continue;
            }
        }
    }
}
