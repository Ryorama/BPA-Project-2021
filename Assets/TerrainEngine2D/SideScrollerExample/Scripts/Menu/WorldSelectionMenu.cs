using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TMPro;

namespace TerrainEngine2D.MenuDemo
{
    /// <summary>
    /// Controls the World Selection Menu
    /// </summary>
    public class WorldSelectionMenu : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown worldDropdown;
        [SerializeField]
        private Button loadButton;

        private List<string> worldNames;

        private string selectedWorldName;
        private bool noWorlds;

        private void OnEnable()
        {
            // Find all of the available worlds for loading
            worldDropdown.ClearOptions();
            DirectoryInfo saveDirectory = new DirectoryInfo(Serialization.GetSaveDirectory());
            if(Directory.Exists(saveDirectory.FullName))
            {
                DirectoryInfo[] worldDirectories = saveDirectory.GetDirectories();
                // If existing worlds are found, then add them as dropdown options
                if (worldDirectories.Length != 0)
                {
                    worldNames = new List<string>(worldDirectories.Length);
                    for (int i = 0; i < worldDirectories.Length; i++)
                    {
                        if (Serialization.IsValidWorldDirectory(worldDirectories[i].FullName))
                        {
                            //Check to make sure the saved world files were generated from the same World Data object
                            //This is to prevent errors from trying to load worlds generated in other projects with different settings
                            string SID;
                            if (Serialization.GetSID(Serialization.GetSaveDirectory() + worldDirectories[i].Name + "/", out SID) && SID == GameManager.Instance.WorldData.GUID)
                                worldNames.Add(worldDirectories[i].Name);
                        }
                    }
                    if (worldNames.Count > 0)
                    {
                        worldDropdown.AddOptions(worldNames);
                        selectedWorldName = worldNames[worldDropdown.value];
                        loadButton.interactable = true;
                        return;
                    }
                }
            }
            loadButton.interactable = false;
            Debug.Log("No worlds found");
            noWorlds = true;
        }

        /// <summary>
        /// Load the selected dropdown option
        /// </summary>
        public void LoadWorld()
        {
            if (noWorlds)
                return;
            GameManager.Instance.LoadWorld(Serialization.GetSaveDirectory() + selectedWorldName + "/");
        }

        /// <summary>
        /// Set the selected world name when a dropdown option is selected
        /// </summary>
        public void OnWorldDropdownValueChanged()
        {
            selectedWorldName = worldNames[worldDropdown.value];
        }

        /// <summary>
        /// Return to the main menu 
        /// </summary>
        public void Back()
        {
            MenuManager.Instance.LoadMenu(MenuManager.Menu.Main);
        }
    }
}
