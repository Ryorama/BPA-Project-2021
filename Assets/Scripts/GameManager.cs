﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TerrainEngine2D;
using System.Text;

namespace Game
{
    public class GameManager : MonoBehaviour
    {

        public List<AudioClip> overworldDayTracks = new List<AudioClip>();
        public List<AudioClip> overworldNightTracks = new List<AudioClip>();

        public static bool isDaytime;

        public SpriteRenderer heldItemPlayer;

        public Text fps;

        public GameObject invObject;
        public bool isInventoryOpen;
        public static World world;

        void Start()
        {
            WorldModifier.groundMaterials.Add(OverwoldTerrainGenerator.MainLayer.Grass);
            WorldModifier.groundMaterials.Add(OverwoldTerrainGenerator.MainLayer.Dirt);

            WorldModifier.stoneMaterials.Add(OverwoldTerrainGenerator.MainLayer.Stone);
        }

        void Awake()
        {
            world = GameObject.Find("World").GetComponent<World>();
        }

        void Update()
        {
            if (ItemWheel.itemSlots[ItemWheel.selectedSlot] != Item.EMPTY)
            {
                heldItemPlayer.sprite = ItemWheel.itemSlots[ItemWheel.selectedSlot].icon;
            } else
            {
                heldItemPlayer.sprite = null;
            }
            
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                if (!isInventoryOpen)
                {
                    invObject.SetActive(true);
                    isInventoryOpen = true;
                }
                else
                {
                    invObject.SetActive(false);
                    isInventoryOpen = false;
                }
            }

            if (world.TimeOfDay > 8 && world.TimeOfDay < 20)
            {
                if (!isDaytime && overworldDayTracks.Count > 0)
                {
                    MusicTicker.ChangeTrack(overworldDayTracks[Random.Range(0, overworldDayTracks.Count - 1)]);
                }

                if (!isDaytime)
                {
                    isDaytime = true;
                }

                if (overworldDayTracks.Count > 0 && !MusicTicker.musicTicker.isPlaying)
                {
                    MusicTicker.ChangeTrack(overworldDayTracks[Random.Range(0, overworldDayTracks.Count - 1)]);
                }
            }
            else
            {
                if (isDaytime && overworldNightTracks.Count > 0)
                {
                    MusicTicker.ChangeTrack(overworldNightTracks[Random.Range(0, overworldNightTracks.Count - 1)]);
                }

                if (isDaytime)
                {
                    isDaytime = false;
                }

                if (overworldNightTracks.Count > 0 && !MusicTicker.musicTicker.isPlaying)
                {
                    MusicTicker.ChangeTrack(overworldNightTracks[Random.Range(0, overworldNightTracks.Count - 1)]);
                }
            }
        }
    }
}