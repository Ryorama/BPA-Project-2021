using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D {

    /// <summary>
    /// Handles chunk loading and unloading
    /// </summary>
    public class ChunkLoader : MonoBehaviourSingleton<ChunkLoader>
    {
        private World world;
        [SerializeField]
        private GameObject chunkPrefab;
        public GameObject ChunkPrefab
        {
            get { return chunkPrefab; }
        }
        [HideInInspector]
        [SerializeField]
        private Transform loadTransform;
        /// <summary>
        /// The transform which to load the chunks around
        /// </summary>
        public Transform LoadTransform
        {
            get { return loadTransform; }
        }

        private float loadRate = 0.1f;
        /// <summary>
        /// Rate of reloading chunks (in seconds)
        /// </summary>
        public float LoadRate
        {
            get { return loadRate; }
        }
        //The world chunk storage
        private Chunk[,] chunks;

        private int chunkSize = 16;
        /// <summary>
        /// The size of the chunk in blocks (one side length)
        /// </summary>
        public int ChunkSize
        {
            get { return chunkSize; }
        }
        private int numChunks;
        /// <summary>
        /// Number of chunks loaded in the scene
        /// </summary>
        public int NumChunks
        {
            get { return numChunks; }
        }
        private int maxChunks;
        /// <summary>
        /// Max number of chunks that can be loaded at once
        /// </summary>
        public int MaxChunks
        {
            get { return maxChunks; }
        }

        private int loadedWorldWidth;
        /// <summary>
        /// Width of the world loaded in chunks
        /// </summary>
        public int LoadedWorldWidth
        {
            get { return loadedWorldWidth; }
        }
        private int loadedWorldHeight;
        /// <summary>
        /// Height of the world loaded in chunks
        /// </summary>
        public int LoadedWorldHeight
        {
            get { return loadedWorldHeight; }
        }

        private int horizontalChunkLoadDist = 48;
        /// <summary>
        /// Horizontal Chunk loading distance
        /// Horizontal distance from the center of the camera used to determine which Chunk column to load and unload
        /// </summary>
        public int HorizontalChunkLoadDist
        {
            get { return horizontalChunkLoadDist; }
        }
        private int verticalChunkLoadDist;
        /// <summary>
        /// Vertical Chunk loading distance
        /// Vertical distance from the center of the camera used to determine which Chunk row to load and unload
        /// </summary>
        public int VerticalChunkLoadDist
        {
            get { return verticalChunkLoadDist; }
        }
        private Vector2Int originLoadedChunks;
        /// <summary>
        /// Coordinates of the bottom left point of the current loaded world
        /// </summary>
        public Vector2Int OriginLoadedChunks
        {
            get { return originLoadedChunks; }
        }
        private Vector2Int endPointLoadedChunks;
        /// <summary>
        /// Coordinates of the top right point of the current loaded world
        /// </summary>
        public Vector2Int EndPointLoadedChunks
        {
            get { return endPointLoadedChunks; }
        }
        //Position to load the initial chunks
        private Vector2Int chunkLoadPosition;
        /// <summary>
        /// Actions invoked by the ChunkLoader
        /// </summary>
        public delegate void ChunkAction();
        /// <summary>
        /// Event called when chunks have been loaded/unloaded
        /// </summary>
        public static event ChunkAction OnChunksLoaded;

        protected override void Awake()
        {
            base.Awake();
            if (chunkPrefab == null)
                throw new UnassignedReferenceException("The Chunk Prefab is missing! Assign the Chunk Prefab to the corresponding field on the ChunkLoader.");

            //Set Properties
            chunkSize = World.WorldData.ChunkSize;
            loadRate = World.WorldData.ChunkLoadRate;
            horizontalChunkLoadDist = World.WorldData.ChunkLoadDistance;

            world = GetComponentInParent<World>();
            //Round the horizontal chunk loading distance up to the nearest Chunk Size
            horizontalChunkLoadDist = Mathf.CeilToInt(horizontalChunkLoadDist / (float)chunkSize) * chunkSize;
            //Calculate the vertical chunk loading distance using the screen aspect ratio
            float screenAspectRatio = (float)Screen.height / Screen.width;
            float rawVerticalChunkLoadDist = horizontalChunkLoadDist * screenAspectRatio;
            //Round to nearest Chunk Size
            verticalChunkLoadDist = Mathf.CeilToInt(rawVerticalChunkLoadDist / chunkSize) * chunkSize;

            //Calculate the width and height of the loaded world (in Unity units (blocks))
            loadedWorldWidth = (Mathf.CeilToInt(horizontalChunkLoadDist / (float)chunkSize) * 2 + 1) * chunkSize;
            loadedWorldHeight = (Mathf.CeilToInt(VerticalChunkLoadDist / (float)chunkSize) * 2 + 1) * chunkSize;
            loadedWorldWidth = Mathf.Min(loadedWorldWidth, world.WorldWidth);
            loadedWorldHeight = Mathf.Min(loadedWorldHeight, world.WorldHeight);
            //Calculate the maximum number of chunks that can be instantiated
            maxChunks = Mathf.CeilToInt(loadedWorldWidth / (float)chunkSize) * Mathf.CeilToInt(loadedWorldHeight / (float)chunkSize);
            //Allocate memory to store chunks for easy access based on a coordinate grid system
            chunks = new Chunk[world.WorldWidth / chunkSize, world.WorldHeight / chunkSize];
        }

        /// <summary>
        /// Set the initial world coordinates and start loading chunks
        /// </summary>
        public void BeginChunkLoading()
        {
            if (loadTransform == null)
            {
                Debug.LogWarning("The Load Transform is not set, setting it to the WorldCamera by default.");
                loadTransform = Camera.main.transform;
            }
            //Set the initial postion of the load transform and load the chunks around it
            LoadChunksAtPosition((int)loadTransform.position.x, (int)loadTransform.position.y, true);
            //Start updating chunks
            InvokeRepeating("UpdateChunks", 0, loadRate);
        }

        /// <summary>
        /// Sets a new position for the LoadTransform and loads the chunks around it
        /// </summary>
        /// <param name="posX">x-coordinate of position</param>
        /// <param name="posY">y-coordinate of position</param>
        /// <param name="initialLoad">Whether this function is being called the first time the chunks are loaded</param>
        public void LoadChunksAtPosition(int posX, int posY, bool initialLoad = false)
        {
            loadTransform.position = new Vector3(posX, posY, loadTransform.position.z);

            //Set the LoadTransform's position to ensure it is within the world bounds
            int xPosition = (int)Mathf.Clamp(loadTransform.position.x, horizontalChunkLoadDist, world.WorldWidth - horizontalChunkLoadDist - chunkSize);
            int yPosition = (int)Mathf.Clamp(loadTransform.position.y, verticalChunkLoadDist, world.WorldHeight - verticalChunkLoadDist - chunkSize);
            chunkLoadPosition = new Vector2Int(xPosition, yPosition);

            //Set the intial world coordinates in chunk units
            //The x point of the origin is the x coordinate of the furthest chunk within horizontal range, left of the loadTransform
            int originXChunks = Mathf.FloorToInt((chunkLoadPosition.x - horizontalChunkLoadDist) / chunkSize);
            int originYChunks = Mathf.FloorToInt((chunkLoadPosition.y - verticalChunkLoadDist) / chunkSize);
            //The x point of the end point is the x coordinate of the of the furthest chunk within horizontal range, right of the loadTransform
            int endPointXChunks = originXChunks + Mathf.CeilToInt(horizontalChunkLoadDist / (float)chunkSize) * 2;
            int endPointYChunks = originYChunks + Mathf.CeilToInt(verticalChunkLoadDist / (float)chunkSize) * 2;
            //Ensure chunks loaded are within the world bounds
            originXChunks = Mathf.Clamp(originXChunks, 0, world.WorldWidth / chunkSize - 1);
            originYChunks = Mathf.Clamp(originYChunks, 0, world.WorldHeight / chunkSize - 1);
            endPointXChunks = Mathf.Clamp(endPointXChunks, 0, world.WorldWidth / chunkSize - 1);
            endPointYChunks = Mathf.Clamp(endPointYChunks, 0, world.WorldHeight / chunkSize - 1);

            //Load in all the chunks
            if (initialLoad)
            {
                //Load in chunks at a new position using new chunks
                for (int x = originXChunks; x <= endPointXChunks; x++)
                {
                    for (int y = originYChunks; y <= endPointYChunks; y++)
                    {
                        LoadChunk(x, y);
                    }
                }
            }
            else
            {
                //Load chunks in a new position by reusing old chunks
                for (int x = 0; x <= endPointXChunks - originXChunks; x++)
                {
                    for (int y = 0; y <= endPointYChunks - originYChunks; y++)
                    {
                        LoadChunk(originXChunks + x, originYChunks + y, true, originLoadedChunks.x / chunkSize + x, originLoadedChunks.y / chunkSize + y);
                    }
                }
            }
            //Store the world coordinates converted to block units (Unity units)
            originLoadedChunks = new Vector2Int(originXChunks, originYChunks) * chunkSize;
            endPointLoadedChunks = new Vector2Int(endPointXChunks, endPointYChunks) * chunkSize;

            //Call event for chunk loading
            OnChunksLoaded();
        }

        /// <summary>
        /// Method for repeatedly updating the chunks every 'loadRate' in seconds
        /// </summary>
        void UpdateChunks()
        {
            //-----Check Top Row-----
            //Calculate the vertical distance of the top bordering chunks to the ChunkLoader
            float distFromChunkLoader = endPointLoadedChunks.y + chunkSize - loadTransform.position.y;
            //Check to see if the ChunkLoader has reached the top of the world
            bool withinWorldBounds = (int)endPointLoadedChunks.y / chunkSize < chunks.GetLength(1) - 1;
            //Check if chunks were loaded
            bool chunksLoaded = false;
            //Load a new top row of chunks if that row is within range and within the bounds of the world
            if (distFromChunkLoader < verticalChunkLoadDist && withinWorldBounds)
            {
                //Get the vertical y coordinate (in chunk units) of the top row to be loaded
                int yTop = (int)endPointLoadedChunks.y / chunkSize + 1;
                //Get the vertical y coordinate (in chunk units) of the bottom row to be unloaded
                int yBottom = (int)originLoadedChunks.y / chunkSize;

                //Loop through all the horizontal chunk x coordinates of the loaded world
                for (int x = (int)originLoadedChunks.x / chunkSize; x <= endPointLoadedChunks.x / chunkSize; x++)
                {
                    //If there is currently no chunk loaded in that position, load the new chunk up top
                    if (chunks[x, yTop] == null)
                    {
                        LoadChunk(x, yTop, true, x, yBottom);
                        chunksLoaded = true;
                    }
                }
                //Set the new world coordinates of the loaded chunks
                endPointLoadedChunks.y = yTop * chunkSize;
                originLoadedChunks.y = (yBottom + 1) * chunkSize;
            }
            else
            {
                //-----Check Bottom Row-----
                distFromChunkLoader = loadTransform.position.y - originLoadedChunks.y;
                withinWorldBounds = (int)originLoadedChunks.y / chunkSize > 0;
                if (distFromChunkLoader < verticalChunkLoadDist && withinWorldBounds)
                {
                    int yTop = (int)endPointLoadedChunks.y / chunkSize;
                    int yBottom = (int)originLoadedChunks.y / chunkSize - 1;
                    for (int x = (int)originLoadedChunks.x / chunkSize; x <= endPointLoadedChunks.x / chunkSize; x++)
                    {
                        if (chunks[x, yBottom] == null)
                        {
                            LoadChunk(x, yBottom, true, x, yTop);
                            chunksLoaded = true;
                        }
                    }
                    endPointLoadedChunks.y = (yTop - 1) * chunkSize;
                    originLoadedChunks.y = yBottom * chunkSize;
                }
            }
            //-----Check Left Column-----
            distFromChunkLoader = loadTransform.position.x - originLoadedChunks.x;
            withinWorldBounds = (int)originLoadedChunks.x / chunkSize > 0;
            if (distFromChunkLoader < horizontalChunkLoadDist && withinWorldBounds)
            {
                int xLeft = (int)originLoadedChunks.x / chunkSize - 1;
                int xRight = (int)endPointLoadedChunks.x / chunkSize;
                for (int y = (int)originLoadedChunks.y / chunkSize; y <= endPointLoadedChunks.y / chunkSize; y++)
                {
                    if (chunks[xLeft, y] == null)
                    {
                        LoadChunk(xLeft, y, true, xRight, y);
                        chunksLoaded = true;
                    }
                }
                endPointLoadedChunks.x = (xRight - 1) * chunkSize;
                originLoadedChunks.x = xLeft * chunkSize;
            }
            else
            {
                //-----Check Right Column-----
                distFromChunkLoader = endPointLoadedChunks.x + chunkSize - loadTransform.position.x;
                withinWorldBounds = (int)endPointLoadedChunks.x / chunkSize < chunks.GetLength(0) - 1;
                if (distFromChunkLoader < horizontalChunkLoadDist && withinWorldBounds)
                {
                    int xLeft = (int)originLoadedChunks.x / chunkSize;
                    int xRight = (int)endPointLoadedChunks.x / chunkSize + 1;
                    for (int y = (int)originLoadedChunks.y / chunkSize; y <= endPointLoadedChunks.y / chunkSize; y++)
                    {
                        if (chunks[xRight, y] == null)
                        {
                            LoadChunk(xRight, y, true, xLeft, y);
                            chunksLoaded = true;
                        }
                    }
                    endPointLoadedChunks.x = xRight * chunkSize;
                    originLoadedChunks.x = (xLeft + 1) * chunkSize;
                }
            }
            //If any chunks were loaded/unloaded call the event
            if (chunksLoaded)
            {
                //Call event for chunk loading
                OnChunksLoaded();
            }
        }

        /// <summary>
        /// Loads and unloads chunks
        /// </summary>
        /// <param name="x">X coordinate of chunk</param>
        /// <param name="y">Y coordinate of chunk</param>
        /// <param name="unload">Unload a chunk</param>
        /// <param name="unloadX">X coordinate of the chunk to unload</param>
        /// <param name="unloadY">Y coordinate of the chunk to unload</param>
        /// <remarks>All coordinates are in chunk units for indexing</remarks>
        public void LoadChunk(int x, int y, bool unload = false, int unloadX = 0, int unloadY = 0)
        {
            GameObject tempChunk;
            //If chunk is avalible for reuse
            if (unload && chunks[unloadX, unloadY] != null)
            {
                //Reuse the chunk
                chunks[x, y] = chunks[unloadX, unloadY];
                //Clear old position 
                chunks[unloadX, unloadY] = null;
                //Regenerate the chunk (mesh and transform)
                chunks[x, y].ReGenerate(x * chunkSize, y * chunkSize);
            }
            //Check if maximum amount of chunks have not been instantiated
            else if (NumChunks < MaxChunks)
            {
                //Instantiate a new chunk
                tempChunk = Instantiate(chunkPrefab, new Vector3(x * chunkSize, y * chunkSize, 0), new Quaternion(0, 0, 0, 0), transform) as GameObject;
                //Store the new chunk in the array
                chunks[x, y] = tempChunk.GetComponent<Chunk>();
                //Generate the chunk (mesh and transform)
                chunks[x, y].InitializeChunk(chunkSize, x * chunkSize, y * chunkSize);
                numChunks++;
            }
            else
                Debug.LogError("Not enough chunks; Maximum chunks: " + MaxChunks + " Number of chunks: " + NumChunks);
        }

        /// <summary>
        /// Updates the chunk of a specified coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="fluidChunk">Whether updating a fluid chunk or not</param>
        /// <remarks>Input the position of a modified block</remarks>
        public void UpdateChunk(int x, int y, bool fluidChunk = false)
        {
            //Gets the coordinates of the bounding chunk
            int chunkX = x / chunkSize;
            int chunkY = y / chunkSize;
            //Return if there is no chunk loaded at that position
            if (ReferenceEquals(chunks[chunkX, chunkY], null))
                return;
            //If updating the fluid chunk
            if (fluidChunk)
            {
                chunks[chunkX, chunkY].FluidChunk.Update = true;
                return;
            }

            //Set the chunk for updating
            chunks[chunkX, chunkY].Update = true;

            bool updateTopChunk = false;
            bool updateBottomChunk = false;
            bool updateLeftChunk = false;
            bool updateRightChunk = false;

            //-----Update adjacent chunks if the coordinate block is bordering that edge-----
            //----Check the top chunk----
            //Y coordinate is at the top border of the chunk
            bool blockBordersEdge = y == chunkSize * chunkY + chunkSize - 1;
            //The chunk is at the top border of the world
            bool edgeOfWorld = chunkY == chunks.GetLength(1) - 1;
            //The above chunk exists
            bool chunkExists = false;
            if(!edgeOfWorld)
                chunkExists = !ReferenceEquals(chunks[chunkX, chunkY + 1], null);

            //Update the top chunk
            if (blockBordersEdge && !edgeOfWorld && chunkExists)
            {
                chunks[chunkX, chunkY + 1].Update = true;
                updateTopChunk = true;
            }
            //Bottom
            else if (y == chunkSize * chunkY && chunkY != 0 && !ReferenceEquals(chunks[chunkX, chunkY - 1], null))
            {
                chunks[chunkX, chunkY - 1].Update = true;
                updateBottomChunk = true;
            }
            //Left
            if (x == chunkSize * chunkX && chunkX != 0 && !ReferenceEquals(chunks[chunkX - 1, chunkY], null))
            {
                chunks[chunkX - 1, chunkY].Update = true;
                updateLeftChunk = true;
            }
            //Right
            else if (x == chunkSize * chunkX + chunkSize - 1 && chunkX != chunks.GetLength(0) - 1 && !ReferenceEquals(chunks[chunkX + 1, chunkY], null))
            {
                chunks[chunkX + 1, chunkY].Update = true;
                updateRightChunk = true;
            }

            //If two adjacent chunks are updated, then the coordinate must refer to a corner block in the chunk
            //Update the corner chunk adjacent to those two updated chunks

            //If both the top chunk and left chunk are updated
            if (updateTopChunk && updateLeftChunk)
                //Update the top left chunk
                chunks[chunkX - 1, chunkY + 1].Update = true;
            else if (updateTopChunk && updateRightChunk)
                chunks[chunkX + 1, chunkY + 1].Update = true;
            else if (updateBottomChunk && updateLeftChunk)
                chunks[chunkX - 1, chunkY - 1].Update = true;
            else if (updateBottomChunk && updateRightChunk)
                chunks[chunkX + 1, chunkY - 1].Update = true;
        }
    }
}

