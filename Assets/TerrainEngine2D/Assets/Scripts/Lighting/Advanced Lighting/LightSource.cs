using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.Lighting
{
    /// <summary>
    /// A source of light
    /// </summary>
    public abstract class LightSource : MonoBehaviour
    {
        [Header("Light Properties")]
        /// <summary>
        /// Color of the light source
        /// </summary>
        [Tooltip("Color of the light source")]
        public Color32 LightColor = new Color32(255, 255, 255, 255);

        protected Vector2Int keyPosition;
        /// <summary>
        /// The key position of the light source
        /// This is used as the dictionary key value for referencing light sources in the AdvancedLightSystem script
        /// </summary>
        public Vector2Int KeyPosition
        {
            get { return keyPosition; }
        }

        protected virtual void Awake()
        {
            //The key position is set to the light sources starting position
            keyPosition = new Vector2Int((int)transform.position.x, (int)transform.position.y);
        }

        protected virtual void Start()
        {
            AdvancedLightSystem.Instance.SendMessage("LightAdded", this, SendMessageOptions.RequireReceiver);
        }

        protected virtual void OnDestroy()
        {
            if(AdvancedLightSystem.Instance != null)
                AdvancedLightSystem.Instance.SendMessage("LightRemoved", this, SendMessageOptions.DontRequireReceiver);
        }
    }
}