using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class Inventory : MonoBehaviour
    {
        public static List<Item> itemSlots = new List<Item>();
        public List<Image> itemSlotIcons = new List<Image>();

        public Item testItem;
        public Item diamondSword;

        public int selectedSlotId = -1;
        public int oldClickedSlot = -1;
        public int slotClickIndex = 0;
        public bool holdingItem = false;

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
                }
                else
                {
                    itemSlotIcons[item].gameObject.SetActive(false);
                }

                itemSlotIcons[item].GetComponent<Button>();
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
                    }
                    else
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
                }
                else
                {
                    itemSlots[slot].stack -= 1;
                    itemSlots[slot] = Item.EMPTY;
                }
            }
        }

        public void ClickInvSlot(int slotID)
        {
            Debug.Log(slotID);
            Debug.Log(oldClickedSlot);

            if (selectedSlotId != slotID)
            {
                selectedSlotId = slotID;
                slotClickIndex += 1;
                holdingItem = true;
                
                if (slotClickIndex == 0)
                {
                    oldClickedSlot = slotID;
                }
            } else
            {
                holdingItem = false;
                slotClickIndex = 0;
                selectedSlotId = -1;
            }

            if (holdingItem == true && slotClickIndex >= 2)
            {
                itemSlots[slotID].icon = null;
                itemSlots[slotID].stack = 0;

                itemSlots[oldClickedSlot].icon = null;
                itemSlots[oldClickedSlot].stack = 0;

                holdingItem = false;
                slotClickIndex = 0;
                selectedSlotId = -1;
                oldClickedSlot = -1;
            }
        }
    }
}