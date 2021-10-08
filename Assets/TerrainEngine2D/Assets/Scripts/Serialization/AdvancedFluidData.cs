using System;
using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Advanced Fluid Data to be saved to file
    /// </summary>
    [Serializable]
    public class AdvancedFluidData : SaveData
    {
        [SerializeField]
        private float[,] fluidWeight;
        /// <summary>
        /// Storage for all the fluid weight data
        /// </summary>
        public float[,] FluidWeight
        {
            get { return fluidWeight; }
        }

        [SerializeField]
        private byte[,] fluidDensity;
        public byte[,] FluidDensity
        {
            get { return fluidDensity; }
        }

        [SerializeField]
        private Color32[,] fluidColor;
        public Color32[,] FluidColor
        {
            get { return fluidColor; }
        }

        public AdvancedFluidData(AdvancedFluidDynamics advancedFluidDynamics) : base()
        {
            fluidWeight = new float[worldData.WorldWidth, worldData.WorldHeight];
            for (int x = 0; x < advancedFluidDynamics.FluidBlocksInternal.GetLength(0); x++)
            {
                for (int y = 0; y < advancedFluidDynamics.FluidBlocksInternal.GetLength(1); y++)
                {
                    fluidWeight[x, y] = advancedFluidDynamics.FluidBlocksInternal[x, y].Weight;
                }
            }
            fluidDensity = new byte[worldData.WorldWidth, worldData.WorldHeight];
            for (int x = 0; x < advancedFluidDynamics.FluidBlocksInternal.GetLength(0); x++)
            {
                for (int y = 0; y < advancedFluidDynamics.FluidBlocksInternal.GetLength(1); y++)
                {
                    fluidDensity[x, y] = ((AdvancedFluidBlock)advancedFluidDynamics.FluidBlocksInternal[x, y]).Density;
                }
            }
            fluidColor = new Color32[worldData.WorldWidth, worldData.WorldHeight];
            for (int x = 0; x < advancedFluidDynamics.FluidBlocksInternal.GetLength(0); x++)
            {
                for (int y = 0; y < advancedFluidDynamics.FluidBlocksInternal.GetLength(1); y++)
                {
                    fluidColor[x, y] = ((AdvancedFluidBlock)advancedFluidDynamics.FluidBlocksInternal[x, y]).Color;
                }
            }
        }
    }
}