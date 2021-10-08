using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TerrainEngine2D.MenuDemo
{
    /// <summary>
    /// Controls the World Generator Menu
    /// </summary>
    public class WorldGeneratorMenu : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField nameInputField;
        [SerializeField]
        private Button generateButton;

        private new string name;

        /// <summary>
        /// Generate a new world with the given name
        /// </summary>
        public void GenerateWorld()
        {
            //If no name is input, then use "WorldX" where X can be any integer value that produces a unique world name
            if (name == "" || name == null)
            {
                string validWorldName = "World";
                int worldNumber = 1;
                while (Serialization.DoesWorldDirectoryExist(validWorldName + worldNumber.ToString()))
                    worldNumber++;
                GameManager.Instance.NewWorld(validWorldName + worldNumber.ToString());
            }
            else
                GameManager.Instance.NewWorld(name);
        }

        /// <summary>
        /// Set the name when the input field value is changed
        /// </summary>
        public void NameInputFieldEdited()
        {
            //Don't allow a world to be generated if one of the same name already exists (to prevent it from being overwritten)
            if (!Serialization.DoesWorldDirectoryExist(nameInputField.text) || nameInputField.text == "")
            {
                name = nameInputField.text;
                generateButton.interactable = true;
            }
            else
            {
                generateButton.interactable = false;
                Debug.Log("A world already exists with that name, please choose another name");
            }
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