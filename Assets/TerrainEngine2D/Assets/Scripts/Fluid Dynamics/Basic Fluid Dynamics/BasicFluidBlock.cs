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
    /// </summary>
    public class BasicFluidBlock : FluidBlock
    {
        //**Optimized for use in the editor**
        /// <summary>
        /// Adjacent top FluidBlock
        /// </summary>
        public BasicFluidBlock TopBlock;
        /// <summary>
        /// Adjacent bottom FluidBlock
        /// </summary>
        public BasicFluidBlock BottomBlock;
        /// <summary>
        /// Adjacent left FluidBlock
        /// </summary>
        public BasicFluidBlock LeftBlock;
        /// <summary>
        /// Adjacent right FluidBlock
        /// </summary>
        public BasicFluidBlock RightBlock;

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
            if (TopBlock != null && TopBlock.Weight > 0)
                return 1;
            return base.GetHeight();
        }
    }

}