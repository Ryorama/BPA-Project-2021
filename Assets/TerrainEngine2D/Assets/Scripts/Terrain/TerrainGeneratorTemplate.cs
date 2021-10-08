using UnityEngine;
using TerrainEngine2D;

// Copyright (C) 2020 Matthew Wilson

//NOTE:  Terrain Generator scripts should only be used for procedural generation. None of the TerrainGenerator 
//       base functions should be called outside the GenerateData function.
public class TerrainGeneratorTemplate : TerrainGenerator
{
    //Layer types
    private enum Layers {  /*Place Layer types in here (optional) */ }
    //Block types
    //-----Place Layer Block/Fluid Enums here (optional)-----
    
    //----------

    /// <summary>
    /// Procedurally generates world block data using random and pseudo-random functions
    /// </summary>
    /// <param name="world">Reference to the world to access block arrays</param>
    public override void GenerateData()
    {
        base.GenerateData();

        //Pass
        for (int x = 0; x < world.WorldWidth; x++)
        {
            //-----Add height variables here-----

            //----------
            for (int y = 0; y < world.WorldHeight; y++)
            {
                //-----Set block data here-----
                
                    
                //----------
            }
        }
    }
}
