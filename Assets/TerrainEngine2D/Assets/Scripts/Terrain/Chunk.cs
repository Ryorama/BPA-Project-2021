using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    /// <summary>
    /// A chunk of blocks for world rendering and collider generation
    /// </summary>
    public class Chunk : MonoBehaviour
    {
        //Reference to the world
        private World world;
        [SerializeField]
        private FluidChunk fluidChunk;
        /// <summary>
        /// Chunk for rendering fluid
        /// </summary>
        public FluidChunk FluidChunk
        {
            get { return fluidChunk; }
        }
        //Generates the PolygonCollider2D paths
        private ColliderGenerator colliderGenerator;
        //Holds reference to the MeshRenderer for setting the materials
        private MeshRenderer meshRenderer;
        //Holds mesh information for rendering the chunk
        private BlockGridMesh blockGridMesh;

        //Cache of Material arrays for dynamically setting the materials of the mesh renderer
        private Material[][] materialCache;

        private int chunkSize;
        /// <summary>
        /// Side length of the chunk in block units
        /// </summary>
        public int ChunkSize
        {
            get { return chunkSize; }
        }
        private int chunkX;
        /// <summary>
        /// X position of the chunk in block units
        /// </summary>
        public int ChunkX
        {
            get { return chunkX; }
        }
        private int chunkY;
        /// <summary>
        /// Y position of the chunk in block units
        /// </summary>
        public int ChunkY
        {
            get { return chunkY; }
        }
        private bool update;
        private bool updateColliders;
        /// <summary>
        /// Whether the Chunk needs to be updated
        /// Used to update the mesh when blocks change
        /// </summary>
        public bool Update
        {
            set { update = value; updateColliders = value; }
        }

        private void Awake()
        {
            colliderGenerator = GetComponent<ColliderGenerator>();
            meshRenderer = GetComponent<MeshRenderer>();

            gameObject.layer = LayerMask.NameToLayer("Terrain");
        }

        /// <summary>
        /// Setup Chunk
        /// </summary>
        public void InitializeChunk(int chunkSize, int chunkX, int chunkY)
        {
            this.chunkSize = chunkSize;
            this.chunkX = chunkX;
            this.chunkY = chunkY;
            world = World.Instance;
            //Initialize the block grid mesh
            blockGridMesh = new BlockGridMesh(GetComponent<MeshFilter>().mesh, chunkSize, world.ZBlockDistance, true, world.NumBlockLayers);
            //Initialize the ColliderGenerator lists if there is a collider layer
            if (world.ColliderLayers.Length != 0)
                colliderGenerator.Initialize(chunkSize);
            
            materialCache = new Material[world.NumBlockLayers][];
            for (int i = 0; i < world.NumBlockLayers; i++)
            {
                materialCache[i] = new Material[i + 1];
            }
            //Set the new position of the chunk
            transform.position = new Vector3(chunkX, chunkY, 0);
            BuildChunk();
            BuildCollider();
        }

        /// <summary>
        /// Regenerate the chunk when it loads or changes position
        /// </summary>
        /// <param name="chunkX">X coordinate of the chunk (in blocks)</param>
        /// <param name="chunkY">Y coordinate of the chunk (in blocks)</param>
        public void ReGenerate(int chunkX, int chunkY)
        {
            this.chunkX = chunkX;
            this.chunkY = chunkY;
            //Set the new position of the chunk
            transform.position = new Vector3(chunkX, chunkY, 0);
            //Update the fluid chunk
            fluidChunk.Update = true;
            //Rebuild the chunk
            BuildChunk();
            BuildCollider();
        }

        void LateUpdate()
        {
            //Updates the chunk when its blocks are modified 
            if (update)
            {
                BuildChunk();
                update = false;
            }
        }

        private void FixedUpdate()
        {
            if (updateColliders)
            {
                BuildCollider();
                updateColliders = false;
            }
        }

        /// <summary>
        /// Builds the chunk mesh
        /// </summary>
        void BuildChunk()
        {
            //Loop through the grid of chunks
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    //Whether blocks may be hidden behind blocks of higher render order
                    bool blocksHiddenByOverlapBlock = false;
                    bool blocksHiddenByDefaultBlock = false;
                    //Loop through all the layers
                    for (int layer = world.NumBlockLayers - 1; layer >= 0; layer--)
                    {
                        //Adds block to the grid mesh if the current layer has a block for rendering at that coordinate
                        if (world.GetBlockLayer(layer).IsRenderblock(x + chunkX, y + chunkY))
                        {
                            //Get information on the current block
                            BlockInfo blockInfo = world.GetBlockLayer(layer).GetBlockInfo(x + chunkX, y + chunkY);
                            //Do not generate the block if it is not a multi-tile block and it is completely hidden
                            if (world.DoNotGenerateHiddenBlocks && !blockInfo.MultiBlock)
                            {
                                if (blockInfo.OverlapBlock)
                                {
                                    if (blocksHiddenByOverlapBlock)
                                    {
                                        //Not all overlap blocks are completely hidden, only skip if all the edges are covered as well
                                        byte bitmask = world.GetBlockLayer(layer).GetBitmask(x + chunkX, y + chunkY);
                                        bool noTopBlock = IsBitSet(bitmask, 4);
                                        bool noRightBlock = IsBitSet(bitmask, 5);
                                        bool noBottomBlock = IsBitSet(bitmask, 6);
                                        bool noLeftBlock = IsBitSet(bitmask, 7);
                                        if (!noTopBlock && !noRightBlock && !noBottomBlock && !noLeftBlock)
                                            continue;
                                    }
                                }
                                else if (blocksHiddenByDefaultBlock)
                                    continue;
                            }
                            //Render order of the block 
                            float zBlockOrder = world.GetBlockLayer(layer).ZLayerOrder - world.GetBlockLayer(layer).GetBlockType(x + chunkX, y + chunkY) * world.ZBlockDistance;
                            //Tile variation for that block
                            Vector2 variation = new Vector2(world.GetBlockLayer(layer).GetVariation(x + chunkX, y + chunkY), 0);
                            //Texture position of the block
                            Vector2 texturePosition = new Vector2(blockInfo.TextureXRelativePosition, blockInfo.TextureYRelativePosition) + variation;
                            //Generate the proper block type in the BlockGridMesh
                            if (blockInfo.OverlapBlock)
                            {
                                byte bitmask = world.GetBlockLayer(layer).GetBitmask(x + chunkX, y + chunkY);
                                blockGridMesh.CreateOverlapBlock(x, y, zBlockOrder, bitmask, texturePosition, (byte)layer, blockInfo.TileUnitX, blockInfo.TileUnitY, world.OverlapBlendSquares);
                            }
                            else
                                blockGridMesh.CreateBlock(x, y, zBlockOrder, texturePosition, (byte)layer, blockInfo.TileUnitX, blockInfo.TileUnitY, blockInfo.TextureWidth, blockInfo.TextureHeight);
                            //If a block is not transparent the other blocks behind it may be completely hidden
                            if (!blockInfo.Transparent)
                            {
                                if (blockInfo.OverlapBlock)
                                    blocksHiddenByOverlapBlock = true;
                                else
                                    blocksHiddenByDefaultBlock = true;
                            }
                        }
                    }
                }
            }

            //Update the block grid mesh
            blockGridMesh.UpdateMesh();
            //Set the materials in the MeshRenderer
            UpdateMeshRenderer();
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
        /// Builds the chunk collider
        /// </summary>
        void BuildCollider()
        {
            //Update collider
            //Loop through the grid of chunks
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    //Generate collider paths if there are any collider layers
                    if (world.ColliderLayers.Length != 0)
                        colliderGenerator.GenColliderPaths(x, y, true, new Vector2Int(0, 0));
                }
            }

            //Update the PolygonCollider2D
            if (world.ColliderLayers.Length != 0)
                colliderGenerator.UpdateCollider();
        }

        /// <summary>
        /// Sets the materials in the MeshRenderer depending on the number of submeshes in the BlockGridMesh and which layers contain blocks
        /// </summary>
        void UpdateMeshRenderer()
        {
            //Sets the materials if there are submeshes in the block grid mesh
            int subMeshCount = blockGridMesh.GetMesh().subMeshCount;
            if (subMeshCount > 0)
            {
                //Gets the correct materialCache size based on the number of submeshes
                Material[] tempMaterials = materialCache[subMeshCount - 1];
                //Loops through the number of block layers
                int indexCounter = 0;
                for (int i = 0; i < world.NumBlockLayers; i++)
                {
                    //If the current layer is a material layer (it contains blocks), then add its material to the cache
                    if (blockGridMesh.IsMaterialLayer(i))
                    {
                        tempMaterials[indexCounter] = world.GetBlockLayer(i).Material;
                        indexCounter++;
                    }
                }
                //Sets the materials of the MeshRenderer
                meshRenderer.materials = tempMaterials;
            }
        }
    }

}