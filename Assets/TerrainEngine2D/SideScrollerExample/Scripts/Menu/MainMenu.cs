using UnityEngine;

namespace TerrainEngine2D.MenuDemo
{
    /// <summary>
    /// Controls the Main Menu
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        /// <summary>
        /// Open the World Generator menu
        /// </summary>
        public void NewGame()
        {
            MenuManager.Instance.LoadMenu(MenuManager.Menu.WorldGenerator);
        }

        /// <summary>
        /// Open the World Selection menu
        /// </summary>
        public void LoadGame()
        {
            MenuManager.Instance.LoadMenu(MenuManager.Menu.WorldSelection);
        }

        /// <summary>
        /// Exit the game
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("Quit Application");
            Application.Quit();
        }
    }
}