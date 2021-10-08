using System;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainEngine2D
{
    [Serializable]
    /// <summary>
    /// Base data to be saved to file
    /// </summary>
    public class BaseData
    {
        /// <summary>
        /// The guid of the World Data Object used to generate this file
        /// </summary>
        public string SID;
        /// <summary>
        /// The name of the world
        /// </summary>
        public string Name;
        /// <summary>
        /// The width of the world in blocks
        /// </summary>
        public int Width;
        /// <summary>
        /// The height of the world in blocks
        /// </summary>
        public int Height;
        /// <summary>
        /// The seed used to generate the world
        /// </summary>
        public int Seed;

        /// <summary>
        /// Default constructor for BlockData
        /// Holds all the base data for saving
        /// </summary>
        public BaseData() { }

        public BaseData(WorldData worldData)
        {
            SID = worldData.GUID;
            Name = worldData.Name;
            Width = worldData.WorldWidth;
            Height = worldData.WorldHeight;
            Seed = worldData.Seed;
        }
    }
}