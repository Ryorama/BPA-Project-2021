using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTicker : MonoBehaviour
{

    public static AudioClip currentTrack;
    public static AudioSource musicTicker;

    void Awake()
    {
        musicTicker = GameObject.Find("WorldMusic").GetComponent<AudioSource>();
    }

    public static void ChangeTrack(AudioClip clip)
    {
        if (currentTrack != clip)
        {
            musicTicker.Stop();
            currentTrack = clip;
            musicTicker.clip = currentTrack;
            musicTicker.Play();
        }
    }

    public static void StopMusic()
    {
        musicTicker.Stop();
    }

    public static void PlayMusic()
    {
        musicTicker.Play();
    }
}
