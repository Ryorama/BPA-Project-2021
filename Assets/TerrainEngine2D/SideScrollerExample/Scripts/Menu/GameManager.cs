using UnityEngine.SceneManagement;
using UnityEngine;

namespace TerrainEngine2D.MenuDemo
{
    /// <summary>
    /// Manager for the game
    /// </summary>
    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
        public WorldData WorldData;
        public enum SceneIndex { Menu, Game }

        /// <summary>
        /// Create a new world, generating terrain from a random seed
        /// </summary>
        /// <param name="name">The name of the new world</param>
        public void NewWorld(string name)
        {
            WorldData.LoadWorld = false;
            WorldData.SaveWorld = false;
            WorldData.Seed = Random.Range(int.MinValue / 100, int.MaxValue / 100);
            WorldData.Name = name;
            LoadScene(SceneIndex.Game);
        }

        /// <summary>
        /// Load an existing world
        /// </summary>
        /// <param name="worldDirectory">The system path to the world directory</param>
        public void LoadWorld(string worldDirectory)
        {
            WorldData.WorldDirectory = worldDirectory;
            WorldData.LoadWorld = true;
            WorldData.SaveWorld = true;
            LoadScene(SceneIndex.Game);
        }

        /// <summary>
        /// Loads the specified scene
        /// </summary>
        /// <param name="sceneIndex">The index of the scene to be loaded</param>
        private void LoadScene(SceneIndex sceneIndex)
        {
            SceneManager.LoadSceneAsync((int)sceneIndex);
        }

    }
}
