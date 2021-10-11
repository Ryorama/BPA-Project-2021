using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public List<AudioClip> overworldDayTracks = new List<AudioClip>();
    public GameObject invObject;
    public bool isInventoryOpen;

    void Awake()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (!isInventoryOpen)
            {
                invObject.SetActive(true);
                isInventoryOpen = true;
            } else
            {
                invObject.SetActive(false);
                isInventoryOpen = false;
            }
        }

        if (overworldDayTracks.Count > 0 && !MusicTicker.musicTicker.isPlaying)
        {
            MusicTicker.ChangeTrack(overworldDayTracks[Random.Range(0, overworldDayTracks.Count - 1)]);
        }
    }
}
