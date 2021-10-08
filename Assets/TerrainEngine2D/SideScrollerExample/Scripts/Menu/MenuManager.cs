using UnityEngine;

namespace TerrainEngine2D.MenuDemo
{
    /// <summary>
    /// Controls the entire menu system
    /// </summary>
    public class MenuManager : MonoBehaviourSingleton<MenuManager>
    {
        public enum Menu { Main, WorldSelection, WorldGenerator }

        [SerializeField]
        private GameObject mainMenu;
        [SerializeField]
        private GameObject worldSelectMenu;
        [SerializeField]
        private GameObject worldGeneratorMenu;

        /// <summary>
        /// Load a given menu
        /// </summary>
        /// <param name="menu">Index to the menu being loaded</param>
        public void LoadMenu(Menu menu)
        {
            mainMenu.SetActive(false);
            worldSelectMenu.SetActive(false);
            worldGeneratorMenu.SetActive(false);

            switch (menu)
            {
                case Menu.Main:
                    mainMenu.SetActive(true);
                    break;
                case Menu.WorldSelection:
                    worldSelectMenu.SetActive(true);
                    break;
                case Menu.WorldGenerator:
                    worldGeneratorMenu.SetActive(true);
                    break;
            }
        }

    }
}