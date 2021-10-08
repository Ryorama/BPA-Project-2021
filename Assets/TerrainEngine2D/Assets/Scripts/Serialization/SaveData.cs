using System;
using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Data to be saved to file
    /// </summary>
    [Serializable]
    public class SaveData
    {
        [NonSerialized]
        protected WorldData worldData;

        public SaveData()
        {
            worldData = World.WorldData;
        }
    }
}