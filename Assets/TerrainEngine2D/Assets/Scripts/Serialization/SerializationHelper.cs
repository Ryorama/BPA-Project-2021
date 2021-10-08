using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Class of helpful functions for serialization
    /// </summary>
    public static class SerializationHelper
    {
        /// <summary>
        /// Converts a jagged byte array to a single byte array
        /// </summary>
        /// <param name="jaggedArray">The jagged array to convert</param>
        /// <returns>Returns the array of bytes</returns>
        public static byte[] ConvertJaggedByteArrayToArray(byte[][,] jaggedArray)
        {
            int lengthD1 = jaggedArray.GetLength(0);
            int lengthD2 = jaggedArray[0].GetLength(0);
            int lengthD3 = jaggedArray[0].GetLength(1);
            byte[] byteArray = new byte[lengthD1 * lengthD2 * lengthD3];
            for (int i = 0; i < lengthD1; i++)
            {
                for (int j = 0; j < lengthD2; j++)
                {
                    for (int k = 0; k < lengthD3; k++)
                    {
                        byteArray[i * lengthD2 * lengthD3 + j * lengthD3 + k] = jaggedArray[i][j, k];
                    }
                }
            }
            return byteArray;
        }

        /// <summary>
        /// Converts a single byte array to a jagged array
        /// </summary>
        /// <param name="byteArray">The byte array to convert</param>
        /// <param name="lengthD1">Length of dimension 1</param>
        /// <param name="lengthD2">Length of dimension 2</param>
        /// <param name="lengthD3">Length of dimension 3</param>
        /// <returns>Returns the jagged array</returns>
        public static byte[][,] ConvertByteArrayToJaggedArray(byte[] byteArray, int lengthD1, int lengthD2, int lengthD3)
        {
            byte[][,] jaggedArray = new byte[lengthD1][,];
            for (int i = 0; i < lengthD1; i++)
            {
                jaggedArray[i] = new byte[lengthD2, lengthD3];
                for (int j = 0; j < lengthD2; j++)
                {
                    for (int k = 0; k < lengthD3; k++)
                    {
                        jaggedArray[i][j, k] = byteArray[i * lengthD2 * lengthD3 + j * lengthD3 + k];
                    }
                }
            }
            return jaggedArray;
        }

        /// <summary>
        /// Converts a jagged bool array to a single byte array
        /// </summary>
        /// <param name="jaggedArray">The jagged array to convert</param>
        /// <returns>Returns the array of bytes</returns>
        public static byte[] ConvertJaggedBoolArrayToByteArray(bool[][,] jaggedArray)
        {
            int lengthD1 = jaggedArray.GetLength(0);
            int lengthD2 = jaggedArray[0].GetLength(0);
            int lengthD3 = jaggedArray[0].GetLength(1);
            byte[] byteArray = new byte[lengthD1 * lengthD2 * lengthD3];
            for (int i = 0; i < lengthD1; i++)
            {
                for (int j = 0; j < lengthD2; j++)
                {
                    for (int k = 0; k < lengthD3; k++)
                    {
                        if (jaggedArray[i][j, k])
                            byteArray[i * lengthD2 * lengthD3 + j * lengthD3 + k] = 1;
                        else
                            byteArray[i * lengthD2 * lengthD3 + j * lengthD3 + k] = 0;
                    }
                }
            }
            return byteArray;
        }

        /// <summary>
        /// Converts a single byte array to a jagged array
        /// </summary>
        /// <param name="byteArray">The byte array to convert</param>
        /// <param name="lengthD1">Length of dimension 1</param>
        /// <param name="lengthD2">Length of dimension 2</param>
        /// <param name="lengthD3">Length of dimension 3</param>
        /// <returns>Returns the jagged array</returns>
        public static bool[][,] ConvertByteArrayToJaggedBoolArray(byte[] byteArray, int lengthD1, int lengthD2, int lengthD3)
        {
            bool[][,] jaggedArray = new bool[lengthD1][,];
            for (int i = 0; i < lengthD1; i++)
            {
                jaggedArray[i] = new bool[lengthD2, lengthD3];
                for (int j = 0; j < lengthD2; j++)
                {
                    for (int k = 0; k < lengthD3; k++)
                    {
                        if (byteArray[i * lengthD2 * lengthD3 + j * lengthD3 + k] == 1)
                            jaggedArray[i][j, k] = true;
                        else
                            jaggedArray[i][j, k] = false;
                    }
                }
            }
            return jaggedArray;
        }
    }
}