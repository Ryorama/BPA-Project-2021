using UnityEngine;
using System;
using TerrainEngine2D.Lighting;
using UnityEngine.Serialization;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// The 2D procedurally generated world of blocks
    /// </summary>
    public class World : MonoBehaviourSingleton<World>
    {
        //Position of the block for bitmasking
        [Flags] private enum BlockPosition {
            TopRight = 1,
            BottomRight = 2,
            BottomLeft = 4,
            TopLeft = 8,
            Top = 16,
            Right = 32,
            Bottom = 64,
            Left = 128
        }

        /// <summary>
        /// Render order of the light system
        /// </summary>
        public const int LIGHT_SYSTEM_Z_ORDER = 1;
        /// <summary>
        /// Z order of world space UI elemnts
        /// </summary>
        public const int UI_Z_ORDER = 9;
        /// <summary>
        /// Z order of the Camera
        /// Used to set the z position of the Camera
        /// </summary>
        public const int CAMERA_Z_ORDER = 10;

        /// <summary>
        /// All data used to generate the world
        /// </summary>
        public static WorldData WorldData;
        //A copy of the data used to control data changes while in play mode
        private WorldData worldDataCopy;
        [SerializeField]
        private WorldData worldData;

        /// <summary>
        /// The name of the World
        /// </summary>
        public string Name = "World";
        //Handles loading the chunks
        [SerializeField]
        private ChunkLoader chunkLoader;
        /// <summary>
        /// Reference to the ChunkLoader for updating Chunks
        /// </summary>
        public ChunkLoader ChunkLoader
        {
            set { chunkLoader = value; }
        }
        //Fluid physics simulation
        [SerializeField]
        private FluidDynamics fluidDynamics;
        /// <summary>
        /// Reference to the FluidDynamics for updating the FluidBlocks
        /// </summary>
        public FluidDynamics FluidDynamics
        {
            set { fluidDynamics = value; }
        }
        [SerializeField]
        private FallingBlockSimulation fallingBlockSimulation;
        /// <summary>
        /// Reference to the Falling Block Simulation for updates
        /// </summary>
        public FallingBlockSimulation FallingBlockSimulation
        {
            set { fallingBlockSimulation = value; }
        }
        //Procedural generation of block data
        [SerializeField]
        private TerrainGenerator terrainGenerator;
        /// <summary>
        /// Reference to the TerrainGenerator script for generating the terrain data
        /// </summary>
        public TerrainGenerator TerrainGenerator
        {
            get { return terrainGenerator; }
        }
        //Reference to the basic light system
        [SerializeField]
        private LightSystem lightSystem;
        /// <summary>
        /// Reference to the LightSystem for updating the shadow mask
        /// </summary>
        public LightSystem LightSystem
        {
            set { lightSystem = value; }
        }
        //Reference to the advanced light system
        [SerializeField]
        private AdvancedLightSystem advancedLightSystem;
        /// <summary>
        /// Reference to the AdvancedLightSystem
        /// </summary>
        public AdvancedLightSystem AdvancedLightSystem
        {
            set { advancedLightSystem = value; }
        }
        //Reference to the light renderer
        [SerializeField]
        private LightRenderer lightRenderer;
        /// <summary>
        /// Reference to the LightRenderer
        /// </summary>
        public LightRenderer LightRenderer
        {
            set { lightRenderer = value; }
        }
        //Reference to the ambient light
        [SerializeField]
        private AmbientLight ambientLight;
        /// <summary>
        /// Reference to the ambient lighting
        /// </summary>
        public AmbientLight AmbientLight
        {
            set { ambientLight = value; }
        }

        [SerializeField]
        private int worldWidth = 1024;
        /// <summary>
        /// /The width of the world in blocks
        /// </summary>
        public int WorldWidth
        {
            get { return worldWidth; }
            set { worldWidth = value; }
        }
        [SerializeField]
        private int worldHeight = 128;
        /// <summary>
        /// The height of the world in blocks
        /// </summary>
        public int WorldHeight
        {
            get { return worldHeight; }
            set { worldHeight = value; }
        }

        [SerializeField]
        private int seed = 12345678;
        /// <summary>
        /// A number used to procedurally generate the terrain data
        /// </summary>
        public int Seed
        {
            get { return seed; }
            set { seed = value; }
        }
        [SerializeField]
        private byte fluidLayer;
        /// <summary>
        /// The layer which fluid is placed
        /// </summary>
        public byte FluidLayer
        {
            get { return fluidLayer; }
            set { fluidLayer = value; }
        }
        [SerializeField]
        private bool renderFluidAsTexture;
        /// <summary>
        /// Whether to render the fluid simulation in a texture, or in chunks of meshes
        /// </summary>
        public bool RenderFluidAsTexture
        {
            get { return renderFluidAsTexture; }
        }
        [SerializeField]
        private byte lightLayer;
        /// <summary>
        /// The layer the light data maps to
        /// </summary>
        public byte LightLayer
        {
            get { return lightLayer; }
            set { lightLayer = value; }
        }
        [SerializeField]
        private byte ambientLightLayer;
        /// <summary>
        /// The layer the ambient light data maps to
        /// </summary>
        public byte AmbientLightLayer
        {
            get { return ambientLightLayer; }
            set { ambientLightLayer = value; }
        }
        [SerializeField]
        private byte fallingBlockLayer;
        /// <summary>
        /// The layer containing falling blocks
        /// </summary>
        public byte FallingBlockLayer
        {
            get { return fallingBlockLayer; }
            set { fallingBlockLayer = value; }
        }
        [SerializeField]
        private bool fluidDisabled;
        /// <summary>
        /// Whether the fluid simulation is disabled
        /// </summary>
        public bool FluidDisabled
        {
            get { return fluidDisabled; }
        }
        [SerializeField]
        private bool basicFluid;
        /// <summary>
        /// Whether using the basic or advanced fluid system
        /// </summary>
        public bool BasicFluid
        {
            get { return basicFluid; }
        }
        [SerializeField]
        private bool fallingBlockDisabled;
        /// <summary>
        /// Whether the falling block simulation is disabled
        /// </summary>
        public bool FallingBlocksDisabled
        {
            get { return fallingBlockDisabled; }
        }
        [SerializeField]
        private bool lightingDisabled;
        /// <summary>
        /// Whether lighting is disabled
        /// </summary>
        public bool LightingDisabled
        {
            get { return lightingDisabled; }
        }
        [SerializeField]
        private bool basicLighting;
        /// <summary>
        /// Whether using basic or advanced lighting
        /// </summary>
        public bool BasicLighting
        {
            get { return basicLighting; }
        }
        [SerializeField]
        private int pixelsPerBlock = 8;
        /// <summary>
        /// The number of pixels per block (Unity units) - should be the same as the pixels per unit of the tileset textures
        /// </summary>
        public int PixelsPerBlock
        {
            get { return pixelsPerBlock; }
        }
        [SerializeField]
        private float zBlockDistance = 0.1f;
        /// <summary>
        /// The z distance between different block types (for render order)
        /// </summary>
        public float ZBlockDistance
        {
            get { return zBlockDistance; }
        }
        [SerializeField]
        private int zLayerFactor = 1;
        /// <summary>
        /// The z distance factor between layers
        /// Layers will be positioned at a distance of zBlockDistance * zLayerFactor away from eachother
        /// </summary>
        public int ZLayerFactor
        {
            get { return zLayerFactor; }
        }

        private float endZPoint;
        /// <summary>
        /// The z coordinate for the furthest block from the origin (used for setting z order of GameObjects that are in front of the world)
        /// </summary>
        public float EndZPoint
        {
            get { return endZPoint; }
        }
        private byte[] colliderLayers;
        /// <summary>
        /// List of all the layers which have colliders
        /// </summary>
        public byte[] ColliderLayers
        {
            get { return colliderLayers; }
        }
        //The block layers
        [SerializeField]
        private BlockLayer[] blockLayers;
        /// <summary>
        /// All the block layers that makeup the terrain
        /// </summary>
        public BlockLayer[] BlockLayers
        {
            get { return blockLayers; }
            set { blockLayers = value;}
        }
        private int numBlockLayers;
        /// <summary>
        /// The number of block layers
        /// </summary>
        public int NumBlockLayers
        {
            get { return numBlockLayers; }
        }
        //Random for generation variations
        private System.Random random;

        [SerializeField]
        private string worldDirectory;
        /// <summary>
        /// Selected path to the World directory
        /// </summary>
        public string WorldDirectory
        {
            get { return worldDirectory; }
            set { worldDirectory = value; }
        }
        /// <summary>
        /// Whether to load the World from file
        /// </summary>
        public bool LoadWorld;
        /// <summary>
        /// Whether to save the World data
        /// </summary>
        public bool SaveWorld;
        /// <summary>
        /// Whether to Overlap Blend Squares over block edges when generating an Overlap Block mesh, or replace them (optimization)
        /// </summary>
        public bool OverlapBlendSquares;
        /// <summary>
        /// Whether to generate blocks which are hidden behind other layers
        /// </summary>
        public bool DoNotGenerateHiddenBlocks = true;
        [SerializeField]
        private float timeOfDay = 12;
        /// <summary>
        /// The current time of day
        /// </summary>
        public float TimeOfDay
        {
            get { return timeOfDay; }
        }
        [SerializeField]
        private int timeFactor = 100;
        /// <summary>
        /// The factor used to determine how fast time will go by in the game (a time factor of 1 is realtime)
        /// </summary>
        public int TimeFactor
        {
            get { return timeFactor; }
        }
        private int dayCount;
        /// <summary>
        /// The amount of days that have gone by
        /// </summary>
        public int DayCount
        {
            get { return dayCount; }
        }
        [SerializeField]
        private bool pauseTime;
        /// <summary>
        /// Whether to pause the current time/day cycle
        /// </summary>
        public bool PauseTime
        {
            get { return pauseTime; }
            set { pauseTime = value; }
        }
        /// <summary>
        /// Actions invoked by World Generation
        /// </summary>
        public delegate void WorldGeneration();
        /// <summary>
        /// Event called when the world is finished loading
        /// </summary>
        public static event WorldGeneration OnWorldGenerated;
        /// <summary>
        /// Actions invoked when the day count changes
        /// </summary>
        /// <param name="dayCount">The current day count</param>
        public delegate void DayCycle(int dayCount);
        /// <summary>
        /// Event called when a new day starts
        /// </summary>
        public static event DayCycle OnNewDay;
        /// <summary>
        /// Actions invoked when a block is placed
        /// </summary>
        public delegate void BlockModifiedAction(int x, int y, byte layer, byte blockType);
        /// <summary>
        /// Event called when blocks have been added
        /// </summary>
        public static event BlockModifiedAction OnBlockPlaced;
        /// <summary>
        /// Event called when blocks have been removed
        /// </summary>
        public static event BlockModifiedAction OnBlockRemoved;

        protected override void Awake()
        {
            base.Awake();

            //Check if the world contains the necessary layers
            int lightingLayer = LayerMask.NameToLayer("Lighting");
            int terrainLayer = LayerMask.NameToLayer("Terrain");
            int ignoreLightingLayer = LayerMask.NameToLayer("Ignore Lighting");
            if (lightingLayer == -1 || terrainLayer == -1 || ignoreLightingLayer == -1)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPaused = true;
#endif
                throw new Exception("The project does not contain the necessary layers for Terrain Engine 2D to work, please add these three layers to the project: Terrain, Lighting, Ignore Lighting");
            }

            if (worldData == null)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPaused = true;
#endif
                throw new MissingReferenceException("The World has no WorldData!");
            }
            //Create a copy of the current world data
            worldDataCopy = worldData.DeepCopy();

            if (chunkLoader == null)
                chunkLoader = GetComponentInChildren<ChunkLoader>(true);
            if (fluidDynamics == null)
                fluidDynamics = GetComponentInChildren<FluidDynamics>(true);
            if(lightSystem == null)
                lightSystem = GetComponentInChildren<LightSystem>(true);
            if (ambientLight == null)
                ambientLight = GetComponentInChildren<AmbientLight>(true);
            if (fallingBlockSimulation == null)
                fallingBlockSimulation = GetComponentInChildren<FallingBlockSimulation>(true);
            if (advancedLightSystem == null)
                advancedLightSystem = GetComponentInChildren<AdvancedLightSystem>(true);
            if(lightRenderer == null)
                lightRenderer = Camera.main.GetComponent<LightRenderer>();

            Initialize();

            if (SaveWorld && !LoadWorld) //(LoadWorld && Serialization.IsValidWorldDirectory(worldDirectory)
                Serialization.SaveBaseData();

            //Determine the number of collider layers
            int numColliderLayers = 0;
            for (int i = 0; i < numBlockLayers; i++)
            {
                    //If this is a collider layer increment the counter
                if (blockLayers[i].ColliderLayer)
                    numColliderLayers++;
            }

            //Set the collider layers
            colliderLayers = new byte[numColliderLayers];
            numColliderLayers = 0;
            for (int i = 0; i < blockLayers.Length; i++)
            {
                if (blockLayers[i].ColliderLayer)
                {
                    colliderLayers[numColliderLayers] = (byte)i;
                    numColliderLayers++;
                }
            }
            //Find the index to the last layer
            int lastBlockLayerIndex = blockLayers.Length - 1;
            //Calculate the z end point by getting the z position of the final layer and adding the total distance of its block types
            endZPoint = blockLayers[lastBlockLayerIndex].ZLayerOrder - blockLayers[lastBlockLayerIndex].GetNumBlockTypes() * zBlockDistance;
        }

        void Start()
        {
            //Generate the block data
            if (LoadWorld)
            {
                if (!Serialization.LoadTerrainData(worldDirectory))
                    throw new Exception("Error loading terrain data");
                GenerateBlockData();
            }
            else
                GenerateTerrain();

            //Update the fluid
            fluidDynamics.UpdateFluid();
            
            if(SaveWorld)
                Serialization.SaveTerrainData();

            //Generate the light map data
            if (!lightingDisabled)
            {
                if (basicLighting)
                    lightSystem.Initialize(this);
                else
                {
                    if(!worldData.AmbientLightDisabled)
                        ambientLight.Initialize();
                    advancedLightSystem.BlockLighting.Initialize();
                }
            }

            //Begin loading chunks
            chunkLoader.BeginChunkLoading();

            //Run World Generated event
            if(OnWorldGenerated != null)
                OnWorldGenerated();
        }

        /// <summary>
        /// Initialize the world
        /// </summary>
        public void Initialize()
        {
            WorldData = worldData;

            //Set Properties
            SaveWorld = worldData.SaveWorld;
            LoadWorld = worldData.LoadWorld;
            Name = worldData.Name;
            worldWidth = worldData.WorldWidth;
            worldHeight = worldData.WorldHeight;
            Seed = worldData.Seed;
            WorldDirectory = worldData.WorldDirectory;
            fluidDisabled = worldData.FluidDisabled;
            fluidLayer = worldData.FluidLayer;
            basicFluid = worldData.BasicFluid;
            renderFluidAsTexture = worldData.RenderFluidAsTexture;
            lightingDisabled = worldData.LightingDisabled;
            basicLighting = worldData.BasicLighting;
            lightLayer = worldData.LightLayer;
            ambientLightLayer = worldData.AmbientLightLayer;
            fallingBlockDisabled = worldData.FallingBlocksDisabled;
            FallingBlockLayer = worldData.FallingBlockLayer;
            OverlapBlendSquares = worldData.OverlapBlendSquares;
            DoNotGenerateHiddenBlocks = worldData.CullHiddenBlocks;
            pixelsPerBlock = worldData.PixelsPerBlock;
            zBlockDistance = worldData.ZBlockDistance;
            zLayerFactor = worldData.ZLayerFactor;
            blockLayers = worldData.BlockLayers;

            if (blockLayers == null)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPaused = true;
#endif
                throw new Exception("The block layers have not been setup yet!");
            }

            //Initialize the random number generator with the given seed
            random = new System.Random(seed);

            numBlockLayers = blockLayers.Length;
            //Setup the block layers
            for (int i = 0; i < blockLayers.Length; i++)
            {
                //If it is the first layer, there are no previous layers so send null
                if (i == 0)
                    blockLayers[i].InitializeBlockLayer(worldWidth, worldHeight, pixelsPerBlock, zBlockDistance, ZLayerFactor, null);
                else
                    blockLayers[i].InitializeBlockLayer(worldWidth, worldHeight, pixelsPerBlock, zBlockDistance, ZLayerFactor, blockLayers[i - 1]);
            }
        }

        /// <summary>
        /// Procedurally generate the block data using the TerrainData script
        /// </summary>
        void GenerateBlockData()
        {
            //Loop through all the blocks of the world in every layer
            for (int i = 0; i < blockLayers.Length; i++)
            {
                for (int x = 0; x < worldWidth; x++)
                {
                    for (int y = 0; y < worldHeight; y++)
                    {
                        //If there is a block at the current position
                        if (blockLayers[i].IsBlockAt(x, y))
                        {
                            //If block is an Overlap block
                            if (blockLayers[i].GetBlockInfo(x, y).OverlapBlock)
                            {
                                //Calculate the bitmask at the current position
                                blockLayers[i].SetBitmask(x, y, CalculateBitmask(x, y, i, blockLayers[i].GetBlockType(x, y)));
                            }
                            //Generate a random variation for the block
                            GenerateVariation(x, y, i);
                        } 
                    }
                }
            }
        }

        private void Update()
        {
            if (!pauseTime)
            {
                timeOfDay += (Time.deltaTime / 3600f) * timeFactor;
                if (timeOfDay >= 24)
                {
                    timeOfDay = 0;
                    dayCount++;
                    if(OnNewDay != null)
                        OnNewDay(dayCount);
                }
            }
        }

        /// <summary>
        /// Generate the Terrain for the world
        /// </summary>
        public void GenerateTerrain()
        {
            //Generate the data using the TerrainData script
            if (terrainGenerator == null)
                throw new MissingReferenceException("Missing Terrain Generator Script!");
            terrainGenerator.GenerateData();
            GenerateBlockData();
        }

        /// <summary>
        /// Randomly set the block variation at a specific point in a layer
        /// </summary>
        /// <param name="x">X coordinate of point</param>
        /// <param name="y">Y coordinate of point</param>
        /// <param name="layer">The block layer</param>
        void GenerateVariation(int x, int y, int layer)
        {
            //Get the number of variations of the block at that coordinate
            int numVariations = blockLayers[layer].GetBlockInfo(x, y).NumVariations;
            //Randomly set a possible variation if there are any
            if (numVariations > 1)
                blockLayers[layer].SetVariation(x, y, (byte)random.Next(numVariations));
        }

        /// <summary>
        /// Calculate the bitmask at a specific point in a layer
        /// </summary>
        /// <param name="x">X coordinate of point</param>
        /// <param name="y">Y coordinate of point</param>
        /// <param name="layer">The block layer</param>
        /// <param name="blockType">The block type at that point</param>
        /// <returns>Returns the calculated bitmask</returns>
        /// <remarks>The bitmask is used for setting vertex and uv values when building the block mesh</remarks>
        byte CalculateBitmask(int x, int y, int layer, byte blockType)
        {
            byte bitmask = 0;
            //---Corner bitmask---
            //If there is a block at the corner of this block of similar type set the bit on
            //This is used so the BlockGridMesh knows whether to generate an overlaying blend block
            //Ex. If the block in the top right corner is the same type set the first bit on
            if (blockLayers[layer].GetBlockType(x + 1, y + 1) == blockType)
                bitmask |= 1;
            if (blockLayers[layer].GetBlockType(x + 1, y - 1) == blockType)
                bitmask |= 2;
            if (blockLayers[layer].GetBlockType(x - 1, y - 1) == blockType)
                bitmask |= 4;
            if (blockLayers[layer].GetBlockType(x - 1, y + 1) == blockType)
                bitmask |= 8;
            //---Cardinal bitmask---
            //If there is no adjacent block of similar type or of type with greater render order set the bit on
            //This is used so the BlockGridMesh knows whether to include a specific block edge when generating the mesh
            //Ex. If the block to the right has a lesser blockType (it is rendered under this block or it is air), then set the bit on
            if (!(blockLayers[layer].GetBlockType(x, y + 1) >= blockType))
                bitmask |= 16;
            if (!(blockLayers[layer].GetBlockType(x + 1, y) >= blockType))
                bitmask |= 32;
            if (!(blockLayers[layer].GetBlockType(x, y - 1) >= blockType))
                bitmask |= 64;
            if (!(blockLayers[layer].GetBlockType(x - 1, y) >= blockType))
                bitmask |= 128;
            return bitmask;
        }

        /// <summary>
        /// Recalculates the bitmask of surrounding blocks
        /// </summary>
        /// <param name="x">The X position of the block that changed</param>
        /// <param name="y">The y position of the block that changed</param>
        /// <param name="width">The width of the block (in block units)</param>
        /// <param name="height">The height of the block (in block units)</param>
        /// <param name="layer">The block layer</param>
        /// <param name="newOverlapBlock">Whether the a new block was added</param>
        /// <param name="blockType">The blockType of the new added block</param>
        /// <remarks>Called when block is placed, block changes, or block is removed</remarks>
        void UpdateBitmask(int x, int y, int width, int height, byte layer, bool newOverlapBlock = false, byte blockType = 0)
        {
            //Caculate the bitmask if a new Overlap block was added
            if (newOverlapBlock)
                blockLayers[layer].SetBitmask(x, y, CalculateBitmask(x, y, layer, blockType));
            
            //Update the bits of the surrounding corners
            //Ex. To update the top right block, the bottom left bit must be updated
            UpdateBlockBit(x + width, y + height, layer, BlockPosition.BottomLeft);
            UpdateBlockBit(x + width, y - 1, layer, BlockPosition.TopLeft);
            UpdateBlockBit(x - 1, y - 1, layer, BlockPosition.TopRight);
            UpdateBlockBit(x - 1, y + height, layer, BlockPosition.BottomRight);
            //Update the bits of the surrounding edge blocks
            for (int edgeX = x; edgeX < x + width; edgeX++)
            {
                UpdateBlockBit(edgeX, y + height, layer, BlockPosition.Bottom);
                UpdateBlockBit(edgeX, y - 1, layer, BlockPosition.Top);
            }
            for (int edgeY = y; edgeY < y + height; edgeY++)
            {
                UpdateBlockBit(x + width, edgeY, layer, BlockPosition.Left);
                UpdateBlockBit(x - 1, edgeY, layer, BlockPosition.Right);
            }
        }

        /// <summary>
        /// Updates the bit of a blocks bitmask
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="layer">The block layer</param>
        /// <param name="blockBit">The block bit to be updated</param>
        void UpdateBlockBit(int x, int y, byte layer, BlockPosition blockBit)
        {
            //Check to make sure there is a block at that position
            if (blockLayers[layer].IsBlockAt(x, y))
            {
                //Only need to update the bitmask if it is an Overlap Block
                if (blockLayers[layer].GetBlockInfo(x, y).OverlapBlock)
                {
                    //Get the block type at the coordinate
                    byte blockType = blockLayers[layer].GetBlockType(x, y);
                    //Get the current bitmask of the block
                    int bitmask = blockLayers[layer].GetBitmask(x, y);
                    //Update the proper bit of the blocks bitmask
                    switch (blockBit)
                    {
                        //Ex. If updating the top right position bit
                        case BlockPosition.TopRight:
                            //Check if there is a block of similar type at that position
                            if (blockLayers[layer].GetBlockType(x + 1, y + 1) == blockType)
                                //Set the bit on
                                bitmask |= 1;
                            else
                                //Set the bit off
                                bitmask &= ~1;
                            break;
                        case BlockPosition.BottomRight:
                            if (blockLayers[layer].GetBlockType(x + 1, y - 1) == blockType)
                                bitmask |= 2;
                            else
                                bitmask &= ~2;
                            break;
                        case BlockPosition.BottomLeft:
                            if (blockLayers[layer].GetBlockType(x - 1, y - 1) == blockType)
                                bitmask |= 4;
                            else
                                bitmask &= ~4;
                            break;
                        case BlockPosition.TopLeft:
                            if (blockLayers[layer].GetBlockType(x - 1, y + 1) == blockType)
                                bitmask |= 8;
                            else
                                bitmask &= ~8;
                            break;
                        case BlockPosition.Top:
                            if (!(blockLayers[layer].GetBlockType(x, y + 1) >= blockType))
                                bitmask |= 16;
                            else
                                bitmask &= ~16;
                            break;
                        case BlockPosition.Right:
                            if (!(blockLayers[layer].GetBlockType(x + 1, y) >= blockType))
                                bitmask |= 32;
                            else
                                bitmask &= ~32;
                            break;
                        case BlockPosition.Bottom:
                            if (!(blockLayers[layer].GetBlockType(x, y - 1) >= blockType))
                                bitmask |= 64;
                            else
                                bitmask &= ~64;
                            break;
                        case BlockPosition.Left:
                            if (!(blockLayers[layer].GetBlockType(x - 1, y) >= blockType))
                                bitmask |= 128;
                            else
                                bitmask &= ~128;
                            break;
                    }
                    //Set the updated bitmask value
                    blockLayers[layer].SetBitmask(x, y, (byte)bitmask);
                }
            }
        }

        /// <summary>
        /// Check if a bit is set in a bitmask
        /// </summary>
        /// <param name="bitmask">The bitmask to check</param>
        /// <param name="position">The position of the bit in the bitmask</param>
        /// <returns>Return true if the bit is set</returns>
        bool IsBitSet(byte bitmask, int position)
        {
            return (bitmask & (1 << position)) != 0;
        }

        /// <summary>
        /// Adds a block to the specified coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="blockType">The type of block</param>
        /// <param name="selectedLayer">The layer to place the block in</param>
        /// <param name="replaceBlock">Whether to replace the current block in that position (optional)</param>
        /// <returns>Returns true if a block is successfully added</returns>
        public bool AddBlock(int x, int y, byte blockType, byte selectedLayer, bool replaceBlock = false)
        {
            //Get the blockType at the current position
            byte currBlockType = GetBlockLayer(selectedLayer).GetBlockType(x, y);
            if (!replaceBlock && currBlockType != BlockLayer.AIR_BLOCK)
                return false;
            //Check whether the block can be added at that position
            if (!blockLayers[selectedLayer].CanAddBlock(x, y, blockType))
                return false;
            //If replacing a block, then remove it first
            if (replaceBlock && currBlockType != BlockLayer.AIR_BLOCK)
                RemoveBlock(x, y, selectedLayer);

            //Add the block
            blockLayers[selectedLayer].AddBlockRaw(x, y, blockType);
            
            //Get the size of the block
            BlockInfo blockInfo = blockLayers[selectedLayer].GetBlockInfo(blockType);
            int width, height;
            if (blockInfo.MultiBlock)
            {
                width = blockInfo.TextureWidth;
                height = blockInfo.TextureHeight;
            } else
            {
                width = 1;
                height = 1;
            }
            //If the selected layer is the fluid layer and fluid is not disabled
            if (selectedLayer == fluidLayer && !fluidDisabled)
            {
                //Set all the fluid blocks within the block's area to solid
                for (int posX = x; posX < x + width; posX++)
                {
                    for (int posY = y; posY < y + height; posY++)
                    {
                        //If there is fluid where the block was placed, evenly split that fluid between adjacent open blocks
                        //If there are no open adjacent blocks then the fluid disappears 
                        BlockLayer blockLayer = GetBlockLayer(fluidLayer);
                        bool noTopBlock = !blockLayer.IsBlockAt(posX, posY + 1);
                        bool noRightBlock = !blockLayer.IsBlockAt(posX + 1, posY);
                        bool noBottomBlock = !blockLayer.IsBlockAt(posX, posY - 1);
                        bool noLeftBlock = !blockLayer.IsBlockAt(posX - 1, posY);

                        FluidBlock fluidBlock = fluidDynamics.GetFluidBlock(posX, posY);
                        float amount = fluidBlock.Weight;
                        int count = 4;
                        while (amount > 0 && count > 0)
                        {
                            float split = amount / count;
                            if (noTopBlock)
                            {
                                fluidDynamics.GetFluidBlock(posX, posY + 1).AddWeight(split);
                                amount -= split;
                            }
                            if (noRightBlock)
                            {
                                fluidDynamics.GetFluidBlock(posX + 1, posY).AddWeight(split);
                                amount -= split;
                            }
                            if (noBottomBlock)
                            {
                                fluidDynamics.GetFluidBlock(posX, posY - 1).AddWeight(split);
                                amount -= split;
                            }
                            if (noLeftBlock)
                            {
                                fluidDynamics.GetFluidBlock(posX - 1, posY).AddWeight(split);
                                amount -= split;
                            }
                            count--;
                        }
                        fluidBlock.SetSolid();
                        
                    }
                }
                fluidDynamics.UpdateFluid();
            //If the selected layer is the light layer and lighting is not disabled
            }

            if (!lightingDisabled)
            {
                if (basicLighting && selectedLayer == lightLayer)
                {
                    //Update the lighting around the block
                    lightSystem.UpdateLighting(x, y, width, height);
                }
            }

            //Update the bitmask of the block and it's surroundings
            if (blockInfo.MultiBlock)
                UpdateBitmask(x, y, width, height, selectedLayer);
            else
                UpdateBitmask(x, y, width, height, selectedLayer, true, blockType);

            //Set the block's variation
            GenerateVariation(x, y, selectedLayer);
            //Update the chunk which contains the block
            chunkLoader.UpdateChunk(x, y);

            //Run event
            if(OnBlockPlaced != null)
                OnBlockPlaced(x, y, selectedLayer, blockType);

            return true;
        }
        /// <summary>
        /// Removes a block from the specified coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="selectedLayer">List of layers to remove blocks from</param>
        /// <returns>Returns the blocktype of the removed block</returns>
        public byte RemoveBlock(int x, int y, byte selectedLayer)
        {
            //Get the size of the block
            byte blockType = blockLayers[selectedLayer].GetBlockType(x, y);
            if (blockType == 0)
                return 0;
            BlockInfo blockInfo = blockLayers[selectedLayer].GetBlockInfo(blockType);
            int width, height;
            if (blockInfo.MultiBlock)
            {
                width = blockInfo.TextureWidth;
                height = blockInfo.TextureHeight;
            }
            else
            {
                width = 1;
                height = 1;
            }
            //Remove the block relative to the coordinate and get it's render position
            Vector2Int blockPos = blockLayers[selectedLayer].RemoveBlock(x, y);

            //If the selected layer is a fluid layer and fluid is not disabled
            if (selectedLayer == fluidLayer && !fluidDisabled)
            {
                //Set all the fluid blocks to empty which make up the area of the block
                for (int posX = blockPos.x; posX < blockPos.x + width; posX++)
                {
                    for (int posY = blockPos.y; posY < blockPos.y + height; posY++)
                        fluidDynamics.GetFluidBlock(posX, posY).SetEmpty();
                }
                fluidDynamics.UpdateFluid();
            }
            if (!LightingDisabled)
            {
                //If the selected layer is a light layer and lighting is not disabled
                if (basicLighting && selectedLayer == lightLayer)
                {
                    //Update the lighting around the block
                    lightSystem.UpdateLighting(blockPos.x, blockPos.y, width, height);
                }
            }

            //Update the bitmask surrounding the block
            UpdateBitmask(blockPos.x, blockPos.y, width, height, selectedLayer);

            //Reset the variation
            blockLayers[selectedLayer].SetVariation(blockPos.x, blockPos.y, 0);

            //Update the chunk holding the block
            chunkLoader.UpdateChunk(blockPos.x, blockPos.y);

            //Run event
            if(OnBlockRemoved != null)
                OnBlockRemoved(blockPos.x, blockPos.y, selectedLayer, blockType);

            return blockType;
        }

        /// <summary>
        /// Get the distance from air at a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Return the distance from air in blocks (Unity units)</returns>
        public int GetBlockDistanceFromAir(int x, int y)
        {
            //If distance is zero there are adjacent air blocks
            int distance = 0;
            //Continue to increase the distance while no adjacent blocks are air, or the block is too far from air
            while (blockLayers[lightLayer].IsBlockAt(x + (distance + 1), y) && blockLayers[lightLayer].IsBlockAt(x - (distance + 1), y) &&
                blockLayers[lightLayer].IsBlockAt(x, y + (distance + 1)) && blockLayers[lightLayer].IsBlockAt(x, y - (distance + 1)) && distance < lightSystem.LightBleed)
            {
                distance++;
            }
            return distance;
        }

        /// <summary>
        /// Get the specified block layer
        /// </summary>
        /// <param name="layer">The block layer index</param>
        /// <returns>Returns the block layer</returns>
        public BlockLayer GetBlockLayer(int layer)
        {
            return blockLayers[layer];
        }

        /// <summary>
        /// Enable or disable the lighting
        /// </summary>
        public void SetLighting(bool enable)
        {
            GameObject lightCameraGO = Camera.main.transform.GetChild(0).gameObject;
            GameObject overlayCameraGO = Camera.main.transform.GetChild(1).gameObject;

            worldData.LightingDisabled = !enable;
            lightingDisabled = worldData.LightingDisabled;

            if (LightingDisabled)
            {
                lightSystem.enabled = false;
                lightSystem.GetComponent<MeshRenderer>().enabled = false;
                lightRenderer.enabled = false;
                advancedLightSystem.enabled = false;
                advancedLightSystem.BlockLighting.enabled = false;
                advancedLightSystem.BlockLighting.GetComponent<MeshRenderer>().enabled = false;
                ambientLight.enabled = false;
                ambientLight.GetComponent<MeshRenderer>().enabled = false;
                lightCameraGO.SetActive(false);
                overlayCameraGO.SetActive(false);
                Camera.main.cullingMask |= 1 << LayerMask.NameToLayer("UI");
                Camera.main.cullingMask |= 1 << LayerMask.NameToLayer("Ignore Lighting");
            }
            else
            {
                if (worldData.BasicLighting)
                {
                    lightSystem.enabled = true;
                    if (Application.isPlaying &&!lightSystem.Initialized)
                        lightSystem.Initialize(this);
                    lightSystem.GetComponent<MeshRenderer>().enabled = true;
                    lightRenderer.enabled = false;
                    advancedLightSystem.enabled = false;
                    advancedLightSystem.BlockLighting.enabled = false;
                    advancedLightSystem.BlockLighting.GetComponent<MeshRenderer>().enabled = false;
                    if (!worldData.AmbientLightDisabled)
                    {
                        ambientLight.enabled = false;
                        ambientLight.GetComponent<MeshRenderer>().enabled = false;
                    }
                    lightCameraGO.SetActive(false);
                    overlayCameraGO.SetActive(false);
                    Camera.main.cullingMask |= 1 << LayerMask.NameToLayer("UI");
                    Camera.main.cullingMask |= 1 << LayerMask.NameToLayer("Ignore Lighting");
                }
                else
                {
                    lightSystem.enabled = false;
                    lightSystem.GetComponent<MeshRenderer>().enabled = false;
                    lightRenderer.enabled = true;
                    advancedLightSystem.enabled = true;
                    advancedLightSystem.BlockLighting.enabled = true;
                    advancedLightSystem.BlockLighting.GetComponent<MeshRenderer>().enabled = true;
                    if (Application.isPlaying && !advancedLightSystem.BlockLighting.Initialized)
                        advancedLightSystem.BlockLighting.Initialize();

                    ambientLight.enabled = !worldData.AmbientLightDisabled;
                    if (Application.isPlaying && !ambientLight.Initialized)
                        ambientLight.Initialize();
                    ambientLight.GetComponent<MeshRenderer>().enabled = !worldData.AmbientLightDisabled;
                    lightCameraGO.SetActive(true);
                    overlayCameraGO.SetActive(true);
                    Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
                    Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("Ignore Lighting"));
#if UNITY_EDITOR
                    if (Application.isPlaying)
                    {
                        advancedLightSystem.UpdateLightSources();
                        advancedLightSystem.BlockLighting.UpdateLighting();
                        ambientLight.UpdateLighting();
                    }
#else
                    advancedLightSystem.UpdateLightSources();
                    advancedLightSystem.BlockLighting.UpdateLighting();
                    ambientLight.UpdateLighting();
#endif
                }
            }
        }

        private void OnApplicationQuit()
        {
            //Revert any changes to the save data
            if (!worldData.SavePlayModeChanges)
            {
                if (worldDataCopy != null)
                {
                    worldData.CopyData(worldDataCopy);
                    Debug.Log("Successfully reverted any WorldData changes.");
                }
            }

            if (SaveWorld)
                Serialization.Save();
        }

    }
}