using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public static List<Item> itemSlots = new List<Item>();
    public List<Image> itemSlotIcons = new List<Image>();

    public Item testItem;
    public Item diamondSword;

    void Start()
    {
        for (int i = 0; i < 12; i++)
        {
            itemSlots.Add(Item.EMPTY);
        }
        Debug.Log(itemSlots.Count);

        AddItemToSlot(testItem, 5);
        AddItemToSlot(diamondSword, 1);
    }


    void Update()
    {
        for (int item = 0; item < itemSlots.Count; item++)
        {
            if (itemSlots[item] != Item.EMPTY)
            {
                itemSlotIcons[item].gameObject.SetActive(true);
                itemSlotIcons[item].sprite = itemSlots[item].icon;
            } else
            {
                itemSlotIcons[item].gameObject.SetActive(false);
            }
        }
    }

    public static void AddItemToSlot(Item item, int amnt)
    {
        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (itemSlots[i] == Item.EMPTY)
            {
                itemSlots[i] = item;
                itemSlots[i].stack += amnt;
                break;
            }
            else
            {
                if (itemSlots[i].stack > 0 && itemSlots[i] == item)
                {
                    itemSlots[i].stack += amnt;
                } else
                {
                    continue;
                }
            }
        }
    }

    public static void RemoveItemFromSlot(int slot, int amnt)
    {
        if (itemSlots[slot] != Item.EMPTY)
        {
            if (itemSlots[slot].stack > 1)
            {
                itemSlots[slot].stack -= amnt;
            } else
            {
                itemSlots[slot].stack -= 1;
                itemSlots[slot] = Item.EMPTY;
            }
        }
    }
}
