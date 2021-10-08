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
    /// The basic Fluid physics system with a single fluid type and faster performance
    /// </summary>
    /// <remarks>The fluid dynamics scripts contain repeat code in order to maximize performance by reducing casting</remarks>
    [DisallowMultipleComponent]
    public class BasicFluidDynamics : FluidDynamics 
    {
        private Color32 mainColor = new Color(0.176f, 0.431f, 0.557f, 0.8f);
        /// <summary>
        /// Main color of the fluid
        /// </summary>
        public Color32 MainColor
        {
            get { return mainColor; }
        }
        private Color32 secondaryColor = new Color(0.275f, 0.686f, 0.894f, 0.8f);
        /// <summary>
        /// Secondary color of the fluid (used for lower pressure blocks)
        /// </summary>
        public Color32 SecondaryColor
        {
            get { return secondaryColor; }
        }
        //The fluid information for each block in the world
        protected BasicFluidBlock[,] fluidBlocks;

        /// <summary>
        /// Set the instance of the fluid dynamics object
        /// </summary>
        protected override void SetInstance()
        {
            Instance = this;
        }
        
        /// <summary>
        /// Allocate memory for the basic fluid blocks
        /// </summary>
        protected override void AllocFluidBlocks()
        {
            fluidBlocks = new BasicFluidBlock[worldData.WorldWidth, worldData.WorldHeight];
            for (int x = 0; x < worldData.WorldWidth; x++)
            {
                for (int y = 0; y < worldData.WorldHeight; y++)
                {
                    fluidBlocks[x, y] = new BasicFluidBlock();
                }
            }
            //Sets the adjacent blocks for each fluid block
            for (int x = 0; x < worldData.WorldWidth; x++)
            {
                for (int y = 0; y < worldData.WorldHeight; y++)
                {
                    //Sets adjacent blocks that are within the world bounds
                    fluidBlocks[x, y].TopBlock = (BasicFluidBlock)GetFluidBlock(x, y + 1);
                    fluidBlocks[x, y].BottomBlock = (BasicFluidBlock)GetFluidBlock(x, y - 1);
                    fluidBlocks[x, y].LeftBlock = (BasicFluidBlock)GetFluidBlock(x - 1, y);
                    fluidBlocks[x, y].RightBlock = (BasicFluidBlock)GetFluidBlock(x + 1, y);
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
            
            mainColor = worldData.MainFluidColor;
            secondaryColor = worldData.SecondaryFluidColor;
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
            BasicFluidBlock basicFluidBlock = fluidBlock as BasicFluidBlock;
            //Reset starting values
            float flowAmount = 0;
            float startAmount = basicFluidBlock.Weight;
            float remainingAmount = startAmount;

            //If there is a block below it that is not solid flow down
            if (basicFluidBlock.BottomBlock != null && basicFluidBlock.BottomBlock.Weight != FluidBlock.SolidWeight)
            {
                //------Calculate the amount of fluid to flow down-----
                //Move one quarter of the difference between the two blocks to the block with less fluid
                flowAmount = (remainingAmount - basicFluidBlock.BottomBlock.Weight) / 4f;

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
                    basicFluidBlock.BottomBlock.Stable = false;

                    //Stop flowing if there is not enough fluid remaining
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //If there is a block to the right that is not solid flow right
            if (basicFluidBlock.RightBlock != null && basicFluidBlock.RightBlock.Weight != BasicFluidBlock.SolidWeight)
            {
                //Calculate the amount of fluid to flow horizontally between the blocks
                //Move one quarter of the difference between the two blocks to the block with less fluid
                flowAmount = (remainingAmount - basicFluidBlock.RightBlock.Weight) / 4f;
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
                        basicFluidBlock.RightBlock.Stable = false;

                    //Stop flowing if there is not enough fluid remaining
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //Flow left
            if (basicFluidBlock.LeftBlock != null && basicFluidBlock.LeftBlock.Weight != BasicFluidBlock.SolidWeight)
            {
                flowAmount = (remainingAmount - basicFluidBlock.LeftBlock.Weight) / 4f;

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
                        basicFluidBlock.LeftBlock.Stable = false;
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //Flow up
            if (basicFluidBlock.TopBlock != null && basicFluidBlock.TopBlock.Weight != BasicFluidBlock.SolidWeight)
            {
                flowAmount = (remainingAmount - basicFluidBlock.TopBlock.Weight) / 4f;

                if (flowAmount < 0)
                    flowAmount = 0;
                if (flowAmount > remainingAmount)
                    flowAmount = remainingAmount;

                if (flowAmount > 0)
                {
                    remainingAmount -= flowAmount;
                    fluidDifference[x, y] -= flowAmount;
                    fluidDifference[x, y + 1] += flowAmount;
                    basicFluidBlock.TopBlock.Stable = false;
                    if (remainingAmount < minWeight)
                        return;
                }
            }
            //Calculate the difference in the liquid amount after flowing
            float difference = startAmount - remainingAmount;
            //If the difference is negligible, set the block to stable
            if (difference < stableAmount && difference > -stableAmount)
                basicFluidBlock.Stable = true;
            //If there is a large difference unsettle the adjacent blocks
            else
                basicFluidBlock.UnsettleNeighbours();
        }

        /// <summary>
        /// Calculates the movement of fluid in a fluid block for a down simulation
        /// </summary>
        /// <param name="x">X coordinate of the fluid block</param>
        /// <param name="y">Y coordinate of the fluid block</param>
        /// <param name="fluidBlock">Reference to the fluid block</param>
        protected override void DownFlow(int x, int y, FluidBlock fluidBlock)
        {
            BasicFluidBlock basicFluidBlock = fluidBlock as BasicFluidBlock;
            //Reset starting values
            float flowAmount = 0;
            float startAmount = basicFluidBlock.Weight;
            float remainingAmount = startAmount;
            
            //If there is a block below it that is not solid flow down
            if (basicFluidBlock.BottomBlock != null && basicFluidBlock.BottomBlock.Weight != FluidBlock.SolidWeight)
            {
                //------Calculate the amount of fluid to flow down-----
                //Get the total amount of fluid
                float combinedAmount = startAmount + basicFluidBlock.BottomBlock.Weight;
                //The total amount is less than the max amount of fluid of a single block
                if (combinedAmount <= maxWeight)
                    //The lower block gets all the fluid
                    flowAmount = startAmount;
                //Both blocks are not fully pressurized with fluid
                else if (combinedAmount < 2 * maxWeight + pressureWeight)
                    //The lower block is filled and compressed by a factor of fluid in the top block
                    flowAmount = (combinedAmount * pressureWeight + maxWeight * maxWeight) / (maxWeight + pressureWeight) - basicFluidBlock.BottomBlock.Weight;
                //Both blocks are full and pressurized
                else
                    //Lower block is filled with max pressure
                    flowAmount = (combinedAmount + pressureWeight) / 2f - basicFluidBlock.BottomBlock.Weight;
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
                    basicFluidBlock.BottomBlock.Stable = false;

                    //Stop flowing if there is not enough fluid remaining
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //If there is a block to the right that is not solid flow right
            if (basicFluidBlock.RightBlock != null && basicFluidBlock.RightBlock.Weight != FluidBlock.SolidWeight)
            {
                //Calculate the amount of fluid to flow horizontally between the blocks
                //Move one quarter of the difference between the two blocks to the block with less fluid
                flowAmount = (remainingAmount - basicFluidBlock.RightBlock.Weight) / 4f;
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
                        basicFluidBlock.RightBlock.Stable = false;

                    //Stop flowing if there is not enough fluid remaining
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //Flow left
            if (basicFluidBlock.LeftBlock != null && basicFluidBlock.LeftBlock.Weight != FluidBlock.SolidWeight)
            {
                flowAmount = (remainingAmount - basicFluidBlock.LeftBlock.Weight) / 4f;
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
                        basicFluidBlock.LeftBlock.Stable = false;
                    if (remainingAmount < minWeight)
                        return;
                }
            }

            //Flow up
            if (basicFluidBlock.TopBlock != null && basicFluidBlock.TopBlock.Weight != FluidBlock.SolidWeight)
            {
                //Get the total amount of fluid
                float combinedAmount = remainingAmount + basicFluidBlock.TopBlock.Weight;
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
                    basicFluidBlock.TopBlock.Stable = false;
                    if (remainingAmount < minWeight)
                        return;
                }
            }
            //Calculate the difference in the liquid amount after flowing
            float difference = startAmount - remainingAmount;
            //If the difference is negligible, set the block to stable
            if (difference < stableAmount && difference > -stableAmount)
                basicFluidBlock.Stable = true;
            //If there is a large difference unsettle the adjacent blocks
            else
                basicFluidBlock.UnsettleNeighbours();
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
