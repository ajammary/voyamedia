using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Text;

namespace MediaManagement
{
    /// <summary>
    /// </summary>
    public class MediaTypeValidation
    {
        /// <summary>
        /// Takes a string List of filenames, 
        /// and returns a new string List containing only the valid video directories.
        /// </summary>
        /// <param name="fileList">A string List of filenames to validate.</param>
        /// <returns>A string List of valid video directories.</returns>
        public static List<string> GetValidVideoDirectoriesFromFileList(List<string> fileList)
        {
            // Main variables.
            List<string> validVideoDirectories = new List<string>() { };

            // Get valid video files from the files list.
            foreach (string file in fileList)
            {
                FileInfo fileInfo = new FileInfo(file);

                if (MediaTypeValidation.IsVideo(file) && !validVideoDirectories.Contains(fileInfo.DirectoryName))
                {
                    validVideoDirectories.Add(fileInfo.DirectoryName);
                }
            }


            // Sort the list alphabetically.
            validVideoDirectories.Sort();

            // Return a string List of the valid video files.
            return validVideoDirectories;
        }

        /// <summary>
        /// Takes a string List of filenames, 
        /// and returns a new string List containing only the valid video filenames.
        /// </summary>
        /// <param name="fileList">A string List of filenames to validate.</param>
        /// <returns>A string List of valid video files.</returns>
        public static List<string> GetValidVideosFromFileList(List<string> fileList)
        {
            // Main variables.
            List<string> validVideoFiles = new List<string>() { };

            // Get valid video files from the files list.
            foreach (string file in fileList)
            {
                if (MediaTypeValidation.IsVideo(file))
                {
                    validVideoFiles.Add(file);
                }
            }

            // Return a string List of the valid video files.
            return validVideoFiles;
        }

        /// <summary>
        /// Tells you if the file is a valid audio file type.
        /// </summary>
        /// <param name="fileNameAndPath">The path and filename of the file to validate.</param>
        /// <example>IsAudio("c:\path\file.mp3") returns True, IsAudio("c:\path\file.jpg") returns False.</example>
        /// <returns>A Boolean: True or False.</returns>
        public static bool IsAudio(string fileNameAndPath)
        {
            // Make a list of valid audio file extensions.
            List<string> audioExtensions = new List<string>() { @".aac", @".aif", @".iff", @".m4a", @".mid", @".mp3", @".mpa", @".wav", @".wma" };

            // Get the details and properties of the file.
            FileInfo fileInfo = null;

            // Check if the fully qualified file name is less than MAX_CHARS of 260 characters.
            if (fileNameAndPath.Length < 260)
            {
                fileInfo = new FileInfo(fileNameAndPath);
            }

            // Validate the file extension.
            if (fileInfo != null)
            {
                if (audioExtensions.Contains(fileInfo.Extension.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tells you if the file is a valid picture file type.
        /// </summary>
        /// <param name="fileNameAndPath">The path and filename of the file to validate.</param>
        /// <example>IsPicture("c:\path\file.jpg") returns True, IsPicture("c:\path\file.mp3") returns False.</example>
        /// <returns>A Boolean: True or False.</returns>
        public static bool IsPicture(string fileNameAndPath)
        {
            // Make a list of valid picture file extensions.
            List<string> pictureExtensions = new List<string>() { @".bmp", @".gif", @".jpg", @".png", @".psd", @".tif" };

            // Get the details and properties of the file.
            FileInfo fileInfo = null;

            // Check if the fully qualified file name is less than MAX_CHARS of 260 characters.
            if (fileNameAndPath.Length < 260)
            {
                fileInfo = new FileInfo(fileNameAndPath);
            }

            // Validate the file extension.
            if (fileInfo != null)
            {
                if (pictureExtensions.Contains(fileInfo.Extension.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tells you if the file is a valid video file type.
        /// </summary>
        /// <param name="fileNameAndPath">The path and filename of the file to validate.</param>
        /// <example>IsVideo("c:\path\file.mpg") returns True, IsVideo("c:\path\file.jpg") returns False.</example>
        /// <returns>A Boolean: True or False.</returns>
        public static bool IsVideo(string fileNameAndPath)
        {
            // Make a list of valid video file extensions.
            List<string> videoExtensions = new List<string>() { @".3g2", @".3gp", @".asf", @".asx", @".avi", @".flv", @".m2ts", @".m4v", @".mkv", @".mov", @".mp4", @".mpg", @".swf", @".ts", @".vob", @".wmv" };

            // Get the details and properties of the file.
            FileInfo fileInfo = null;

            // Check if the fully qualified file name is less than MAX_CHARS of 260 characters.
            if (fileNameAndPath.Length < 260)
            {
                fileInfo = new FileInfo(fileNameAndPath);
            }

            // Validate the file extension.
            if (fileInfo != null)
            {
                if (videoExtensions.Contains(fileInfo.Extension.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

    }

}
