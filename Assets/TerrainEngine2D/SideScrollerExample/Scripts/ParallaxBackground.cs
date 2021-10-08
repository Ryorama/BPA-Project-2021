using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.SideScrollerDemo
{
    /// <summary>
    /// A parallaxing background
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxBackground : MonoBehaviour
    {
        [HideInInspector]
        public ParallaxBackground leftBackground;
        [HideInInspector]
        public ParallaxBackground rightBackground;

        private SpriteRenderer spriteRenderer;
        public SpriteRenderer SpriteRenderer
        {
            get { return spriteRenderer; }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}