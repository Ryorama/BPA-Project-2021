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
    /// Advanced Fluid physics system allowing multiple different fluid types
    /// </summary>
    /// <remarks>The fluid dynamics scripts contain repeat code in order to maximize performance by reducing casting</remarks>
    [DisallowMultipleComponent]
    public class AdvancedFluidDynamics : FluidDynamics {
        /// <summary>
        /// A Color32 representing a clear color (transparent, lacking color)
        /// </summary>
        public static Color32 ClearColor = new Color32();
        
        //The fluid information for each block in the world
        protected AdvancedFluidBlock[,] fluidBlocks;
        
        //Allow fluids of different densities to mix in order to fill in the top surface layer of fluid (the added fluid is converted to the base)
        private bool allowSurfaceFilling;
        //The factor effecting how two fluids mix together, a higher factor results in a more drastic color change
        private float fluidMixingFactor = 0.1f;

        /// <summary>
        /// Set the instance of the fluid dynamics object
        /// </summary>
        protected override void SetInstance()
        {
            Instance = this;
        }
        
        /// <summary>
        /// Allocate memory for advanced fluid blocks
        /// </summary>
        protected override void AllocFluidBlocks()
        {
            fluidBlocks = new AdvancedFluidBlock[worldData.WorldWidth, worldData.WorldHeight];
            for (int x = 0; x < worldData.WorldWidth; x++)
            {
                for (int y = 0; y < worldData.WorldHeight; y++)
                {
                    fluidBlocks[x, y] = new AdvancedFluidBlock();
                }
            }
            //Sets the adjacent blocks for each fluid block
            for (int x = 0; x < worldData.WorldWidth; x++)
            {
                for (int y = 0; y < worldData.WorldHeight; y++)
                {
                    //Sets adjacent blocks that are within the world bounds
                    fluidBlocks[x, y].TopBlock = (AdvancedFluidBlock)GetFluidBlock(x, y + 1);
                    fluidBlocks[x, y].BottomBlock = (AdvancedFluidBlock)GetFluidBlock(x, y - 1);
                    fluidBlocks[x, y].LeftBlock = (AdvancedFluidBlock)GetFluidBlock(x - 1, y);
                    fluidBlocks[x, y].RightBlock = (AdvancedFluidBlock)GetFluidBlock(x + 1, y);
                }
            }

            fluidBlocksInternal = fluidBlocks;
        }

        /// <summary>
        /// Sets the fluid properties from the world data file
        /// </summary>
        /// <param name="worldData">The data file containing the fluid properties</param>
        public override void UpdateProperties(WorldData worldData)
        {
            base.UpdateProperties(worldData);
            allowSurfaceFilling = worldData.AllowSurfaceFilling;
            fluidMixingFactor = worldData.FluidMixingFactor;
        }
        
        
        /// <summary>
        /// Updates the fluid blocks and runs the fluid simulation
        /// </summary>
        /// <param name="startPosition">The starting position of the loaded world</param>
        /// <param name="endPosition">The end position of the loaded world</param>
        protected override void SimulateFluid(Vector2Int startPosition, Vector2Int endPosition)
        {
            updateFluid = false;
            //Sets the coordinates for looping through the fluid blocks
            //Loops through all the loaded fluid blocks
            for (int x = startPosition.x; x < endPosition.x; x++)
            {
                for (int y = startPosition.y; y < endPosition.y; y++)
                {
                    //Gets the current fluid block
                    FluidBlock fluidBlock = fluidBlocks[x, y];
                    //Skips the block if it has settled or it has less liquid than the minimum
                    if (fluidBlock.Weight > minWeight && !fluidBlock.Stable)
                    {
                        //Calculate the liquid flow
                        if (topDown)
                            TopDownFlow(x, y, fluidBlock);
                        else
                            DownFlow(x, y, fluidBlock);
                    }
                }
            }
            //Second loop for setting values
            for (int x = startPosition.x; x < endPosition.x; x++)
            {
                for (int y = startPosition.y; y < endPosition.y; y++)
                {
                    FluidBlock fluidBlock = fluidBlocks[x, y];
                    //Skips the current block if it is solid
                    if (fluidBlock.Weight == FluidBlock.SolidWeight)
                        continue;
                    //Applies the change to the fluid weight
                    fluidBlock.Weight += fluidDifference[x, y];
                    //Updates the chunk if there is substancial difference in the block's fluid
                    if (fluidDifference[x, y] > stableAmount || fluidDifference[x, y] < -stableAmount)
                    {
                        if(renderFluidTexture)
                            fluidRenderer.UpdateTexture();
                        else
                            chunkLoader.UpdateChunk(x, y, true);
                    }

                    float weight = fluidBlock.Weight;
                    //Empty the block if it has lower than the minimum amount of fluid and it is not already empty or solid
                    if (weight < minWeight && weight > 0)
                    {
                        if (!topDown)
                            fluidBlock.SetEmpty();
  
                        //Unsettle the block and adjacent blocks if it was just emptied
                    }
                    else if (weight == 0 && fluidDifference[x, y] != 0)
                    {
                        fluidBlock.UnsettleNeighbours();
                        fluidBlock.Stable = false;
                    }
                    //Resets the fluid difference
                    fluidDifference[x, y] = 0;
                    //If the fluid block is not stable and still has liquid continue to run the simulation
                    if (!fluidBlock.Stable && weight > minWeight)
                        updateFluid = true;
                }
            }
            
        }

        /// <summary>
        /// Calculates the movement of fluid in a fluid block for a top-down simulation
        /// </summary>
        /// <param name="x">X coordinate of the fluid block</param>
        /// <param name="y">Y coordinate of the fluid block</param>
        /// <param name="fluidBlock">Reference to the fluid block</param>
        protected override void TopDownFlow(int x, int y, FluidBlock fluidBlock)
        {
            AdvancedFluidBlock advancedFluidBlock = fluidBlock as AdvancedFluidBlock;
            //Reset starting values
            float flowAmount = 0;
            float startAmount = advancedFluidBlock.Weight;
            float remainingAmount = startAmount;

            //If there is a block below it that is empty or has fluid of the same density then allow the fluid to flow down
            if (advancedFluidBlock.BottomBlock != null && advancedFluidBlock.BottomBlock.Weight != FluidBlock.SolidWeight && (advancedFluidBlock.BottomBlock.Density == advancedFluidBlock.Density || advancedFluidBlock.BottomBlock.Weight == 0))
            {
                //------Calculate the amount of fluid to flow down-----
                //Move one quarter of the difference between the two blocks to the block with less fluid
                flowAmount = (remainingAmount - advancedFluidBlock.BottomBlock.Weight) / 4f;

                //Ensure there isn't more fluid flowing than the block started with
                if (flowAmount < 0)
                    flowAmount = 0;
                if (flowAmount > remainingAmount)
                    flowAmount = remainingAmount;
                //If fluid is flowing down set the temporary values
                if (flowAmount > 0)
                {
                    //Remove fluid from the fluid block
                    remainingAmount -= flowAmount;
                    fluidDifference[x, y] -= flowAmount;
                    //Add fluid to the block below it
                    fluidDifference[x, y - 1] += flowAmount;
                    advancedFluidBlock.BottomBlock.Stable = false;
                    //If the bottom block is empty, set the new fluid density and color
                    if (advancedFluidBlock.BottomBlock.Weight == 0)
                    {
                        advancedFluidBlock.BottomBlock.Density = advancedFluidBlock.Density;
                        advancedFluidBlock.BottomBlock.Color = advancedFluidBlock.Color;
                    }
                    //If the block contains fluid that is the same density and a different color, then blend the colors together
                    else if (advancedFluidBlock.BottomBlock.Density == advancedFluidBlock.Density && !advancedFluidBlock.BottomBlock.Color.Equals(advancedFluidBlock.Color))
                        advancedFluidBlock.BottomBlock.Color = Color32.Lerp(advancedFluidBlock.BottomBlock.Color, advancedFluidBlock.Color, flowAmount / (advancedFluidBlock.BottomBlock.Weight + flowAmount) * fluidMixingFactor); 

                    //Stop flowing if there is not enough fluid remaining
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //If there is a block below it that is empty or has fluid of the same density then allow the fluid to flow right
            if (advancedFluidBlock.RightBlock != null && advancedFluidBlock.RightBlock.Weight != FluidBlock.SolidWeight && (advancedFluidBlock.RightBlock.Density == advancedFluidBlock.Density || advancedFluidBlock.RightBlock.Weight == 0)) 
            {
                //Calculate the amount of fluid to flow horizontally between the blocks
                //Move one quarter of the difference between the two blocks to the block with less fluid
                flowAmount = (remainingAmount - advancedFluidBlock.RightBlock.Weight) / 4f;
                //Ensure there isn't more fluid flowing than the block started with
                if (flowAmount < 0)
                    flowAmount = 0;
                if (flowAmount > remainingAmount)
                    flowAmount = remainingAmount;
                //If fluid is flowing down set the temporary values
                if (flowAmount > 0)
                {
                    //Remove fluid from the fluid block
                    remainingAmount -= flowAmount;
                    fluidDifference[x, y] -= flowAmount;
                    //Add fluid to the block to the right
                    fluidDifference[x + 1, y] += flowAmount;
                    if (flowAmount > stableAmount)
                        advancedFluidBlock.RightBlock.Stable = false;
                    //If the right block is empty, set the new fluid density and color
                    if (advancedFluidBlock.RightBlock.Weight == 0)
                    {
                        advancedFluidBlock.RightBlock.Density = advancedFluidBlock.Density;
                        advancedFluidBlock.RightBlock.Color = advancedFluidBlock.Color;
                    }
                    //If the block contains fluid that is the same density and a different color, then blend the colors together
                    else if (advancedFluidBlock.RightBlock.Density == advancedFluidBlock.Density && !advancedFluidBlock.RightBlock.Color.Equals(advancedFluidBlock.Color))
                        advancedFluidBlock.RightBlock.Color = Color32.Lerp(advancedFluidBlock.RightBlock.Color, advancedFluidBlock.Color, flowAmount / (advancedFluidBlock.RightBlock.Weight + flowAmount) * fluidMixingFactor); 
                    //Stop flowing if there is not enough fluid remaining
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //Flow left
            if (advancedFluidBlock.LeftBlock != null && advancedFluidBlock.LeftBlock.Weight != FluidBlock.SolidWeight && (advancedFluidBlock.LeftBlock.Density == advancedFluidBlock.Density || advancedFluidBlock.LeftBlock.Weight == 0)) 
            {
                flowAmount = (remainingAmount - advancedFluidBlock.LeftBlock.Weight) / 4f;

                if (flowAmount < 0)
                    flowAmount = 0;
                if (flowAmount > remainingAmount)
                    flowAmount = remainingAmount;

                if (flowAmount > 0)
                {
                    remainingAmount -= flowAmount;
                    fluidDifference[x, y] -= flowAmount;
                    fluidDifference[x - 1, y] += flowAmount;
                    if (flowAmount > stableAmount)
                        advancedFluidBlock.LeftBlock.Stable = false;
                    if (advancedFluidBlock.LeftBlock.Weight == 0)
                    {
                        advancedFluidBlock.LeftBlock.Density = advancedFluidBlock.Density;
                        advancedFluidBlock.LeftBlock.Color = advancedFluidBlock.Color;
                    }
                    else if (advancedFluidBlock.LeftBlock.Density == advancedFluidBlock.Density && !advancedFluidBlock.LeftBlock.Color.Equals(advancedFluidBlock.Color))
                        advancedFluidBlock.LeftBlock.Color = Color32.Lerp(advancedFluidBlock.LeftBlock.Color, advancedFluidBlock.Color, flowAmount / (advancedFluidBlock.LeftBlock.Weight + flowAmount) * fluidMixingFactor); 
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //Flow up
            if (advancedFluidBlock.TopBlock != null && advancedFluidBlock.TopBlock.Weight != FluidBlock.SolidWeight && (advancedFluidBlock.TopBlock.Density == advancedFluidBlock.Density || advancedFluidBlock.TopBlock.Weight == 0))
            {
                flowAmount = (remainingAmount - advancedFluidBlock.TopBlock.Weight) / 4f;

                if (flowAmount < 0)
                    flowAmount = 0;
                if (flowAmount > remainingAmount)
                    flowAmount = remainingAmount;

                if (flowAmount > 0)
                {
                    remainingAmount -= flowAmount;
                    fluidDifference[x, y] -= flowAmount;
                    fluidDifference[x, y + 1] += flowAmount;
                    advancedFluidBlock.TopBlock.Stable = false;
                    if (advancedFluidBlock.TopBlock.Weight == 0)
                    {
                        advancedFluidBlock.TopBlock.Density = advancedFluidBlock.Density;
                        advancedFluidBlock.TopBlock.Color = advancedFluidBlock.Color;
                    }
                    else if (advancedFluidBlock.TopBlock.Density == advancedFluidBlock.Density && !advancedFluidBlock.TopBlock.Color.Equals(advancedFluidBlock.Color))
                        advancedFluidBlock.TopBlock.Color = Color32.Lerp(advancedFluidBlock.TopBlock.Color, advancedFluidBlock.Color, flowAmount / (advancedFluidBlock.TopBlock.Weight + flowAmount) * fluidMixingFactor);
                    if (remainingAmount < minWeight)
                        return;
                }
            }
            //Calculate the difference in the liquid amount after flowing
            float difference = startAmount - remainingAmount;
            //If the difference is negligible, set the block to stable
            if (difference < stableAmount && difference > -stableAmount)
                advancedFluidBlock.Stable = true;
            //If there is a large difference unsettle the adjacent blocks
            else
                advancedFluidBlock.UnsettleNeighbours();
        }

        /// <summary>
        /// Calculates the movement of fluid in a fluid block for a down simulation
        /// </summary>
        /// <param name="x">X coordinate of the fluid block</param>
        /// <param name="y">Y coordinate of the fluid block</param>
        /// <param name="fluidBlock">Reference to the fluid block</param>
        protected override void DownFlow(int x, int y, FluidBlock fluidBlock)
        {
            AdvancedFluidBlock advancedFluidBlock = fluidBlock as AdvancedFluidBlock;
            
            //Reset starting values
            float flowAmount = 0;
            float startAmount = advancedFluidBlock.Weight;
            float remainingAmount = startAmount;
            
            //If there is a block below it that is empty or has fluid of the same density or has fluid of a different density, but Surface Filling is enabled and that block is not full then allow the fluid to flow down
            if (advancedFluidBlock.BottomBlock != null && advancedFluidBlock.BottomBlock.Weight != FluidBlock.SolidWeight && (advancedFluidBlock.BottomBlock.Density == advancedFluidBlock.Density || advancedFluidBlock.BottomBlock.Weight == 0 || (allowSurfaceFilling && advancedFluidBlock.BottomBlock.Weight < maxWeight)))
            {
                //------Calculate the amount of fluid to flow down-----
                //Get the total amount of fluid
                float combinedAmount = startAmount + advancedFluidBlock.BottomBlock.Weight;
                //The total amount is less than the max amount of fluid of a single block
                if (combinedAmount <= maxWeight)
                    //The lower block gets all the fluid
                    flowAmount = startAmount;
                //Both blocks are not fully pressurized with fluid
                else if (combinedAmount < 2 * maxWeight + pressureWeight)
                    //The lower block is filled and compressed by a factor of fluid in the top block
                    flowAmount = (combinedAmount * pressureWeight + maxWeight * maxWeight) / (maxWeight + pressureWeight) - advancedFluidBlock.BottomBlock.Weight;
                //Both blocks are full and pressurized
                else
                    //Lower block is filled with max pressure
                    flowAmount = (combinedAmount + pressureWeight) / 2f - advancedFluidBlock.BottomBlock.Weight;
                //Value equal to or between maxWeight and maxWeight + pressureWeight

                //Ensure there isn't more fluid flowing than the block started with
                if (flowAmount < 0)
                    flowAmount = 0;
                if (flowAmount > startAmount)
                    flowAmount = startAmount;

                //If fluid is flowing down set the temporary values
                if (flowAmount > 0)
                {
                    //Remove fluid from the fluid block
                    remainingAmount -= flowAmount;
                    fluidDifference[x, y] -= flowAmount;
                    //Add fluid to the block below it
                    fluidDifference[x, y - 1] += flowAmount;
                    advancedFluidBlock.BottomBlock.Stable = false;
                    //If the bottom block is empty, set the new fluid density and color
                    if (advancedFluidBlock.BottomBlock.Weight == 0)
                    {
                        advancedFluidBlock.BottomBlock.Density = advancedFluidBlock.Density;
                        advancedFluidBlock.BottomBlock.Color = advancedFluidBlock.Color;
                    }
                    //If the block contains fluid that is the same density and a different color, then blend the colors together
                    else if (advancedFluidBlock.BottomBlock.Density == advancedFluidBlock.Density && !advancedFluidBlock.BottomBlock.Color.Equals(advancedFluidBlock.Color))
                        advancedFluidBlock.BottomBlock.Color = Color32.Lerp(advancedFluidBlock.BottomBlock.Color, advancedFluidBlock.Color, fluidMixingFactor); //flowAmount / (fluidBlock.BottomBlock.Weight + flowAmount)

                    //Stop flowing if there is not enough fluid remaining
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //If there is a block below it that is empty or has fluid of the same density or has fluid of a different density, but Surface Filling is enabled and that block is not full then allow the fluid to flow right
            if (advancedFluidBlock.RightBlock != null && advancedFluidBlock.RightBlock.Weight != FluidBlock.SolidWeight && (advancedFluidBlock.RightBlock.Density == advancedFluidBlock.Density || advancedFluidBlock.RightBlock.Weight == 0)) //|| (SurfaceFilling && fluidBlock.RightBlock.Weight < maxWeight)
            {
                //Calculate the amount of fluid to flow horizontally between the blocks
                //Move one quarter of the difference between the two blocks to the block with less fluid
                flowAmount = (remainingAmount - advancedFluidBlock.RightBlock.Weight) / 4f;
                //Ensure there isn't more fluid flowing than the block started with
                if (flowAmount < 0)
                    flowAmount = 0;
                if (flowAmount > remainingAmount)
                    flowAmount = remainingAmount;
                //If fluid is flowing down set the temporary values
                if (flowAmount > 0)
                {
                    //Remove fluid from the fluid block
                    remainingAmount -= flowAmount;
                    fluidDifference[x, y] -= flowAmount;
                    //Add fluid to the block to the right
                    fluidDifference[x + 1, y] += flowAmount;
                    if (flowAmount > stableAmount)
                        advancedFluidBlock.RightBlock.Stable = false;
                    //If the right block is empty, set the new fluid density and color
                    if (advancedFluidBlock.RightBlock.Weight == 0)
                    {
                        advancedFluidBlock.RightBlock.Density = advancedFluidBlock.Density;
                        advancedFluidBlock.RightBlock.Color = advancedFluidBlock.Color;
                    }
                    //If the block contains fluid that is the same density and a different color, then blend the colors together
                    else if (advancedFluidBlock.RightBlock.Density == advancedFluidBlock.Density && !advancedFluidBlock.RightBlock.Color.Equals(advancedFluidBlock.Color))
                        advancedFluidBlock.RightBlock.Color = Color32.Lerp(advancedFluidBlock.RightBlock.Color, advancedFluidBlock.Color, fluidMixingFactor); //flowAmount / (fluidBlock.RightBlock.Weight + flowAmount)
                    //Stop flowing if there is not enough fluid remaining
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //Flow left
            if (advancedFluidBlock.LeftBlock != null && advancedFluidBlock.LeftBlock.Weight != FluidBlock.SolidWeight && (advancedFluidBlock.LeftBlock.Density == advancedFluidBlock.Density || advancedFluidBlock.LeftBlock.Weight == 0)) //|| (SurfaceFilling && fluidBlock.LeftBlock.Weight < maxWeight)
            {
                flowAmount = (remainingAmount - advancedFluidBlock.LeftBlock.Weight) / 4f;
                if (flowAmount < 0)
                    flowAmount = 0;
                if (flowAmount > remainingAmount)
                    flowAmount = remainingAmount;

                if (flowAmount > 0)
                {
                    remainingAmount -= flowAmount;
                    fluidDifference[x, y] -= flowAmount;
                    fluidDifference[x - 1, y] += flowAmount;
                    if (flowAmount > stableAmount)
                        advancedFluidBlock.LeftBlock.Stable = false;
                    if (advancedFluidBlock.LeftBlock.Weight == 0)
                    {
                        advancedFluidBlock.LeftBlock.Density = advancedFluidBlock.Density;
                        advancedFluidBlock.LeftBlock.Color = advancedFluidBlock.Color;
                    }
                    else if (advancedFluidBlock.LeftBlock.Density == advancedFluidBlock.Density && !advancedFluidBlock.LeftBlock.Color.Equals(advancedFluidBlock.Color))
                        advancedFluidBlock.LeftBlock.Color = Color32.Lerp(advancedFluidBlock.LeftBlock.Color, advancedFluidBlock.Color, fluidMixingFactor); //flowAmount / (fluidBlock.LeftBlock.Weight + flowAmount)
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //Flow up
            if (advancedFluidBlock.TopBlock != null && advancedFluidBlock.TopBlock.Weight != FluidBlock.SolidWeight && (advancedFluidBlock.TopBlock.Density == advancedFluidBlock.Density || advancedFluidBlock.TopBlock.Weight == 0)) //|| (SurfaceFilling && fluidBlock.TopBlock.Weight < maxWeight)
            {
                //Get the total amount of fluid
                float combinedAmount = remainingAmount + advancedFluidBlock.TopBlock.Weight;
                //The total amount is less than the max amount of fluid of a single block
                if (combinedAmount <= maxWeight)
                    //The lower block gets all the fluid
                    flowAmount = 0;
                //Both blocks are not fully pressurized with fluid
                else if (combinedAmount < 2 * maxWeight + pressureWeight)
                {
                    //The lower block is filled and compressed by a factor of fluid in the top block
                    flowAmount = remainingAmount - (combinedAmount * pressureWeight + maxWeight * maxWeight) / (maxWeight + pressureWeight);
                    //Both blocks are full and pressurized
                }
                else
                    //Lower block is filled with max pressure, the rest flows up
                    flowAmount = remainingAmount - (combinedAmount + pressureWeight) / 2f;
                //Returns a value equal to or between maxWeight and maxWeight + pressureWeight

                if (flowAmount < 0)
                    flowAmount = 0;
                if (flowAmount > remainingAmount)
                    flowAmount = remainingAmount;

                if (flowAmount > 0)
                {
                    remainingAmount -= flowAmount;
                    fluidDifference[x, y] -= flowAmount;
                    fluidDifference[x, y + 1] += flowAmount;
                    advancedFluidBlock.TopBlock.Stable = false;
                    if (advancedFluidBlock.TopBlock.Weight == 0)
                    {
                        advancedFluidBlock.TopBlock.Density = advancedFluidBlock.Density;
                        advancedFluidBlock.TopBlock.Color = advancedFluidBlock.Color;
                    }
                    else if (advancedFluidBlock.TopBlock.Density == advancedFluidBlock.Density && !advancedFluidBlock.TopBlock.Color.Equals(advancedFluidBlock.Color))
                        advancedFluidBlock.TopBlock.Color = Color32.Lerp(advancedFluidBlock.TopBlock.Color, advancedFluidBlock.Color,  fluidMixingFactor); //flowAmount / (fluidBlock.TopBlock.Weight + flowAmount)
                    if (remainingAmount < minWeight)
                        return;
                }
            }
            //Calculate the difference in the liquid amount after flowing
            float difference = startAmount - remainingAmount;
            //If the difference is negligible, set the block to stable
            if (difference < stableAmount && difference > -stableAmount)
                advancedFluidBlock.Stable = true;
            //If there is a large difference unsettle the adjacent blocks
            else
                advancedFluidBlock.UnsettleNeighbours();
        }
        
        /// <summary>
        /// Get the fluid block at a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns the fluid block</returns>
        public override FluidBlock GetFluidBlock(int x, int y)
        {
            if (x < 0 || x >= worldData.WorldWidth || y < 0 || y >= worldData.WorldHeight)
                return null;
            return fluidBlocks[x, y];
        }
    }
}