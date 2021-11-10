using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class ItemWheel : MonoBehaviour
    {
        public static List<Item> itemSlots = new List<Item>();
        public List<Image> itemSlotIcons = new List<Image>();

        public Image selectedItemIcon;

        public Item wtf;
        public Item pickaxe;
        public Item shovel;

        public static int selectedSlot = 0;

        void Start()
        {
            for (int i = 0; i < 8; i++)
            {
                itemSlots.Add(Item.EMPTY);
                Debug.Log(i);
            }
            AddItemToSlot(wtf, 5);
            AddItemToSlot(pickaxe, 1);
            AddItemToSlot(shovel, 1);
        }


        void Update()
        {
            if (itemSlots[selectedSlot] != Item.EMPTY)
            {
                selectedItemIcon.gameObject.SetActive(true);
                selectedItemIcon.sprite = itemSlotIcons[selectedSlot].sprite;
            } else
            {
                selectedItemIcon.sprite = null;
                selectedItemIcon.gameObject.SetActive(false);
            }

            if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                if (selectedSlot > 0)
                {
                    selectedSlot--;
                }
                else
                {
                    selectedSlot = 7;
                }
            }

            if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                if (selectedSlot < 7)
                {
                    selectedSlot++;
                }
                else
                {
                    selectedSlot = 0;
                }
            }

            for (int slot = 0; slot < itemSlots.Count; slot++)
            {
                if (selectedSlot == slot)
                {
                    itemSlotIcons[slot].gameObject.SetActive(true);

                    if (itemSlots[slot] != Item.EMPTY)
                    {
                        itemSlotIcons[slot].sprite = itemSlots[slot].icon;
                    } else
                    {
                        itemSlotIcons[slot].sprite = null;
                    }
                }
                else
                {
                    itemSlotIcons[slot].gameObject.SetActive(false);
                }
                Debug.Log(slot);
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
    }
}