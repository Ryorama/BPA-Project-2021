using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// A type of fluid
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "FluidType", menuName = "Terrain Engine 2D/Fluid Type")]
    public class FluidType : ScriptableObject
    {
        public string Name = "Fluid";
        public Color32 DefaultColor = UnityEngine.Color.blue;
        [HideInInspector]
        public byte Density;
    }
}