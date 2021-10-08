using System.Collections.Generic;
using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.Lighting
{
    /// <summary>
    /// An advanced 2D block lighting system
    /// </summary>
    public class AdvancedLightSystem : MonoBehaviourSingleton<AdvancedLightSystem>
    {
        [SerializeField]
        private BlockLighting blockLighting;
        /// <summary>
        /// Reference to the main block lighting system
        /// </summary>
        public BlockLighting BlockLighting
        {
            get { return blockLighting; }
        }

        private Dictionary<Vector2Int, LightSource> lights;
        //List of all the static lights (require manual updates)
        private List<MeshLight> meshLights;
        //List of all block lights
        private List<BlockLightSource> blockLights;

        private void OnEnable()
        {
            ChunkLoader.OnChunksLoaded += OnChunksLoaded;
            World.OnBlockPlaced += BlockPlaced;
            World.OnBlockRemoved += BlockRemoved;
        }

        private void OnDisable()
        {
            ChunkLoader.OnChunksLoaded -= OnChunksLoaded;
            World.OnBlockPlaced -= BlockPlaced;
            World.OnBlockRemoved -= BlockRemoved;
        }

        protected override void Awake()
        {
            base.Awake();
            
            meshLights = new List<MeshLight>();
            blockLights = new List<BlockLightSource>();
            lights = new Dictionary<Vector2Int, LightSource>();

            //Set Properties
            blockLighting.IntensityFactor = World.WorldData.IntensityFactor;
            blockLighting.TransmissionFactor = World.WorldData.TransmissionFactor;
        }

        /// <summary>
        /// Check to see if there is a light at a particular position
        /// </summary>
        /// <param name="position">The position to check for the light</param>
        /// <returns>Returns true if there is a light at that position</returns>
        public bool IsLightAt(Vector2Int position)
        {
            LightSource lightSource;
            if (lights.TryGetValue(position, out lightSource))
                return true;
            return false;
        }

        /// <summary>
        /// Removes a light from the world
        /// </summary>
        /// <param name="key">The key/position of the light to be removed</param>
        /// <returns>Returns true if the light was successfully removed</returns>
        public bool RemoveLight(Vector2Int key)
        {
            LightSource lightSource;
            if (lights.TryGetValue(key, out lightSource))
            {
                Destroy(lightSource.gameObject);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Try to get a light using a key/position
        /// </summary>
        /// <param name="key">The key/position of the light</param>
        /// <param name="lightSource">Reference to set the light source</param>
        /// <returns>Returns true if a light was found using that key</returns>
        public bool TryGetLight(Vector2Int key, out LightSource lightSource)
        {
            if (lights.TryGetValue(key, out lightSource))
                return true;
            return false;
        }

        /// <summary>
        /// Enable or disable the lights if they are inside/outside of the loaded chunks
        /// </summary>
        public void UpdateLightSources()
        {
            Vector2 origin = ChunkLoader.Instance.OriginLoadedChunks;
            Vector2 endPoint = ChunkLoader.Instance.EndPointLoadedChunks;
            float chunkSize = ChunkLoader.Instance.ChunkSize;
            foreach (MeshLight light in meshLights)
            {
                if (light.transform.position.x + light.LightRadius < origin.x ||
                    light.transform.position.y + light.LightRadius < origin.y ||
                    light.transform.position.x - light.LightRadius > endPoint.x + chunkSize ||
                    light.transform.position.y - light.LightRadius > endPoint.y + chunkSize)
                    light.transform.gameObject.SetActive(false);
                else if (!light.gameObject.activeInHierarchy)
                    light.transform.gameObject.SetActive(true);
            }
            foreach (BlockLightSource light in blockLights)
            {
                if (light.transform.position.x < origin.x ||
                    light.transform.position.y < origin.y ||
                    light.transform.position.x > endPoint.x + chunkSize ||
                    light.transform.position.y > endPoint.y + chunkSize)
                    light.transform.gameObject.SetActive(false);
                else if (!light.gameObject.activeInHierarchy)
                    light.transform.gameObject.SetActive(true);
            }
        }

        #region Messages

        /// <summary>
        /// A light has been added to the game
        /// </summary>
        /// <param name="lightSource">The light which was added</param>
        /// <param name="key">The key to the light source</param>
        private void LightAdded(MeshLight lightSource)
        {
            meshLights.Add(lightSource);
            if(lightSource.Stationary)
                lights.Add(lightSource.KeyPosition, lightSource);
        }

        /// <summary>
        /// A block light has been added to the game
        /// </summary>
        /// <param name="blockLightSource">The light which was added</param>
        /// <param name="key">The key to the light source</param>
        private void LightAdded(BlockLightSource blockLightSource)
        {
            if (blockLighting.AddLightSource(blockLightSource.KeyPosition, blockLightSource.LightColor))
            {
                blockLights.Add(blockLightSource);
                lights.Add(blockLightSource.KeyPosition, blockLightSource);
            }
            else
                Debug.LogError("Failed to add block light source, there is already a block light source in that position. " +
                    "Consider adding some checks to ensure there are no block light sources in the position you are instantiating lights.");
        }

        /// <summary>
        /// A light has been removed from the game
        /// </summary>
        /// <param name="lightSource">The light which was removed</param>
        private void LightRemoved(MeshLight lightSource)
        {
            meshLights.Remove(lightSource);
            if(lightSource.Stationary)
                lights.Remove(lightSource.KeyPosition);
        }

        /// <summary>
        /// A block light has been removed from the game
        /// </summary>
        /// <param name="blockLightSource">The light which was removed</param>
        private void LightRemoved(BlockLightSource blockLightSource)
        {
            blockLighting.RemoveLightSource(blockLightSource.KeyPosition);
            blockLights.Remove(blockLightSource);
            lights.Remove(blockLightSource.KeyPosition);
        }

        #endregion

        #region Events

        /// <summary>
        /// Update the light source when new chunks are loaded
        /// </summary>
        private void OnChunksLoaded()
        {
            UpdateLightSources();
        }

        /// <summary>
        /// Update the static lights if a block was placed within their vicinity
        /// </summary>
        /// <param name="x">The x coordinate where the block was placed</param>
        /// <param name="y">The y coordinate where the block was placed</param>
        /// <param name="layer">The layer the block was placed in</param>
        /// <param name="blockType">The type of block placed</param>
        private void BlockPlaced(int x, int y, byte layer, byte blockType)
        {
            foreach (MeshLight light in meshLights)
            {
                if (light.gameObject.activeInHierarchy)
                {
                    if (x < light.transform.position.x - light.LightRadius ||
                        x > light.transform.position.x + light.LightRadius ||
                        y < light.transform.position.y - light.LightRadius ||
                        y > light.transform.position.y + light.LightRadius)
                        continue;
                    light.UpdateLight();
                }
            }
        }

        /// <summary>
        /// Update the static lights if a block was removed within their vicinity
        /// </summary>
        /// <param name="x">The x coordinate where the block was removed</param>
        /// <param name="y">The y coordinate where the block was removed</param>
        /// <param name="layer">The layer the block was removed from</param>
        /// <param name="blockType">The type of block removed</param>
        private void BlockRemoved(int x, int y, byte layer, byte blockType)
        {
            foreach (MeshLight light in meshLights)
            {
                if (light.gameObject.activeInHierarchy)
                {
                    if (x < light.transform.position.x - light.LightRadius ||
                        x > light.transform.position.x + light.LightRadius ||
                        y < light.transform.position.y - light.LightRadius ||
                        y > light.transform.position.y + light.LightRadius)
                        continue;
                    light.UpdateLight();
                }
            }
        }

        #endregion

    }
}