using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    /// <summary>
    /// A basic 2D block lighting system
    /// </summary>
    public class LightSystem : MonoBehaviourSingleton<LightSystem>
    {
        private World world;
        //Reference to the ChunkLoader for information on the currently loaded Chunks
        private ChunkLoader chunkLoader;

        private int lightBleed = 2;
        /// <summary>
        /// The number of blocks from the edge of the terrain for light to illuminate
        /// Higher value means more blocks illuminated further from the edge of the terrain
        /// </summary>
        public int LightBleed
        {
            get { return lightBleed; }
        }
        //Whether the light data was initialized 
        public bool Initialized { get; set; }
        //Render Texture for generating the shadowTexture
        private RenderTexture renderTexture;
        //The texture of the shadow mask
        private Texture2D shadowTexture;
        //Mesh properties
        private Mesh mesh;
        private Vector3[] vertices;
        private Vector2[] uvs;
        private int[] triangles;
        //Light data for the shadowTexture
        private Color[] tempLightData;
        //Light data for the whole world
        private Color32[,] lightMap;
        //Size of the shadow mask
        private int width, height;
        //Whether to update the light system
        private bool update;

        public void OnEnable()
        {
            //Update the lighting system when chunks are loaded
            ChunkLoader.OnChunksLoaded += UpdateLightSystem;
        }

        public void OnDisable()
        {
            ChunkLoader.OnChunksLoaded -= UpdateLightSystem;
        }

        protected override void Awake()
        {
            base.Awake();
            mesh = GetComponent<MeshFilter>().mesh;
            //Set Properties
            lightBleed = World.WorldData.LightBleedAmount;
        }

        public void Start()
        {
            world = World.Instance;
            //Set the z position/render order for the light system
            transform.position = new Vector3(transform.position.x, transform.position.y, world.EndZPoint - world.ZBlockDistance * World.LIGHT_SYSTEM_Z_ORDER);
            chunkLoader = ChunkLoader.Instance;
            //Set the size of the shadow mask
            width = chunkLoader.LoadedWorldWidth;
            height = chunkLoader.LoadedWorldHeight;

            renderTexture = new RenderTexture(width, height, 0);
            shadowTexture = new Texture2D(width, height);
            tempLightData = new Color[width * height];
            //Clamp the shadow texture to prevent the texture from repeating
            shadowTexture.wrapMode = TextureWrapMode.Clamp;
            //Setup the renderer to render the shadow texture
            GetComponent<Renderer>().material.mainTexture = shadowTexture;

            //-----Generate the mesh-----
            //Simple rectangle to mask the loaded world

            //Setup vertices array
            vertices = new Vector3[4];
            vertices[0] = new Vector3(0, 0, 0);
            vertices[1] = new Vector3(0, height, 0);
            vertices[2] = new Vector3(width, height, 0);
            vertices[3] = new Vector3(width, 0, 0);
            //Setup triangles array
            triangles = new int[6];
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 2;
            triangles[4] = 3;
            triangles[5] = 0;
            //Setup uvs array
            uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(0, 1);
            uvs[2] = new Vector2(1, 1);
            uvs[3] = new Vector2(1, 0);
            //Set the mesh properties
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            //No need to keep the data in memory
            vertices = null;
            triangles = null;
            uvs = null;
        }

        /// <summary>
        /// Setup the light map data
        /// </summary>
        /// <param name="world">Reference to the world</param>
        public void Initialize(World world)
        {
            if (Initialized)
                return;

            if(world == null)
                this.world = world;
            //Initialize the light map for the world
            lightMap = new Color32[world.WorldWidth, world.WorldHeight];
            for (int x = 0; x < world.WorldWidth; x++)
            {
                for (int y = 0; y < world.WorldHeight; y++)
                {
                    //Add a black pixel to the lightmap for blocks in the lightlayer
                    if (world.GetBlockLayer(world.LightLayer).IsBlockAt(x, y))
                        //Set the alpha of the pixels depending on their distance from air (greater distance from air means higher alpha)
                        lightMap[x, y] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(world.GetBlockDistanceFromAir(x, y) / (float)lightBleed * 255));
                }
            }
            update = true;
            Initialized = true;
        }

        /// <summary>
        /// Set the light system for an update
        /// </summary>
        public void UpdateLightSystem()
        {
            update = true;
        }

        private void LateUpdate()
        {
            //Check if the light system needs to update and the lighting is not disabled
            if (update && !world.LightingDisabled)
            {
                //Update the position of the world and regenerate the shadow mask
                transform.position = new Vector3(chunkLoader.OriginLoadedChunks.x, chunkLoader.OriginLoadedChunks.y, transform.position.z);
                GenerateShadowMask(lightMap);
                update = false;
            }
        }

        /// <summary>
        /// Generate the shadow mask
        /// </summary>
        /// <param name="lightMap2D">The light data for the whole world</param>
        public void GenerateShadowMask(Color32[,] lightMap2D)
        {
            //Get the light data
            Color[] lightData = SectionLightMap(lightMap2D);
            //Generate a new shadow texture from the light data
            RenderTexture.active = renderTexture;
            shadowTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shadowTexture.SetPixels(lightData);
            shadowTexture.Apply();
            RenderTexture.active = null;
        }

        /// <summary>
        /// Update the lightmap data when blocks change in the lightLayer
        /// </summary>
        /// <param name="posX">The x position of the block change</param>
        /// <param name="posY">The y position of the block change</param>
        /// <param name="width">The width of the block (in block units)</param>
        /// <param name="height">The height of the block (in block units)</param>
        public void UpdateLighting(int posX, int posY, int width, int height)
        {
            if (!world.LightingDisabled)
            {
                //Update the lightmap data for all blocks near the edge
                for (int x = posX - lightBleed; x <= posX + width +  lightBleed; x++)
                {
                    for (int y = posY - lightBleed; y <= posY + height + lightBleed; y++)
                    {
                        //Skip block if it is outside the world bounds
                        if (x < 0 || x >= world.WorldWidth || y < 0 || y >= world.WorldHeight)
                            continue;
                        //Calculate a new pixel color if there is a block at the current position else clear the pixel
                        if (world.GetBlockLayer(world.LightLayer).IsBlockAt(x, y))
                        {
                            lightMap[x, y] = new Color(0, 0, 0, world.GetBlockDistanceFromAir(x, y) / (float)lightBleed);
                        }
                        else
                            lightMap[x, y] = Color.clear;
                    }
                }
                //Update the shadow mask
                update = true;
            }
        }

        /// <summary>
        /// Sections the light data from the light map
        /// </summary>
        /// <param name="lightMap2D">The light data for the whole world</param>
        /// <returns>Returns the sectioned light data</returns>
        Color[] SectionLightMap(Color32[,] lightMap2D)
        {
            //Current position of the shadow mask
            int posX = (int)transform.position.x;
            int posY = (int)transform.position.y;

            int index = 0;
            //Grab the a section of the light data from the current position of the loaded world
            for (int y = posY; y < posY + height; y++)
            {
                for (int x = posX; x < posX + width; x++)
                {
                    //Set the light data to the array
                    tempLightData[index] = lightMap2D[x, y];
                    index++;
                }
            }
            return tempLightData;
        }

    }

}