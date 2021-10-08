using UnityEngine;

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
    /// <summary>
    /// Information about the fluid contained in a block of the world
    /// </summary>
    public abstract class FluidBlock
    {
        /// <summary>
        /// Weight used for representing solid blocks
        /// </summary>
        public const int SolidWeight = -100;
        
        /// <summary>
        /// Amount of liquid in the block
        /// </summary>
        public float Weight;

        /// <summary>
        /// If the fluid has settled
        /// </summary>
        public bool Stable;

        /// <summary>
        /// Check if fluid block is solid
        /// </summary>
        /// <returns>Returns true if the block is solid</returns>
        public bool IsSolid()
        {
            return Weight == SolidWeight;
        }

        /// <summary>
        /// Sets fluid block to solid
        /// </summary>
        public void SetSolid()
        {
            Weight = SolidWeight;
            UnsettleNeighbours();
        }

        /// <summary>
        /// Empties fluid block
        /// </summary>
        public void SetEmpty()
        {
            Weight = 0;
            Stable = false;
            UnsettleNeighbours();
        }

        /// <summary>
        /// Adds liquid to the fluid block
        /// </summary>
        /// <param name="amount">Amount of fluid to add</param>
        public void AddWeight(float amount)
        {
            Weight += amount;
            Stable = false;
        }

        /// <summary>
        /// Set all adjacent blocks to unstable
        /// </summary>
        public abstract void UnsettleNeighbours();

        /// <summary>
        /// Get the height of the fluid block (for mesh generation)
        /// </summary>
        /// <returns>The height</returns>
        public virtual float GetHeight()
        {
            //Height is set based on the amount of fluid
            float height = Mathf.Min(1, Weight);
            return height;
        }
    }
}