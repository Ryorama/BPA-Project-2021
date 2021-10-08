using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.Lighting
{
    /// <summary>
    /// 2D Ambient Lighting
    /// </summary>
    public class AmbientLight : BlockLighting
    {
        private static AmbientLight instance;

        /// <summary>
        /// A singleton instance of the Ambient Lighting
        /// </summary>
        public static AmbientLight Instance
        {
            get { return instance; }
        }

        [HideInInspector]
        /// <summary>
        /// The color of the ambient light in the daytime
        /// Changes the color of the Main Camera
        /// </summary>
        public Color DaylightColor = Color.white;

        [HideInInspector]
        /// <summary>
        /// The color of the ambient lighting in the nighttime
        /// Changes the color of the Main Camera
        /// </summary>
        public Color NightColor;

        //The colors used to set the ambient lighting
        [HideInInspector]
        /// <summary>
        /// The time the sun will rise, used for setting the color of the ambient lighting material (default 7)
        /// </summary>
        public float SunriseTime = 7;

        [HideInInspector]
        /// <summary>
        /// The time the sun will set, used for settinge the color of the ambient lighting material (default 19)
        /// </summary>
        public float SunsetTime = 19;

        private bool useHeightMap;

        /// <summary>
        /// Whether to use the heightmap to calulate an ambient light value
        /// </summary>
        public bool UseHeightMap
        {
            get { return useHeightMap; }
        }

        //Heightmap used for generating ambient light
        private int[] heightMap;

        private Color32 whiteColor32 = new Color32(255, 255, 255, 255);

        protected override void Awake()
        {
            base.Awake();

            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Debug.Log("Destroying extra instance of " + name);
                Destroy(this);
            }

            //Set Properties
            IntensityFactor = World.WorldData.AmbientIntensityFactor;
            TransmissionFactor = World.WorldData.AmbientTransmissionFactor;
            useHeightMap = World.WorldData.UseHeightMap;
            blockLayerIndex = World.WorldData.AmbientLightLayer;
        }

        protected override void Start()
        {
            base.Start();
            SetAmbientLightColor();
        }

        /// <summary>
        /// Initialize the ambient lighting
        /// </summary>
        public override void Initialize()
        {
            if (initialized)
                return;

            if (world == null)
                world = World.Instance;

            base.Initialize();

            //Calculate the Height Map data by finding the position of the surface blocks 
            if (useHeightMap)
            {
                heightMap = new int[world.WorldWidth];
                for (int x = 0; x < world.WorldWidth; x++)
                {
                    int y = world.WorldHeight - 1;
                    while (!world.GetBlockLayer(blockLayerIndex).IsBlockAt(x, y) && y > 0)
                    {
                        //Place a light source at every position above the terrain surface to imitate sunlight
                        AddLightSourceNoUpdate(new Vector2Int(x, y), whiteColor32);
                        y--;
                    }

                    heightMap[x] = y;
                }
            }
            else
            {
                for (int x = 0; x < world.WorldWidth; x++)
                {
                    for (int y = 0; y < world.WorldHeight; y++)
                    {
                        if (!world.GetBlockLayer(blockLayerIndex).IsBlockAt(x, y))
                        {
                            //Place a light source wherever there isn't an ambient light block to imitate sunlight
                            AddLightSourceNoUpdate(new Vector2Int(x, y), whiteColor32);
                        }
                    }
                }
            }

            UpdateLighting();
        }

        private void Update()
        {
            if (world.PauseTime)
                return;

            SetAmbientLightColor();
        }

        private void SetAmbientLightColor()
        {
            float time = world.TimeOfDay;
            //Set the ambient light color based on the time of day
            if (time < SunriseTime || time >= SunsetTime + 1)
                Camera.main.backgroundColor = NightColor;
            else if (time >= SunriseTime + 1 && time < SunsetTime)
                Camera.main.backgroundColor = DaylightColor;
            else if (time >= SunriseTime && time < SunriseTime + 1)
                Camera.main.backgroundColor = Color.Lerp(NightColor, DaylightColor, (time - SunriseTime) / 1f);
            else if (time >= SunsetTime && time < SunsetTime + 1)
                Camera.main.backgroundColor = Color.Lerp(DaylightColor, NightColor, (time - SunsetTime) / 1f);

            if (time < SunriseTime || time >= SunsetTime + 1)
                material.SetFloat(Shader.PropertyToID("_Alpha"), NightColor.a);
            else if (time >= SunriseTime + 1 && time < SunsetTime)
                material.SetFloat(Shader.PropertyToID("_Alpha"), DaylightColor.a);
            else if (time >= SunriseTime && time < SunriseTime + 1)
                material.SetFloat(Shader.PropertyToID("_Alpha"),
                    Mathf.Lerp(NightColor.a, DaylightColor.a, (time - SunriseTime) / 1f));
            else if (time >= SunsetTime && time < SunsetTime + 1)
                material.SetFloat(Shader.PropertyToID("_Alpha"),
                    Mathf.Lerp(DaylightColor.a, NightColor.a, (time - SunsetTime) / 1f));
        }

        /// <summary>
        /// Update the lighting if a block is placed
        /// If there is a light source where the block is placed then remove it
        /// </summary>
        /// <param name="x">The x coordinate where the block was placed</param>
        /// <param name="y">The y coordinate where the block was placed</param>
        /// <param name="layer">The layer the block was placed in</param>
        /// <param name="blockType">The type of block placed</param>
        protected override void BlockPlaced(int x, int y, byte layer, byte blockType)
        {
            if (layer == blockLayerIndex)
            {
                BlockInfo blockInfo = world.GetBlockLayer(layer).GetBlockInfo(blockType);
                int width = 1;
                if (blockInfo.MultiBlock)
                    width = blockInfo.TextureWidth;
                int height = 1;
                if (blockInfo.MultiBlock)
                    height = blockInfo.TextureHeight;

                if (useHeightMap)
                {
                    //Update the height map when terrain is modified
                    for (int i = x; i < x + width; i++)
                    {
                        //If a block is added above the surface of the terrain then calculate the new height
                        // and remove any ambient light sources that are now below the terrains surface
                        int blockTop = y + height - 1;
                        if (heightMap[i] < blockTop)
                        {
                            int j = heightMap[i] + 1;
                            RemoveLightsFromArea(new Vector2Int(i, j), 1, blockTop - heightMap[i], false);
                            heightMap[i] = blockTop;
                        }
                        for (int j = y; j < y + height; j++)
                        {
                            lightMap[i, j] = TerrainColor;
                        }
                    }
                }
                else
                {
                    //If a block was added, then remove the light sources in the grid positions that the block took up
                    for (int i = x; i < x + width; i++)
                    {
                        for (int j = y; j < y + height; j++)
                        {
                            lightMap[i, j] = TerrainColor;
                        }
                    }
                }

                UpdateLighting();
            }
        }

        /// <summary>
        /// Update the block lighting if a block was removed 
        /// </summary>
        /// <param name="x">The x coordinate where the block was removed</param>
        /// <param name="y">The y coordinate where the block was removed</param>
        /// <param name="layer">The layer the block was removed from</param>
        /// <param name="blockType">The type of block removed</param>
        protected override void BlockRemoved(int x, int y, byte layer, byte blockType)
        {
            if (layer == blockLayerIndex)
            {
                BlockInfo blockInfo = world.GetBlockLayer(layer).GetBlockInfo(blockType);
                int width = 1;
                if (blockInfo.MultiBlock)
                    width = blockInfo.TextureWidth;
                int height = 1;
                if (blockInfo.MultiBlock)
                    height = blockInfo.TextureHeight;

                if (useHeightMap)
                {
                    //Update the height map when terrain is modified
                    for (int i = x; i < x + width; i++)
                    {
                        //If a surface block was removed then calculate the new height of the surface block and 
                        //add light sources to all the blocks above the new surface block that were previously blocked off
                        if (heightMap[i] == y + height - 1)
                        {
                            int newHeightMapY = heightMap[i];
                            while (!world.GetBlockLayer(blockLayerIndex).IsBlockAt(i, newHeightMapY) &&
                                   newHeightMapY > 0)
                            {
                                AddLightSourceNoUpdate(new Vector2Int(i, newHeightMapY), whiteColor32);
                                newHeightMapY--;
                            }

                            heightMap[i] = newHeightMapY;
                        }
                    }
                }
                else
                {
                    //If a block was added, then remove the light sources in the grid positions that the block took up
                    for (int i = x; i < x + width; i++)
                    {
                        for (int j = y; j < y + height; j++)
                        {
                            AddLightSourceNoUpdate(new Vector2Int(i, j), whiteColor32);
                        }
                    }
                }

                UpdateLighting();
            }
        }
    }
}