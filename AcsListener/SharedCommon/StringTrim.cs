using System;

namespace SharedCommon
{
    /// <summary>Helper extension class for String manipulation</summary>
    public static class StringTrim
    {
        /// <summary>Extension method that trims the last character of an input string.</summary>
        /// <param name="stringToBeTrimmed">The string to be trimmed.</param>
        /// <returns>String that has been trimmed of the last character.</returns>
        public static string TrimLastCharacter(this String stringToBeTrimmed)
        {
            if (String.IsNullOrEmpty(stringToBeTrimmed))
            {
                return stringToBeTrimmed;
            }
            else
            {
                return stringToBeTrimmed.TrimEnd(stringToBeTrimmed[stringToBeTrimmed.Length - 1]);
            }
        }
    }
}
