using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaInfoApi
{
    public static class MediaInfo
    {
        // Private class member properties.
        private static ResourceManager resourceManager = new ResourceManager("MediaInfoApi.Properties.Resources", Assembly.GetExecutingAssembly());

        // MediaType structure.
        public struct MediaType
        {
            public const string Audio   = "audio";
            public const string Picture = "pictures";
            public const string Video   = "videos";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static string RemoveHtmlTags(string inputString)
        {
            // Remove html tags.
            inputString = inputString.Replace(@"<br>",   "");
            inputString = inputString.Replace(@"<br />", "");
            inputString = inputString.Replace(@"<b>",    "");
            inputString = inputString.Replace(@"</b>",   "");
            inputString = inputString.Replace(@"<i>",    "");
            inputString = inputString.Replace(@"</i>",   "");
            inputString = inputString.Replace('+',      ' ');

            // Remove <a href='URL'> tags.
            string          aHrefPattern = @"<a href=('|"")\w+://(\w|\d|\.|-|_|\s|%|/)+('|"")\s*>";
            MatchCollection aHrefMatches = Regex.Matches(inputString, aHrefPattern);

            foreach (Match aHrefMatch in aHrefMatches)
            {
                inputString = inputString.Replace(aHrefMatch.ToString(), "");
            }

            // Remove closing </a> tags.
            inputString = inputString.Replace(@"</a>", "");
            
            // Return the trimmed string.
            return inputString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static string RemoveSpecialCharacters(string inputString)
        {
            // Remove special characters.
            inputString = inputString.Replace('.', ' ');
            inputString = inputString.Replace('_', ' ');
            inputString = inputString.Replace('-', ' ');
            inputString = inputString.Replace('+', ' ');
            inputString = inputString.Replace('!', ' ');
            inputString = inputString.Replace('"', ' ');
            inputString = inputString.Replace('\'', ' ');
            inputString = inputString.Replace('$', ' ');
            inputString = inputString.Replace('%', ' ');
            inputString = inputString.Replace("(", "");
            inputString = inputString.Replace(")", "");
            inputString = inputString.Replace("[", "");
            inputString = inputString.Replace("]", "");
            inputString = inputString.Replace("{", "");
            inputString = inputString.Replace("}", "");

            // Remove the trimmed string.
            return inputString;
        }
    }
}
