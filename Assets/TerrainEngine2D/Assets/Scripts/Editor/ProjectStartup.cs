using UnityEngine;
using UnityEditor;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.Editor
{
    /// <summary>
    /// Tasks that run on startup
    /// </summary>
    [InitializeOnLoad]
    public class ProjectStartup
    {
        static ProjectStartup()
        {
            // Ensure every world data object has a unique ID
            string[] guids = AssetDatabase.FindAssets("t:WorldData");
            foreach (string guid in guids)
            {
                WorldData worldData = AssetDatabase.LoadAssetAtPath<WorldData>(AssetDatabase.GUIDToAssetPath(guid));
                if (string.IsNullOrEmpty(worldData.GUID))
                    worldData.GUID = guid;
            }
        }
    }
}

