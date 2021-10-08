using TerrainEngine2D;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.TopDownDemo
{
    //Layer types
    public enum Layers { Background, Main, Nature, Mountain }
    //Block types
    public enum BackgroundLayer { Sand, Dirt }
    public enum MainLayer { Sand, Grass, Dirt }
    public enum NatureLayer { Flower, Rock }
    public enum MountainLayer { DarkRock, Rock }
    //Fluid types
    public enum FluidType { Water }

    public class TopDownTerrainGenerator : TerrainGenerator
    {
        /// <summary>
        /// Procedurally generates world block data using random and pseudo-random functions
        /// </summary>
        public override void GenerateData()
        {
            base.GenerateData();

            //Pass 1
            for (int x = 0; x < world.WorldWidth; x++)
            {
                for (int y = 0; y < world.WorldHeight; y++)
                {
                    //-----Set block data here-----
                    //Cover the world with a layer of sand in the background
                    SetBlock(x, y, (byte)Layers.Background, (byte)BackgroundLayer.Sand);
                    //Cover the world with water
                    AddFluid(x, y, 4, (byte)FluidType.Water);
                    //Noise representing a large area 
                    if (PerlinNoise(x, y, 60, 55) > 15)
                    {
                        //Removes the water in this area
                        RemoveFluid(x, y);
                        //Adds sand to the main layer in this area
                        SetBlock(x, y, (byte)Layers.Main, (byte)MainLayer.Sand);
                        //Noise representing an inset portion of the original area size
                        if (PerlinNoise(x, y, 60, 55) > 20)
                        {
                            //Adds dirt to the background and main layer in this area (replacing the sand)
                            SetBlock(x, y, (byte)Layers.Background, (byte)BackgroundLayer.Dirt);
                            SetBlock(x, y, (byte)Layers.Main, (byte)MainLayer.Dirt);

                            if (PerlinNoise(x, y, 60, 55) > 22)
                            {
                                //Adds grass to the main layer
                                SetBlock(x, y, (byte)Layers.Main, (byte)MainLayer.Grass);
                                //Add mountain blocks using a smaller area of perlin noise inset by 2 blocks from the outer grassy area
                                if (PerlinNoise(x, y, 24, 19) > 8 && PerlinNoise(x, y, 60, 55) > 24)
                                {
                                    //Adds dark rock mountain blocks
                                    SetBlock(x, y, (byte)Layers.Mountain, (byte)MountainLayer.DarkRock);
                                    if (PerlinNoise(x, y * 3, 6, 8) > 3)
                                    {
                                        //Sets some of the mountain blocks to rock
                                        SetBlock(x, y, (byte)Layers.Mountain, (byte)MountainLayer.Rock);
                                    }
                                    //Adds small rock decorations near the bottom of the mountains
                                }
                                else
                                {
                                    if (PerlinNoise(x, y, 24, 19) <= 8 && PerlinNoise(x, y, 24, 19) > 6)
                                    {
                                        SetBlock(x, y, (byte)Layers.Nature, (byte)NatureLayer.Rock, 5);
                                    }
                                    else
                                    {
                                        //Adds flowers randomly to the grass
                                        SetBlock(x, y, (byte)Layers.Nature, (byte)NatureLayer.Flower, 10);
                                    }
                                }
                            }
                        }
                    }
                    //-----
                }
            }
        }
    }
}