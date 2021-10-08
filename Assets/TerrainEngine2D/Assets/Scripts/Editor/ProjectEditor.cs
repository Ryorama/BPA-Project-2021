using System.IO;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.Editor
{
    /// <summary>
    /// A collection of functions used to help update old projects to the latest version of Terrain Engine 2D
    /// </summary>
    public static class ProjectEditor
    {
        [MenuItem("File/Terrain Engine 2D/Update Scene")]
        public static void UpdateScene()
        {
            if(EditorUtility.DisplayDialog("Terrain Engine 2D", "The Terrain Engine 2D GameObjects in your scene will be updated, this will remove any extra Components or child GameObjects you have added. In order to prevent data loss please backup your Scene before continuing.", "Continue", "Cancel"))
            {
                EditorUtility.DisplayProgressBar("Terrain Engine 2D", "Finding object prefabs", 0);
                GameObject worldPrefab = GetPrefab("World");
                GameObject worldCameraPrefab = GetPrefab("WorldCamera");
                //GameObject gridSelectorPrefab = GetPrefab("GridSelector");
                //GameObject OSDPrefab = GetPrefab("OSD");

                EditorUtility.DisplayProgressBar("Terrain Engine 2D", "Finding objects in scene", 0.2f);
                List<Object> ObjectsToDestroy = new List<Object>();

                Object[] worldObjects = Object.FindObjectsOfType(typeof(World));
                GameObject worldObject;
                if (worldObjects.Length == 0)
                    worldObject = CreateWorldObject();
                else
                {
                    worldObject = ((World)worldObjects[0]).gameObject;
                    for (int i = 1; i < worldObjects.Length; i++)
                        ObjectsToDestroy.Add(worldObjects[i]);
                }
                Object[] worldCameraObjects = Object.FindObjectsOfType(typeof(CameraController));
                GameObject worldCameraObject;
                if (worldCameraObjects.Length == 0)
                    worldCameraObject = CreateCameraObject();
                else
                {
                    worldCameraObject = ((CameraController)worldCameraObjects[0]).gameObject;
                    for (int i = 1; i < worldCameraObjects.Length; i++)
                        ObjectsToDestroy.Add(worldCameraObjects[i]);
                }

                Object[] gridSelectorObjects = Object.FindObjectsOfType(typeof(GridSelectorImageSetter));
                GameObject gridSelectorObject;
                if (gridSelectorObjects.Length == 0)
                    gridSelectorObject = CreateGridSelectorObject();
                else
                {
                    gridSelectorObject = ((GridSelectorImageSetter)gridSelectorObjects[0]).gameObject;
                    for (int i = 1; i < gridSelectorObjects.Length; i++)
                        ObjectsToDestroy.Add(gridSelectorObjects[i]);
                }

                Object[] OSDObjects = Object.FindObjectsOfType(typeof(OSDController));
                GameObject OSDObject;
                if (OSDObjects.Length == 0)
                    OSDObject = CreateOSDObject();
                else
                {
                    OSDObject = ((OSDController)OSDObjects[0]).gameObject;
                    for (int i = 1; i < OSDObjects.Length; i++)
                        ObjectsToDestroy.Add(OSDObjects[i]);
                }

                if (ObjectsToDestroy.Count > 0)
                {
                    EditorUtility.DisplayProgressBar("Terrain Engine 2D", "Destroying Duplicates", 0.4f);
                    foreach (Object tempObject in ObjectsToDestroy)
                        Undo.DestroyObjectImmediate(tempObject);
                }

                EditorUtility.DisplayProgressBar("Terrain Engine 2D", "Updating World", 0.5f);
                Undo.RegisterCompleteObjectUndo(worldObject, "Updating World Components and Children");
                UpdateComponents(worldObject, worldPrefab, true);

                EditorUtility.DisplayProgressBar("Terrain Engine 2D", "Updating World Camera", 0.7f);
                Undo.RegisterCompleteObjectUndo(worldCameraObject, "Updating World Camera Components and Children");
                UpdateComponents(worldCameraObject, worldCameraPrefab, true);

                EditorUtility.DisplayProgressBar("Terrain Engine 2D", "Updating Grid Selector", 0.8f);
                //Undo.RegisterCompleteObjectUndo(gridSelectorObject, "Updating Grid Selector Components and Children");
                Undo.DestroyObjectImmediate(gridSelectorObject);
                CreateGridSelectorObject();

                EditorUtility.DisplayProgressBar("Terrain Engine 2D", "Updating OSD", 0.9f);
                //Undo.RegisterCompleteObjectUndo(OSDObject, "Updating OSD Components and Children");
                //UpdateComponents(OSDObject, OSDPrefab, true);
                Undo.DestroyObjectImmediate(OSDObject);
                CreateOSDObject();

                EditorUtility.DisplayProgressBar("Terrain Engine 2D", "Finishing up", 1);
                EditorUtility.ClearProgressBar();
                Debug.Log("The scene has been updated successfully");
                EditorUtility.DisplayDialog("Terrain Engine 2D", "The update has completed successfully, to preserve data, some components were not removed and newly added fields will have to be manually populated, refer to the prefabs for values.", "Okay");
            }
        }

        private static void UpdateComponents(GameObject gameObject, GameObject prefab, bool includeChildren = false)
        {
            //Remove any old components that may be missing or deprecated
            Component[] components = gameObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    Object.DestroyImmediate(component);
                    continue;
                }
                    
                if (!prefab.GetComponent(component.GetType()))
                    Object.DestroyImmediate(component);
            }
            //Ensure the GameObject has all the same components as the prefab, if not add those components
            Component[] prefabComponents = prefab.GetComponents<Component>();
            foreach(Component component in prefabComponents)
            {
                if (!gameObject.GetComponent(component.GetType()))
                {
                    ComponentUtility.CopyComponent(component);
                    ComponentUtility.PasteComponentAsNew(gameObject);
                }
            }

            if (includeChildren)
            {
                //Remove other children of the object that may be deprecated 
                foreach (Transform child in gameObject.transform)
                {
                    if (!prefab.transform.Find(child.name))
                        Undo.DestroyObjectImmediate(child.gameObject);
                }
                //Ensure the GameObject has all the same children as the prefab, if not add a copy of those children
                foreach (Transform prefabChild in prefab.transform)
                {
                    Transform child = gameObject.transform.Find(prefabChild.name);
                    if (child == null)
                    {
                        GameObject newChild = Object.Instantiate(prefabChild.gameObject, gameObject.transform);
                        Undo.RegisterCreatedObjectUndo(newChild, "Created new child object");
                        newChild.name = prefabChild.name;
                    }
                    else
                        UpdateComponents(child.gameObject, prefabChild.gameObject, true);
                }
            }
        }

        [MenuItem("GameObject/Terrain Engine 2D/Create All", priority = 10)]
        public static void CreateAllObjects()
        {
            CreateWorldObject();
            CreateCameraObject();
            CreateGridSelectorObject();
            CreateOSDObject();
        }

        [MenuItem("GameObject/Terrain Engine 2D/World", priority = 11)]
        public static GameObject CreateWorldObject()
        {
            GameObject worldPrefab = GetPrefab("World");
            GameObject gameObject = Object.Instantiate(worldPrefab);
            Undo.RegisterCreatedObjectUndo(gameObject, "World Object Created");
            gameObject.name = worldPrefab.name;
            return gameObject;
        }

        [MenuItem("GameObject/Terrain Engine 2D/World Camera", priority = 11)]
        public static GameObject CreateCameraObject()
        {
            GameObject worldCameraPrefab = GetPrefab("WorldCamera");
            GameObject gameObject = Object.Instantiate(worldCameraPrefab);
            Undo.RegisterCreatedObjectUndo(gameObject, "Camera Object Created");
            gameObject.name = worldCameraPrefab.name;
            return gameObject;
        }

        [MenuItem("GameObject/Terrain Engine 2D/Grid Selector", priority = 11)]
        public static GameObject CreateGridSelectorObject()
        {
            GameObject gridSelectorPrefab = GetPrefab("GridSelector");
            GameObject gameObject = Object.Instantiate(gridSelectorPrefab);
            Undo.RegisterCreatedObjectUndo(gameObject, "Grid Selector Object Created");
            gameObject.name = gridSelectorPrefab.name;
            return gameObject;
        }

        [MenuItem("GameObject/Terrain Engine 2D/OSD", priority = 11)]
        public static GameObject CreateOSDObject()
        {
            GameObject OSDPrefab = GetPrefab("OSD");
            GameObject gameObject = Object.Instantiate(OSDPrefab);
            Undo.RegisterCreatedObjectUndo(gameObject, "OSD Object Created");
            gameObject.name = OSDPrefab.name;
            return gameObject;
        }

        private static GameObject GetPrefab(string name)
        {
            string[] allPaths = Directory.GetDirectories(Application.dataPath, "TerrainEngine2D", SearchOption.AllDirectories);
            for (int i = 0; i < allPaths.Length; i++)
            {
                allPaths[i] = allPaths[i].Replace("\\", "/");
                allPaths[i] = allPaths[i].Remove(0, Application.dataPath.Length);
                allPaths[i] = allPaths[i].Insert(0, "Assets");
            }

            string[] guids = AssetDatabase.FindAssets(name + " t:prefab", allPaths);

            GameObject prefab = null;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject asset = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                if (asset != null && asset.name == name)
                {
                    prefab = asset;
                    break;
                }
            }
            if (prefab == null)
                throw new System.Exception("The Prefab: " + name + " could not be found! Ensure you have not renamed the TerrainEngine2D folder.");

            return prefab;
        }
    }
}