using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Sets the image of the GridSelector
    /// </summary>
    public class GridSelectorImageSetter : MonoBehaviourSingleton<GridSelectorImageSetter>
    {
        private World world;
        private WorldInputHandler worldInputHandler;
        [Tooltip("Material image of the default grid selector")]
        [SerializeField]
        private Material gridSelectionMat;
        [Tooltip("Material used for rendering fluid")]
        [SerializeField]
        private Material fluidMat;
        [Tooltip("Material used to set the image for a light source")]
        [SerializeField]
        private Material lightImageMat;
        private MeshRenderer meshRenderer;
        
        //Mesh for building the grid selector image
        private Mesh mesh;
        private BlockGridMesh gridSelectorMesh;

        private bool initialized;

        protected override void Awake()
        {
            base.Awake();
            mesh = GetComponent<MeshFilter>().mesh;
            meshRenderer = GetComponent<MeshRenderer>();

            gameObject.layer = LayerMask.NameToLayer("Ignore Lighting");
        }

        private void Start()
        {
            if (!initialized)
                Initialize();
        }

        /// <summary>
        /// Initialize the data required to set the Grid Selector's image
        /// </summary>
        public void Initialize()
        {
            if (initialized)
                return;
            worldInputHandler = WorldInputHandler.Instance;
            world = World.Instance;
            //Setup the grid selector mesh sized according to the maximum modifying radius
            gridSelectorMesh = new BlockGridMesh(mesh, World.WorldData.MaxModifyRadius, world.ZBlockDistance, true, 1, true);
            //Start with image set to default
            SetImageToDefault();
            initialized = true;
        }

        /// <summary>
        /// Sets the Grid Selector to its default image
        /// </summary>
        public void SetImageToDefault()
        {
            //Reset the Z position
            transform.position = new Vector3(transform.position.x, transform.position.y, world.EndZPoint - world.ZBlockDistance * world.ZLayerFactor);
            //Sets the MeshRenderer material to that of the default Grid Selector Material
            meshRenderer.material = gridSelectionMat;
            //Set the position of the image according to the current modifying radius
            int x = -worldInputHandler.ModifyRadius;
            int y = -worldInputHandler.ModifyRadius;
            //Set the size of the image according to the current modifying radius
            int width = worldInputHandler.ModifyRadius * 2 + 1;
            int height = worldInputHandler.ModifyRadius * 2 + 1;
            //Generate the image
            for(int i = x; i < x + width; i++)
            {
                for(int j = y; j < y + height; j++)
                {
                    //Set the color and alpha 
                    float alpha = Mathf.RoundToInt(255 * (1 - (Mathf.Sqrt(Mathf.Pow(i , 2) + Mathf.Pow(j , 2)) / (worldInputHandler.ModifyRadius + 1))));
                    byte a = (byte)Mathf.Max(10, alpha);
                    Color32 color = new Color32(255, 255, 255, a);
                    gridSelectorMesh.CreateBlock(i, j, 0, new Vector2(0, 0), 0, 1, 1, 1, 1, color);
                }
            }           
            gridSelectorMesh.UpdateMesh();
        }

        /// <summary>
        /// Sets the Grid Selector to a fluid image
        /// </summary>
        public void SetImageToFluidBlock(Color color)
        {
            //Set the z position so that the Grid Selector image is rendered in the fluid layer
            transform.position = new Vector3(transform.position.x, transform.position.y, world.GetBlockLayer(world.FluidLayer).ZLayerOrder - world.ZBlockDistance / 4f);
            //Sets the MeshRenderer material to that of the default fluid material
            meshRenderer.material = fluidMat;
            //Set the position of the image according to the current modifying radius
            int x = -worldInputHandler.ModifyRadius;
            int y = -worldInputHandler.ModifyRadius;
            //Set the size of the image according to the current modifying radius
            int width = worldInputHandler.ModifyRadius * 2 + 1;
            int height = worldInputHandler.ModifyRadius * 2 + 1;
            //Generate the image
            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    gridSelectorMesh.CreateBlock(i, j, 0, new Vector2(0, 0), 0, 1, 1, 1, 1, color);
                }
            }
            gridSelectorMesh.UpdateMesh();
        }

        /// <summary>
        /// Sets the Grid Selector to a light source image
        /// </summary>
        public void SetImageToLightSource()
        {
            //Set the z position so that the Grid Selector image is rendered in the light layer
            transform.position = new Vector3(transform.position.x, transform.position.y, worldInputHandler.LightZPosition);
            //Sets the MeshRenderer material to render a light source image
            meshRenderer.material = lightImageMat;
            //Set the position of the image according to the current modifying radius
            int x = -worldInputHandler.ModifyRadius;
            int y = -worldInputHandler.ModifyRadius;
            //Set the size of the image according to the current modifying radius
            int width = worldInputHandler.ModifyRadius * 2 + 1;
            int height = worldInputHandler.ModifyRadius * 2 + 1;
            //Generate the image
            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    gridSelectorMesh.CreateBlock(i, j, 0, new Vector2(0, 0), 0, 1, 1, 1, 1);
                }
            }
            gridSelectorMesh.UpdateMesh();
        }

        /// <summary>
        /// Set the Grid Selector's image to the current selected block
        /// </summary>
        /// <param name="blockInfo">The information of the current selected block</param>
        /// <param name="material">The material of the current layer</param>
        /// <param name="blockType">The type of block</param>
        /// <param name="layer">The layer of the block</param>
        /// <param name="zLayerOrder">The z order of the layer</param>
        public void SetImageToBlock(BlockInfo blockInfo, Material material, byte blockType, byte layer, float zLayerOrder)
        {
            //Set the z position so that the Grid Selector image is rendered just in front of the selected block type
            transform.position = new Vector3(transform.position.x, transform.position.y, -world.ZBlockDistance * 3 / 4f);
            //Sets the MeshRenderer material to the selected layers material
            meshRenderer.material = material;
            //Calculate the exact z order for that block type
            float zBlockOrder = zLayerOrder - blockType * world.ZBlockDistance;
            //-----Generate the image-----
            //Creates a square of the selected block type sized appropriately with the current modifying radius
            for (int x = -worldInputHandler.ModifyRadius; x <= worldInputHandler.ModifyRadius; x++)
            {
                for (int y = -worldInputHandler.ModifyRadius; y <= worldInputHandler.ModifyRadius; y++)
                {
                    //Generates a random tile variation
                    Vector2 variation = new Vector2(Random.Range(0, blockInfo.NumVariations), 0);
                    //Texture position of the block
                    Vector2 texturePosition = new Vector2(blockInfo.TextureXRelativePosition, blockInfo.TextureYRelativePosition) + variation;
                    //If the block is an Overlap Block then it generates the appropriate overlapping border for the square image
                    if (blockInfo.OverlapBlock)
                    {
                        //Determines the bitmask
                        byte bitmask = 0;
                        if (x == -worldInputHandler.ModifyRadius)
                            bitmask |= 128; //left
                        if (x == worldInputHandler.ModifyRadius)
                            bitmask |= 32; //right
                        if (y == -worldInputHandler.ModifyRadius)
                            bitmask |= 64; //bottom
                        if (y == worldInputHandler.ModifyRadius)
                            bitmask |= 16; //top
                        gridSelectorMesh.CreateOverlapBlock(x, y, zBlockOrder, bitmask, texturePosition, 0, blockInfo.TileUnitX, blockInfo.TileUnitY, world.OverlapBlendSquares);
                    }
                    else
                    {
                        gridSelectorMesh.CreateBlock(x, y, zBlockOrder, texturePosition, 0, blockInfo.TileUnitX, blockInfo.TileUnitY, blockInfo.TextureWidth, blockInfo.TextureHeight);
                    }

                }
            }
            gridSelectorMesh.UpdateMesh();
        }

    }
}