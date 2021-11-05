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

        void Start()
        {
            for (int i = 0; i < 8; i++)
            {
                itemSlots.Add(Item.EMPTY);
            }
            Debug.Log(itemSlots.Count);
        }


        void Update()
        {
            for (int slot = 0; slot < itemSlots.Count; slot++)
            {
                if (itemSlots[slot] != Item.EMPTY)
                {
                    itemSlotIcons[slot].gameObject.SetActive(true);
                } else
                {
                    itemSlotIcons[slot].gameObject.SetActive(false);
                }
                Debug.Log(slot);
            }
        }
    }
}