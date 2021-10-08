using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

namespace TerrainEngine2D
{
    /// <summary>
    /// Ensures the input of an Input Field is a valid Fluid Density
    /// </summary>
    [CreateAssetMenu(fileName = "FluidDensityInputValidator", menuName = "Terrain Engine 2D/TextMeshPro/Fluid Density Input Validator")]
    public class FluidDensityInputValidator : TMP_InputValidator
    {
        /// <summary>
        /// Determines whether the current input char in an Input Field is valid
        /// </summary>
        /// <param name="text">The current text of the input field</param>
        /// <param name="pos">The current text position in the input field</param>
        /// <param name="ch">The character to be added to the string</param>
        /// <returns>Returns the character that has been added to the string, or null if invalid</returns>
        public override char Validate(ref string text, ref int pos, char ch)
        {
            //Ensure char is a digit between 0 and 9
            if (ch >= '0' && ch <= '9')
            {
                //Add the char to the string
                string newText = text;
                if (pos == text.Length)
                    newText += ch;
                else
                    newText = newText.Insert(pos, ch.ToString());
                //See if the new text will parse to a byte
                byte result;
                var digitPattern = @"\d|\d\d|\d\d\d";
                if (Regex.IsMatch(newText, digitPattern) && byte.TryParse(newText, out result))
                {
                    //If a valid byte is parsed, then set the new string
                    text = newText;
                    pos++;
                    return ch;
                }
            }
            return (char)0x00;
        }

    }
}