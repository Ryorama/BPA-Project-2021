using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.Lighting
{
    /// <summary>
    /// A 2d block lighting system that runs on the gpu
    /// </summary>
    public class BlockLighting : TexturedMesh
    {
        protected World world;
        protected ChunkLoader chunkLoader;

        [SerializeField] [Tooltip("The compute shader for generating the lighting")]
        private ComputeShader lightSpreadShader;

        [HideInInspector] [SerializeField] [Tooltip("A higher value means light can transmit through more blocks")]
        protected int transmissionFactor = 20;

        /// <summary>
        /// The global transmission factor, used to determine how much light is lost when transmitting through a block
        /// </summary>
        public int TransmissionFactor
        {
            get { return transmissionFactor; }
            set { transmissionFactor = value; }
        }

        [HideInInspector]
        [SerializeField]
        [Tooltip(
            "The global intensity of the light, used to determine how far light can propagate from a light source")]
        protected int intensityFactor = 6;

        /// <summary>
        /// The global intensity of the light, used to determine how far the light can propagate from a light source
        /// </summary>
        public int IntensityFactor
        {
            get { return intensityFactor; }
            set { intensityFactor = value; }
        }

        protected Color32[,] lightMap;

        /// <summary>
        /// The positioning of light sources in the world
        /// Black pixels of varying transparency represents the terrain blocks (their transparency
        /// representing the block's distance from the edge)
        /// Transparent pixels represent air blocks
        /// All other colours are light sources
        /// </summary>
        public Color32[,] LightMap
        {
            get { return lightMap; }
        }

        //Whether the block lighting has been initialized yet
        protected bool initialized;

        /// <summary>
        /// Whether the lighting system has been initialized or not
        /// </summary>
        public bool Initialized
        {
            get { return initialized; }
        }

        //The block layer to use for the block lighting
        protected int blockLayerIndex;

        //Input texture for inputting lighting data into the compute shader
        protected RenderTexture inputTexture;

        //Transparent colour for clearing pixel data
        protected Color32 ClearColor = new Color32(0, 0, 0, 0);

        //Black colour for setting terrain pixels
        protected Color32 TerrainColor = new Color32(0, 0, 0, 255);

        /// <summary>
        /// Actions invoked by the Block Lighting System
        /// </summary>
        public delegate void BlockLightingEvent();

        /// <summary>
        /// Event called when lighting has been generated
        /// </summary>
        public event BlockLightingEvent OnLightGenerated;

        private void OnEnable()
        {
            ChunkLoader.OnChunksLoaded += GenerateLighting;
            World.OnBlockPlaced += BlockPlaced;
            World.OnBlockRemoved += BlockRemoved;
        }

        private void OnDisable()
        {
            ChunkLoader.OnChunksLoaded -= GenerateLighting;
            World.OnBlockPlaced -= BlockPlaced;
            World.OnBlockRemoved -= BlockRemoved;
        }

        protected override void Awake()
        {
            base.Awake();
            gameObject.layer = LayerMask.NameToLayer("Lighting");
            blockLayerIndex = World.WorldData.LightLayer;
        }

        protected virtual void Start()
        {
            //Ensure the block lighting has been initialized
            if (!initialized)
                Initialize();
        }

        /// <summary>
        /// Initialize the block lighting
        /// </summary>
        public virtual void Initialize()
        {
            if (initialized)
                return;

            world = World.Instance;
            chunkLoader = ChunkLoader.Instance;
            base.Initialize(chunkLoader.LoadedWorldWidth, chunkLoader.LoadedWorldHeight);
            material.mainTexture = outputTexture;
            
            //Set the z position/render order for the light system
            transform.position = new Vector3(transform.position.x, transform.position.y, world.EndZPoint -
                world.ZBlockDistance * world.ZLayerFactor * World.LIGHT_SYSTEM_Z_ORDER);

            inputTexture = new RenderTexture(width, height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            inputTexture.enableRandomWrite = true;
            inputTexture.filterMode = FilterMode;
            inputTexture.Create();

            //Initialize the light map for the world
            lightMap = new Color32[world.WorldWidth, world.WorldHeight];
            for (int x = 0; x < world.WorldWidth; x++)
            {
                for (int y = 0; y < world.WorldHeight; y++)
                {
                    //Add a black pixel to the lightmap for blocks in the lightlayer
                    if (world.GetBlockLayer(blockLayerIndex).IsBlockAt(x, y))
                        //Set the alpha of the pixels depending on their distance from air (greater distance from air means higher alpha)
                        lightMap[x, y] = TerrainColor;
                }
            }

            initialized = true;
        }

        protected override void LateUpdate()
        {
            if (update)
            {
                GenerateLighting();
                update = false;
            }
        }

        protected override Color32[] GetPixelData()
        {
            Color32[,] pixels = lightMap;
            //Current position of the texture
            int posX = (int) transform.position.x;
            int posY = (int) transform.position.y;

            int index = 0;
            //Grab the a section of the pixel data from the current position of the loaded world
            for (int y = posY; y < posY + height; y++)
            {
                for (int x = posX; x < posX + width; x++)
                {
                    //Set the pixel data to the array
                    tempPixelData[index] = pixels[x, y];
                    index++;
                }
            }

            return tempPixelData;
        }

        /// <summary>
        /// Generate the block lighting 
        /// </summary>
        private void GenerateLighting()
        {
            transform.position = new Vector3(chunkLoader.OriginLoadedChunks.x,
                chunkLoader.OriginLoadedChunks.y, transform.position.z);
            //Create a texture from the colour array
            GenerateTexture(GetPixelData());
            //Set the amount the colour intensity will drop block to block as the light spreads from the source
            float intensityDrop = 1.0f / intensityFactor;
            //Set the additional amount the colour intensity will drop as light spreads to a terrain block
            float transmissionDrop = 1.0f / transmissionFactor;
            lightSpreadShader.SetFloats("IntensityDrop",
                new float[3] {intensityDrop, intensityDrop, intensityDrop});
            lightSpreadShader.SetFloats("TransmissionDrop",
                new float[3] {transmissionDrop, transmissionDrop, transmissionDrop});
            //Spread the lighting from the light sources using a compute shader
            int lightSpreadKernelIndex = lightSpreadShader.FindKernel("SpreadLight");
            lightSpreadShader.SetTexture(lightSpreadKernelIndex, "Input", texture2D);
            lightSpreadShader.SetTexture(lightSpreadKernelIndex, "TerrainMask", texture2D);
            lightSpreadShader.SetTexture(lightSpreadKernelIndex, "Output", outputTexture);
            lightSpreadShader.Dispatch(lightSpreadKernelIndex, width / 8, height / 8, 1);

            //Run multiple passes as each pass spreads the lighting by one block
            for (int i=0; i<=intensityFactor; i++)
            {
                RenderTexture tempTexture = inputTexture;
                inputTexture = outputTexture;
                outputTexture = tempTexture;
              
                lightSpreadShader.SetTexture(lightSpreadKernelIndex, "Input", inputTexture);
                lightSpreadShader.SetTexture(lightSpreadKernelIndex, "Output", outputTexture);
                lightSpreadShader.Dispatch(lightSpreadKernelIndex, width / 8, height / 8, 1);
            }

            //Run the light generated event
            if (OnLightGenerated != null)
                OnLightGenerated();
        }


        /// <summary>
        /// Manually Generate Lighting
        /// Use when lights are added in bulk
        /// </summary>
        public void UpdateLighting()
        {
            update = true;
        }

        /// <summary>
        /// Add a light source
        /// </summary>
        ///<param name="color">The color of the light source</param>
        ///<param name="position">The position of the light source</param>
        /// <returns>Returns false if there is already a light in that key position</returns>
        public bool AddLightSource(Vector2Int position, Color32 color)
        {
            if (IsLightSource(position))
                return false;
            lightMap[position.x, position.y] = color;
            update = true;
            return true;
        }

        /// <summary>
        /// Add a light source without regenerating the lighting
        /// Make sure to call ManualLightGeneration after all light sources are added
        /// </summary>
        ///<param name="color">The color of the light source</param>
        ///<param name="position">The position of the light source</param>
        /// <returns>Returns false if there is already a light in that key position</returns>
        public bool AddLightSourceNoUpdate(Vector2Int position, Color32 color)
        {
            if (IsLightSource(position))
                return false;
            lightMap[position.x, position.y] = color;
            return true;
        }

        /// <summary>
        /// Remove a light source from a given position
        /// </summary>
        /// <param name="position">The position to remove a light source</param>
        /// <returns>Returns true if a light was removed from that position</returns>
        public bool RemoveLightSource(Vector2Int position)
        {
            if (!IsLightSource(position))
                return false;
            lightMap[position.x, position.y] = ClearColor;
            update = true;
            return true;
        }

        /// <summary>
        /// Remove a light source from a given position without regenerating the lighting
        /// Make sure to call ManualLightGeneration after all light sources are removed
        /// </summary>
        /// <param name="position">The position to remove a light source</param>
        /// <returns>Returns true if a light was removed from that position</returns>
        public bool RemoveLightSourceNoUpdate(Vector2Int position)
        {
            if (!IsLightSource(position))
                return false;
            Debug.Log(position.x + " " + position.y);
            lightMap[position.x, position.y] = ClearColor;
            return true;
        }

        /// <summary>
        /// Remove an area of light sources
        /// </summary>
        /// <param name="position">The starting position to remove the light sources</param>
        /// <param name="width">The width of the area</param>
        /// <param name="height">The height of the area</param>
        /// <param name="update">Whether to regenerate the lighting (optional)</param>
        public void RemoveLightsFromArea(Vector2Int position, int width, int height, bool update = true)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (IsLightSource(position.x + i, position.y + j))
                        lightMap[position.x + i, position.y + j] = ClearColor;
                }
            }

            if (update)
                update = true;
        }

        /// <summary>
        /// Get the light source from a position
        /// </summary>
        /// <param name="position">The position to try and get a light source from</param>
        /// <param name="lightSource">The reference to a light source for output</param>
        /// <returns>Returns true if a light source is found at that position</returns>
        public bool GetLightSource(Vector2Int position, out Color32 lightSource)
        {
            if (IsLightSource(position))
            {
                lightSource = lightMap[position.x, position.y];
                return true;
            }

            lightSource = ClearColor;
            return false;
        }

        /// <summary>
        /// Check if there is a light source at the position
        /// </summary>
        /// <param name="position">The position to check for a light source</param>
        /// <returns>Returns true if a light source is found at that position</returns>
        public bool IsLightSource(Vector2Int position)
        {
            return lightMap[position.x, position.y].r != 0 || lightMap[position.x, position.y].g != 0
                                                           || lightMap[position.x, position.y].b != 0;
        }

        /// <summary>
        /// Check if there is a light source at the position
        /// </summary>
        /// <param name="x">The x-coordinate of the light source</param>
        /// <param name="y">The y-coordinate of the light source</param>
        /// <returns>Returns true if a light source is found at that position</returns>
        public bool IsLightSource(int x, int y)
        {
            return lightMap[x, y].r != 0 || lightMap[x, y].g != 0 || lightMap[x, y].b != 0;
        }

        /// <summary>
        /// Clear the map of all lights
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < lightMap.GetLength(0); i++)
            {
                for (int j = 0; j < lightMap.GetLength(1); j++)
                {
                    if (IsLightSource(i, j))
                        lightMap[i, j] = ClearColor;
                }
            }
            update = true;
        }

        /// <summary>
        /// Update the lighting if a block is placed
        /// If there is a light source where the block is placed then remove it
        /// </summary>
        /// <param name="x">The x coordinate where the block was placed</param>
        /// <param name="y">The y coordinate where the block was placed</param>
        /// <param name="layer">The layer the block was placed in</param>
        /// <param name="blockType">The type of block placed</param>
        protected virtual void BlockPlaced(int x, int y, byte layer, byte blockType)
        {
            if (layer == blockLayerIndex)
            {
                BlockInfo blockInfo = world.GetBlockLayer(layer).GetBlockInfo(blockType);
                if (blockInfo.MultiBlock)
                {
                    for (int i = 0; i < blockInfo.TextureWidth; i++)
                    {
                        for (int j = 0; j < blockInfo.TextureHeight; j++)
                        {
                            lightMap[x + i, y + j] = TerrainColor;
                        }
                    }
                }
                else
                {
                    lightMap[x, y] = TerrainColor;
                }

                update = true;
            }
        }

        /// <summary>
        /// Update the block lighting if a block was removed 
        /// </summary>
        /// <param name="x">The x coordinate where the block was removed</param>
        /// <param name="y">The y coordinate where the block was removed</param>
        /// <param name="layer">The layer the block was removed from</param>
        /// <param name="blockType">The type of block removed</param>
        protected virtual void BlockRemoved(int x, int y, byte layer, byte blockType)
        {
            if (layer == blockLayerIndex)
            {
                BlockInfo blockInfo = world.GetBlockLayer(layer).GetBlockInfo(blockType);
                if (blockInfo.MultiBlock)
                {
                    for (int i = 0; i < blockInfo.TextureWidth; i++)
                    {
                        for (int j = 0; j < blockInfo.TextureHeight; j++)
                        {
                            lightMap[x + i, y + j] = ClearColor;
                        }
                    }
                }
                else
                    lightMap[x, y] = ClearColor;

                update = true;
            }
        }
    }
}