using UnityEngine;
using System;

// Copyright (C) 2020 Matthew Wilson
//
// This is part of a derivative work and portions of this file are licensed under the MIT
//     open source license (https://opensource.org/licenses/MIT)
// Please refer to the Third-Party Notices.txt for more information
//
// References (Refer to Third-Party Notices.txt for all Third Party Licenses):
// https://w-shadow.com/blog/2009/09/01/simple-fluid-simulation/
// http://www.jgallant.com/2d-liquid-simulator-with-cellular-automaton-in-unity/

namespace TerrainEngine2D
{
    [Serializable]
    /// <summary>
    /// Information about the fluid contained in a block of the world
    /// Advanced fluid also contain a color and density (type)
    /// </summary>
    public class AdvancedFluidBlock : FluidBlock
    {
        /// <summary>
        /// Adjacent top FluidBlock
        /// </summary>
        public AdvancedFluidBlock TopBlock;
        /// <summary>
        /// Adjacent bottom FluidBlock
        /// </summary>
        public AdvancedFluidBlock BottomBlock;
        /// <summary>
        /// Adjacent left FluidBlock
        /// </summary>
        public AdvancedFluidBlock LeftBlock;
        /// <summary>
        /// Adjacent right FluidBlock
        /// </summary>
        public AdvancedFluidBlock RightBlock;
        
        /// <summary>
        /// The color of the liquid 
        /// </summary>
        public Color32 Color;

        /// <summary>
        /// The type of liquid
        /// </summary>
        public byte Density;

        /// <summary>
        /// Adds liquid of a certain colour and density to the fluid block
        /// This will change the density (type) of this block to the given value
        /// </summary>
        /// <param name="density">The density (type) of the fluid</param>
        /// <param name="amount">Amount of fluid to add</param>
        /// <param name="color">The color of the fluid</param>
        public void AddWeight(byte density, float amount, Color32 color)
        {
            Weight += amount;
            Stable = false;
            Density = density;
            if (Color.Equals(AdvancedFluidDynamics.ClearColor))
                Color = color;
            else
                Color = Color32.Lerp(Color, color, amount / Weight);
        }
        
        /// <summary>
        /// Set all adjacent blocks to unstable
        /// </summary>
        public override void UnsettleNeighbours()
        {
            if (TopBlock != null)
                TopBlock.Stable = false;
            if (BottomBlock != null)
                BottomBlock.Stable = false;
            if (LeftBlock != null)
                LeftBlock.Stable = false;
            if (RightBlock != null)
                RightBlock.Stable = false;
        }
        
        /// <summary>
        /// Get the height of the fluid block (for mesh generation)
        /// </summary>
        /// <returns>The height</returns>
        public override float GetHeight()
        {
            //Set falling blocks as full
            if (TopBlock != null && TopBlock.Weight > 0 && TopBlock.Density == Density)
                return 1;
            return base.GetHeight();
        }
    }

}