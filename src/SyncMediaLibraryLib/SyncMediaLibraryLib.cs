using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using Database;
using FileSystem;
using MediaInfoApi;
using MediaInfoLib;
using MediaManagement;

namespace SyncMediaLibraryLib
{
    /// <summary>
    /// 
    /// </summary>
    public static class SyncAction
    {
        // Define Private Members.
        private static ResourceManager resourceManager = new ResourceManager("SyncMediaLibraryLib.Properties.Resources", Assembly.GetExecutingAssembly());
        private static string          syncJobId;

        // Define Public Members.
        public static string SyncJobId
        {
            get { return SyncAction.syncJobId; }
            set { SyncAction.syncJobId = value; }
        }

        // SortDirection structure.
        public struct SortDirection
        {
            public const string Ascending  = "ASC";
            public const string Descending = "DESC";
        }

        /// <summary>
        /// 
        /// </summary>
        public static void AddAudio(string fileNameAndPath)
        {
            // Main variables.
            MediaInfoDll mediaInfo = new MediaInfoDll();

            // Un-escape double back-slashes ("\\" -> "\").
            fileNameAndPath = fileNameAndPath.Replace(@"\\", @"\");

            try
            {
                // Get file details about the selected media file.
                FileInfo fileInfo = new FileInfo(fileNameAndPath);

                //SyncAction.SetSyncStatusAction(@"Inserting Audio: """ + fileInfo.Name.Replace(fileInfo.Extension, "") + @""" ... ", fileInfo.DirectoryName);

                try
                {
                    // Get media details about the selected media file.
                    mediaInfo.Open(fileNameAndPath);
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                        Class:  SyncMediaLibraryLib
                        Method: AddAudio(""" + fileNameAndPath + @""")
                        Action: mediaInfo.Open(""" + fileNameAndPath + @""")
                        
                        " + exception.Message
                        , EventLogEntryType.Error, 123);
                }

                // Create a new SQL object.
                SQL sql = new SQL();

                // Set the selected table.
                sql.TableName = "audio";

                // Select values of the columns to update.
                if (mediaInfo != null)
                {
                    /*
                    sql.SqlCommandParameters.Add("bit_rate", mediaInfo.Get(StreamKind.Audio, 0, @"BitRate/String"));
                    */

                    string audio         = "";
                    string audioBitRate  = mediaInfo.Get(MediaInfoLib.StreamKind.Audio, 0, @"BitRate/String").Replace(" ", "");
                    string audioChannels = mediaInfo.Get(MediaInfoLib.StreamKind.Audio, 0, @"Channel(s)");
                    string audioCodec    = mediaInfo.Get(MediaInfoLib.StreamKind.Audio, 0, @"Format");

                    if (audioBitRate.Contains(@"/"))
                    {
                        audioBitRate = audioBitRate.Substring(0, audioBitRate.IndexOf(@"/"));
                    }

                    switch (audioChannels)
                    {
                        case "1":
                            audioChannels = @"1.0";
                            break;
                        case "2":
                            audioChannels = @"2.0";
                            break;
                        case "3":
                            audioChannels = @"2.1";
                            break;
                        case "6":
                            audioChannels = @"5.1";
                            break;
                        default:
                            break;
                    }

                    if (audioCodec.Contains("AC-3"))
                    {
                        audio = @"DD " + audioChannels + @" " + audioBitRate;
                    }
                    else if (audioCodec.Contains("DTS"))
                    {
                        audio = @"DTS " + audioChannels + @" " + audioBitRate;
                    }
                    else if (audioCodec.Contains("AAC"))
                    {
                        audio = @"AAC " + audioChannels + @" " + audioBitRate;
                    }
                    else if (audioCodec.Contains("MPEG"))
                    {
                        audio = @"MP3 " + audioChannels + @" " + audioBitRate;
                    }
                    else if (audioCodec.Contains("WMA"))
                    {
                        audio = @"WMA " + audioChannels + @" " + audioBitRate;
                    }
                    else
                    {
                        audio = audioCodec + @" " + audioChannels;
                    }

                    sql.SqlCommandParameters.Add("audio",        audio);
                    sql.SqlCommandParameters.Add("album",        mediaInfo.Get(StreamKind.General, 0, @"Album"));
                    sql.SqlCommandParameters.Add("artist",       mediaInfo.Get(StreamKind.General, 0, @"Artist"));
                    sql.SqlCommandParameters.Add("cover",        mediaInfo.Get(StreamKind.General, 0, @"Cover_Data"));
                    sql.SqlCommandParameters.Add("duration",     mediaInfo.Get(StreamKind.Audio,   0, @"Duration/String"));
                    sql.SqlCommandParameters.Add("file_size",    mediaInfo.Get(StreamKind.General, 0, @"FileSize/String"));
                    sql.SqlCommandParameters.Add("genre",        mediaInfo.Get(StreamKind.General, 0, @"Genre"));
                    sql.SqlCommandParameters.Add("release_year", mediaInfo.Get(StreamKind.General, 0, @"Recorded_Date"));
                    sql.SqlCommandParameters.Add("track",        mediaInfo.Get(StreamKind.General, 0, @"Track"));
                }

                sql.SqlCommandParameters.Add("unique_id", Guid.NewGuid().ToString());
                sql.SqlCommandParameters.Add("filename",  fileInfo.Name.Replace(fileInfo.Extension, ""));

                if (fileInfo.DirectoryName.LastIndexOf('\\') == (fileInfo.DirectoryName.Length - 1))
                {
                    sql.SqlCommandParameters.Add("path", fileInfo.DirectoryName.Substring(0, (fileInfo.DirectoryName.Length - 1)));
                }
                else
                {
                    sql.SqlCommandParameters.Add("path", fileInfo.DirectoryName);
                }

                sql.SqlCommandParameters.Add("file_type", fileInfo.Extension.Substring(1).ToUpper());
                sql.SqlCommandParameters.Add("tag", "");

                // Try to write the data to the database table.
                sql.InsertData();

                //SyncAction.SetSyncStatusAction(@"System is currently being scanned for valid Audio, Pictures and Videos ... ", fileInfo.DirectoryName);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: AddAudio(""" + fileNameAndPath + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            finally
            {
                // Dispose/Close the mediaInfo object.
                mediaInfo.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void AddPicture(string fileNameAndPath)
        {
            // Main variables.
            MediaInfoDll mediaInfo = new MediaInfoDll();

            // Un-escape double back-slashes ("\\" -> "\").
            fileNameAndPath = fileNameAndPath.Replace(@"\\", @"\");

            try
            {
                // Get file details about the selected media file.
                FileInfo fileInfo = new FileInfo(fileNameAndPath);

                //SyncAction.SetSyncStatusAction(@"Inserting Picture: """ + fileInfo.Name.Replace(fileInfo.Extension, "") + @""" ... ", fileInfo.DirectoryName);

                try
                {
                    // Get media details about the selected media file.
                    mediaInfo.Open(fileNameAndPath);
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                        Class:  SyncMediaLibraryLib
                        Method: AddPicture(""" + fileNameAndPath + @""")
                        Action: mediaInfo.Open(""" + fileNameAndPath + @""")
                        
                        " + exception.Message
                        , EventLogEntryType.Error, 123);
                }

                // Create a new SQL object.
                SQL sql = new SQL();

                // Set the selected table.
                sql.TableName = "pictures";

                // Select values of the columns to update.
                if (mediaInfo != null)
                {
                    if (!string.IsNullOrEmpty(mediaInfo.Get(StreamKind.Video, 0, @"Width")) && !String.IsNullOrEmpty(mediaInfo.Get(StreamKind.Video, 0, @"Height/String")))
                    {
                        sql.SqlCommandParameters.Add("dimensions", mediaInfo.Get(StreamKind.Image, 0, @"Width") + @" x " + mediaInfo.Get(StreamKind.Image, 0, @"Height/String"));
                    }

                    sql.SqlCommandParameters.Add("file_size", mediaInfo.Get(StreamKind.General, 0, @"FileSize/String"));
                }

                sql.SqlCommandParameters.Add("unique_id",  Guid.NewGuid().ToString());
                sql.SqlCommandParameters.Add("filename",   fileInfo.Name.Replace(fileInfo.Extension, ""));

                if (fileInfo.DirectoryName.LastIndexOf('\\') == (fileInfo.DirectoryName.Length - 1))
                {
                    sql.SqlCommandParameters.Add("path", fileInfo.DirectoryName.Substring(0, (fileInfo.DirectoryName.Length - 1)));
                }
                else
                {
                    sql.SqlCommandParameters.Add("path", fileInfo.DirectoryName);
                }

                sql.SqlCommandParameters.Add("file_type", fileInfo.Extension.Substring(1).ToUpper());
                sql.SqlCommandParameters.Add("comment",    "");
                sql.SqlCommandParameters.Add("tag",        "");

                // Try to write the data to the database table.
                sql.InsertData();

                //SyncAction.SetSyncStatusAction(@"System is currently being scanned for valid Audio, Pictures and Videos ... ", fileInfo.DirectoryName);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: AddPicture(""" + fileNameAndPath + @""")
                                        
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            finally
            {
                // Dispose/Close the mediaInfo object.
                mediaInfo.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void AddVideo(string fileNameAndPath)
        {
            try
            {
                // Get file details about the selected media file.
                FileInfo fileInfo = new FileInfo(fileNameAndPath);

                // Un-escape double back-slashes ("\\" -> "\").
                fileNameAndPath = fileNameAndPath.Replace(@"\\", @"\");
                
                //SyncAction.SetSyncStatusAction(@"Inserting Video: """ + fileInfo.Name.Replace(fileInfo.Extension, "") + @""" ... ", fileInfo.DirectoryName);
                
                // Create a new SQL object.
                SQL sql = new SQL();

                // Set the selected table.
                sql.TableName = "videos";
                
                // Add the media file details to the SQL command parameter values.
                sql.SqlCommandParameters.Add("file_type", fileInfo.Extension.Substring(1).ToUpper());
                sql.SqlCommandParameters.Add("filename",  fileInfo.Name.Replace(fileInfo.Extension, ""));
                sql.SqlCommandParameters.Add("tag",       "");
                sql.SqlCommandParameters.Add("unique_id", Guid.NewGuid().ToString());

                // Add the media directory path details to the SQL command parameter values.
                if (fileInfo.DirectoryName.LastIndexOf('\\') == (fileInfo.DirectoryName.Length - 1))
                {
                    sql.SqlCommandParameters.Add("path", fileInfo.DirectoryName.Substring(0, (fileInfo.DirectoryName.Length - 1)));
                }
                else
                {
                    sql.SqlCommandParameters.Add("path", fileInfo.DirectoryName);
                }
                                
                // Try to write the data to the database table.
                sql.InsertData();
                
                //SyncAction.SetSyncStatusAction(@"System is currently being scanned for valid Audio, Pictures and Videos ... ", fileInfo.DirectoryName);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"), 
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: AddVideo(""" + fileNameAndPath + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeyId"></param>
        private static void DeleteMedia(string tableName, string primaryKeyId)
        {
            try
            {
                // Create a new SQL object.
                SQL sql;

                if ((tableName == @"sync_status") || (tableName == @"disabled_movie_details"))
                {
                    sql = new SQL(Database.SQL.DatabaseType.MediaOrganizer);
                }
                else
                {
                    sql = new SQL();
                }

                // Set the selected table.
                sql.TableName = tableName;

                // Try to write the data to the database table.
                sql.DeleteData(primaryKeyId);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: DeleteMedia(""" + tableName + @""", """ + primaryKeyId + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryKeyId"></param>
        public static void DeleteAudio(string primaryKeyId)
        {
            SyncAction.DeleteMedia(MediaInfo.MediaType.Audio, primaryKeyId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryKeyId"></param>
        public static void DeletePicture(string primaryKeyId)
        {
            SyncAction.DeleteMedia(MediaInfo.MediaType.Picture, primaryKeyId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryKeyId"></param>
        public static void DeleteVideo(string primaryKeyId)
        {
            SyncAction.DeleteMedia(MediaInfo.MediaType.Video, primaryKeyId);
        }

        /// <summary>
        /// Clears the movie and tv show details for the specified media file.
        /// </summary>
        /// <param name="mediaType">audio, pictures or vidoes</param>
        /// <param name="primaryKeyId">Primary Key ID of the media file.</param>
        /// <param name="path">Path of the media files.</param>
        private static void ClearMovieDetails(string mediaType, string primaryKeyId, string path = "")
        {
            try
            {
                // Create a new SQL object.
                SQL sql = new SQL();

                // Set the selected table.
                sql.TableName = mediaType;

                // Specify the new values.
                sql.SqlCommandParameters.Add("movie_details_id",             "");
                sql.SqlCommandParameters.Add("movie_details_backdrop",       "");
                sql.SqlCommandParameters.Add("movie_details_cast",           "");
                sql.SqlCommandParameters.Add("movie_details_cover",          "");
                sql.SqlCommandParameters.Add("movie_details_director",       "");
                sql.SqlCommandParameters.Add("movie_details_genre",          "");
                sql.SqlCommandParameters.Add("movie_details_name",           "");
                sql.SqlCommandParameters.Add("movie_details_released",       "");
                sql.SqlCommandParameters.Add("movie_details_summary",        "");
                sql.SqlCommandParameters.Add("movie_details_trailer",        "");
                sql.SqlCommandParameters.Add("movie_details_writers",        "");
                sql.SqlCommandParameters.Add("episode_details_airdate",      "");
                sql.SqlCommandParameters.Add("episode_details_cast",         "");
                sql.SqlCommandParameters.Add("episode_details_cover",        "");
                sql.SqlCommandParameters.Add("episode_details_director",     "");
                sql.SqlCommandParameters.Add("episode_details_genre",        "");
                sql.SqlCommandParameters.Add("episode_details_name",         "");
                sql.SqlCommandParameters.Add("episode_details_next_airdate", "");
                sql.SqlCommandParameters.Add("episode_details_next_number",  "");
                sql.SqlCommandParameters.Add("episode_details_next_title",   "");
                sql.SqlCommandParameters.Add("episode_details_number",       "");
                sql.SqlCommandParameters.Add("episode_details_summary",      "");
                sql.SqlCommandParameters.Add("episode_details_title",        "");
                sql.SqlCommandParameters.Add("episode_details_writers",      "");

                // Select which table records to update.
                if (String.IsNullOrEmpty(path))
                {
                    sql.SqlWhereParameters.Add("where_pkid", primaryKeyId);
                }
                else
                {
                    sql.SqlWhereParameters.Add("where_path", path);
                }

                // Try to write the data to the database table.
                if (String.IsNullOrEmpty(path))
                {
                    sql.UpdateData();
                }
                else
                {
                    sql.UpdateData(true);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: ClearMovieDetails(""" + mediaType + @""", """ + primaryKeyId + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }
        
        /// <summary>
        /// Clears and disables the movie and tv show details for the specified media file.
        /// </summary>
        /// <param name="mediaType">audio, pictures or vidoes</param>
        /// <param name="primaryKeyId">Primary Key ID of the media file.</param>
        /// <param name="path">Path of the media files.</param>
        private static void ClearAndDisableMovieDetails(string mediaType, string primaryKeyId, string path = "")
        {
            try
            {
                // Create a new SQL object.
                SQL sql = new SQL();

                // Set the selected table.
                sql.TableName = mediaType;

                // Specify the new values.
                sql.SqlCommandParameters.Add("movie_details_id",             "DISABLED");
                sql.SqlCommandParameters.Add("movie_details_backdrop",       "DISABLED");
                sql.SqlCommandParameters.Add("movie_details_cast",           "DISABLED");
                sql.SqlCommandParameters.Add("movie_details_cover",          "DISABLED");
                sql.SqlCommandParameters.Add("movie_details_director",       "DISABLED");
                sql.SqlCommandParameters.Add("movie_details_genre",          "DISABLED");
                sql.SqlCommandParameters.Add("movie_details_name",           "DISABLED");
                sql.SqlCommandParameters.Add("movie_details_released",       "DISABLED");
                sql.SqlCommandParameters.Add("movie_details_summary",        "DISABLED");
                sql.SqlCommandParameters.Add("movie_details_trailer",        "DISABLED");
                sql.SqlCommandParameters.Add("movie_details_writers",        "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_airdate",      "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_cast",         "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_cover",        "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_director",     "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_genre",        "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_name",         "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_next_airdate", "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_next_number",  "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_next_title",   "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_number",       "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_summary",      "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_title",        "DISABLED");
                sql.SqlCommandParameters.Add("episode_details_writers",      "DISABLED");
                
                // Select which table records to update.
                if (String.IsNullOrEmpty(path))
                {
                    sql.SqlWhereParameters.Add("where_pkid", primaryKeyId);
                }
                else
                {
                    sql.SqlWhereParameters.Add("where_path", path);
                }

                // Try to write the data to the database table.
                if (String.IsNullOrEmpty(path))
                {
                    sql.UpdateData();
                }
                else
                {
                    sql.UpdateData(true);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: ClearMovieDetails(""" + mediaType + @""", """ + primaryKeyId + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Adds the specified file or directory name to the disable movie details table.
        /// This prevents the sync job from retrieving movie details from themoviedb.org API for the specified media file, 
        /// or for all media files withing the specified directory path.
        /// </summary>
        /// <param name="path">Media directory path.</param>
        /// <param name="filename">Media filename.</param>
        public static void DisableMovieDetails(string path, string filename = "")
        {
            try
            {
                // Create a new SQL object.
                SQL sql = new SQL(SQL.DatabaseType.MediaOrganizer);

                // Set the selected table.
                sql.TableName = "disabled_movie_details";

                // Specify the new values.
                sql.SqlCommandParameters.Add("path", path);
                
                if (!String.IsNullOrEmpty(filename))
                {
                    sql.SqlCommandParameters.Add("filename", filename);
                }

                // Try to write the data to the database table.
                sql.InsertData();

                if (String.IsNullOrEmpty(filename))
                {
                    // Clear the movie details.
                    SyncAction.ClearAndDisableMovieDetails("videos", "", path);
                }
                else
                {
                    // Get the media details of the file we are disabling.
                    DataTable mediaFile = SyncAction.SelectFromDatabaseTable("videos", 0, 1, false, "", "path", path, false, "filename", filename, false, true);

                    // Clear the movie details.
                    SyncAction.ClearAndDisableMovieDetails("videos", mediaFile.Rows[0]["pkid"].ToString());
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: DisableMovieDetails(""" + path + @""", """ + filename + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Removes the specified file or directory name from the disable movie details table.
        /// This allows the sync job to retrieve movie details from themoviedb.org API for the specified media file, 
        /// or for all media files withing the specified directory path.
        /// </summary>
        /// <param name="path">Media directory path.</param>
        /// <param name="filename">Media filename.</param>
        public static void EnableMovieDetails(string path, string filename = "")
        {
            try
            {
                // Main variables.
                string tableName = "disabled_movie_details";

                if (String.IsNullOrEmpty(filename))
                {
                    // Get a list of files in the same directory path as the one we are enabling.
                    DataTable mediaFilesInPath = SyncAction.SelectFromDatabaseTable(tableName, 0, 100000, false, "", "path", path, true);

                    // Try to delete the data from the database table.
                    foreach (DataRow mediaFile in mediaFilesInPath.Rows)
                    {
                        SyncAction.DeleteMedia(tableName, mediaFile["pkid"].ToString());
                    }

                    // Clear the movie details.
                    SyncAction.ClearMovieDetails("videos", "", path);
                }
                else
                {
                    // Get the media details of the file we are enabling.
                    DataTable mediaFiles = SyncAction.SelectFromDatabaseTable(tableName, 0, 100000, false, "", "path", path, false, "filename", filename, false, true);

                    // Try to delete the data from the database table.
                    foreach (DataRow file in mediaFiles.Rows)
                    {
                        SyncAction.DeleteMedia(tableName, file["pkid"].ToString());
                    }

                    // Get the media details of the file we are enabling.
                    DataTable mediaFile = SyncAction.SelectFromDatabaseTable("videos", 0, 1, false, "", "path", path, false, "filename", filename, false, true);

                    // Clear the movie details.
                    SyncAction.ClearMovieDetails("videos", mediaFile.Rows[0]["pkid"].ToString());
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: EnableMovieDetails(""" + path + @""", """ + filename + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Renames the filename of the selected media file in the media library.
        /// </summary>
        /// <param name="mediaType">audio, pictures or vidoes</param>
        /// <param name="newFileName">New media filename.</param>
        /// <param name="primaryKeyId">Primary Key ID of the media file.</param>
        public static void RenameMediaFile(string mediaType, string newFileName, string primaryKeyId)
        {
            try
            {
                // Main variables.
                string filename = newFileName.Substring((newFileName.LastIndexOf(@"\")+1), (newFileName.LastIndexOf('.') - (newFileName.LastIndexOf(@"\")+1)));
                string fileType = newFileName.Substring(newFileName.LastIndexOf('.')+1).ToUpper();
                string path     = newFileName.Substring(0, newFileName.LastIndexOf(@"\"));

                // Create a new SQL object.
                SQL sql = new SQL();

                // Set the selected table.
                sql.TableName = mediaType;

                // Specify the new values.
                sql.SqlCommandParameters.Add("filename",  filename);
                sql.SqlCommandParameters.Add("file_type", fileType);
                sql.SqlCommandParameters.Add("path",      path);
                
                // Select which table records to update.
                sql.SqlWhereParameters.Add("where_pkid", primaryKeyId);

                // Try to write the data to the database table.
                sql.UpdateData();

                // Clear the movie details.
                SyncAction.ClearMovieDetails(mediaType, primaryKeyId);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: RenameMediaFile(""" + mediaType + @""", """ + newFileName + @""", """ + primaryKeyId + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Renames the directory name selected in the media browser.
        /// </summary>
        /// <param name="mediaType">audio, pictures or vidoes</param>
        /// <param name="oldName">Old media filename.</param>
        /// <param name="newFileName">New media filename.</param>
        public static void RenameMediaPath(string mediaType, string oldName, string newName)
        {
            try
            {
                // Get a list of all Video file entries in the Media Library.
                DataTable videoFilesInMediaLibrary = SyncAction.SelectFromDatabaseTable("videos");

                // Get details for each video file in the media library.
                foreach (DataRow tableRow in videoFilesInMediaLibrary.Rows)
                {
                    // Main variables.
                    string newPath = "";

                    // Only update table rows that contain the old path.
                    if (tableRow["path"].ToString().Contains(oldName))
                    {
                        // Build a new path by replacing the old path string with the new one.
                        newPath = tableRow["path"].ToString().Replace(oldName, newName);

                        // Initialize a new SQL object.
                        SQL sql = new SQL();

                        // Set the selected table.
                        sql.TableName = mediaType;

                        // Specify the new values.
                        sql.SqlCommandParameters.Add("path", newPath);

                        // Select which table records to update.
                        sql.SqlWhereParameters.Add("where_pkid", tableRow["pkid"].ToString());

                        // Try to write the data to the database table.
                        sql.UpdateData();
                    }
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: RenameMediaPath(""" + mediaType + @""", """ + oldName + @""", """ + newName + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Retrieves data from an SQL Database Table.
        /// </summary>
        /// <param name="tableName">Name of the Database Table.</param>
        /// <param name="offset"></param>
        /// <param name="numberOfRows"></param>
        /// <param name="uniqueResult"></param>
        /// <param name="uniqueFilterColumn"></param>
        /// <param name="whereColumn"></param>
        /// <param name="whereValue"></param>
        /// <param name="search"></param>
        /// <param name="sortDirection"></param>
        /// <returns>The DataSet and all the retrieved data records as a DataTable object.</returns>
        public static DataTable SelectFromDatabaseTable(
            string tableName, 
            int    offset             = -1, 
            int    numberOfRows       = -1, 
            bool   uniqueResult       = false, 
            string uniqueFilterColumn = "", 
            string whereColumn        = "", 
            string whereValue         = "", 
            bool   search             = false,
            string secondWhereColumn  = "", 
            string secondWhereValue   = "", 
            bool   secondSearch       = false,
            bool   whereMatchBoth     = false,
            string sortDirection      = SortDirection.Ascending,
            string orderByColumns     = "id"
        ) {
            // Initialize a database query instance.
            SQL selectDB;

            if ((tableName == @"sync_status") || (tableName == @"disabled_movie_details"))
            {
                selectDB = new SQL(Database.SQL.DatabaseType.MediaOrganizer);
            }
            else
            {
                selectDB = new SQL();
            }

            DataTable databaseTableData = new DataTable();

            // Set the selected table.
            selectDB.TableName = tableName;

            // Build the SQL Query string.
            selectDB.SqlQuery = @"SELECT ";

            // Select only unique column values.
            if (uniqueResult && !String.IsNullOrEmpty(uniqueFilterColumn))
            {
                selectDB.SqlQuery += @" DISTINCT " + uniqueFilterColumn + @" ";
            }
            // Select all column values.
            else
            {
                selectDB.SqlQuery += @" * ";
            }

            if (!String.IsNullOrEmpty(selectDB.SqlQuery))
            {
                // Select from the specified database table.
                selectDB.SqlQuery += @" FROM " + tableName + @" ";

                // Filter the result.
                if (!String.IsNullOrEmpty(whereColumn) && !String.IsNullOrEmpty(whereValue))
                {
                    selectDB.SqlQuery += @" WHERE " + whereColumn + @" ";

                    // Filter by a search string.
                    if (search)
                    {
                        selectDB.SqlQuery += @" LIKE '%" + whereValue + @"%' ";
                    }
                    // Filter by exact column value.
                    else
                    {
                        selectDB.SqlQuery += @" = '" + whereValue + @"' ";
                    }
                }

                if (!String.IsNullOrEmpty(secondWhereColumn) && !String.IsNullOrEmpty(secondWhereValue))
                {
                    // If wherMatchBoth is set to true, both WHERE conditions must match.
                    if (whereMatchBoth)
                    {
                        selectDB.SqlQuery += @" AND " + secondWhereColumn + @" ";
                    }
                    // Otherwise only one of them has to match.
                    else
                    {
                        selectDB.SqlQuery += @" OR " + secondWhereColumn + @" ";
                    }

                    // Filter by a search string.
                    if (secondSearch)
                    {
                        selectDB.SqlQuery += @" LIKE '%" + secondWhereValue + @"%' ";
                    }
                    // Filter by exact column value.
                    else
                    {
                        selectDB.SqlQuery += @" = '" + secondWhereValue + @"' ";
                    }
                }

                // Sort the result.
                if (uniqueResult && !String.IsNullOrEmpty(uniqueFilterColumn))
                {
                    selectDB.SqlQuery += @" ORDER BY " + uniqueFilterColumn + @" " + sortDirection + " ";
                }
                else if (!String.IsNullOrEmpty(orderByColumns))
                {
                    selectDB.SqlQuery += @" ORDER BY " + orderByColumns + " " + sortDirection + " ";
                }
                else
                {
                    if ((tableName == @"audio") || (tableName == @"pictures") || (tableName == @"videos"))
                    {
                        selectDB.SqlQuery += @" ORDER BY path, filename, file_type " + sortDirection + " ";
                    }
                    else
                    {
                        selectDB.SqlQuery += @" ORDER BY id " + sortDirection + " ";
                    }
                }

                // Retrieve only results after the specified Offset.
                if (offset == -1)
                {
                    selectDB.SqlQuery += @" OFFSET 0 ROWS ";
                }
                else
                {
                    selectDB.SqlQuery += @" OFFSET " + offset.ToString() + @" ROWS ";
                }

                // Retrieve only the number of rows specified after the specified offset.
                if (numberOfRows == -1)
                {
                    selectDB.SqlQuery += @" FETCH NEXT 100000 ROWS ONLY ";
                }
                else
                {
                    selectDB.SqlQuery += @" FETCH NEXT " + numberOfRows.ToString() + @" ROWS ONLY ";
                }
                
                // Try to retrieve the data from the database table.
                databaseTableData = selectDB.GetData();
            }

            // Return the DataSet as a DataTable object.
            return databaseTableData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="syncAction"></param>
        /// <param name="directoryPath"></param>
        public static void SetSyncStatusAction(string syncAction, string directoryPath)
        {
            try
            {
                if (!String.IsNullOrEmpty(SyncAction.syncJobId))
                {
                    // Create a new SQL object.
                    SQL sql = new SQL(SQL.DatabaseType.MediaOrganizer);

                    // Set the selected table.
                    sql.TableName = "sync_status";

                    // Select values of the columns to update.
                    sql.SqlCommandParameters.Add("job_id",    SyncAction.syncJobId);
                    sql.SqlCommandParameters.Add("date",      sql.GetCurrentDate());
                    sql.SqlCommandParameters.Add("directory", directoryPath);
                    sql.SqlCommandParameters.Add("action",    syncAction);

                    // Select which table row to update.
                    sql.SqlWhereParameters.Add("job_id", SyncAction.syncJobId);

                    // Try to write the data to the database table.
                    sql.InsertData();
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: SetSyncStatusAction()
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        public static void SyncAddMediaToLibrary(string file)
        {
            if (MediaTypeValidation.IsAudio(file))
            {
                SyncAction.AddAudio(file);
            }
            else if (MediaTypeValidation.IsPicture(file))
            {
                SyncAction.AddPicture(file);
            }
            else if (MediaTypeValidation.IsVideo(file))
            {
                SyncAction.AddVideo(file);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="primaryKeyId"></param>
        public static void SyncUpdateMediaInLibrary(string file, string primaryKeyId = "")
        {
            // Get the details and properties of the file.
            FileInfo fileInfo = new FileInfo(file);

            // Update the existing media in the Media Library.
            if (MediaTypeValidation.IsAudio(file))
            {
                // Get the record details for the specified media file.
                DataTable mediaDatabaseTable = SyncAction.SelectFromDatabaseTable(@"audio", 0, 1, false, @"", @"path", fileInfo.DirectoryName, false, @"filename", fileInfo.Name.Replace(fileInfo.Extension, ""), false, true);

                if (String.IsNullOrEmpty(primaryKeyId))
                {
                    SyncAction.UpdateAudio(file, "", mediaDatabaseTable.Rows[0]["pkid"].ToString());
                }
                else
                {
                    SyncAction.UpdateAudio(file, "", primaryKeyId);
                }
            }
            else if (MediaTypeValidation.IsPicture(file))
            {
                // Get the record details for the specified media file.
                DataTable mediaDatabaseTable = SyncAction.SelectFromDatabaseTable(@"pictures", 0, 1, false, @"", @"path", fileInfo.DirectoryName, false, @"filename", fileInfo.Name.Replace(fileInfo.Extension, ""), false, true);

                if (String.IsNullOrEmpty(primaryKeyId))
                {
                    SyncAction.UpdatePicture(file, "", mediaDatabaseTable.Rows[0]["pkid"].ToString());
                }
                else
                {
                    SyncAction.UpdatePicture(file, "", primaryKeyId);
                }
            }
            else if (MediaTypeValidation.IsVideo(file))
            {
                // Get the record details for the specified media file.
                DataTable mediaDatabaseTable = SyncAction.SelectFromDatabaseTable(@"videos", 0, 1, false, @"", @"path", fileInfo.DirectoryName, false, @"filename", fileInfo.Name.Replace(fileInfo.Extension, ""), false, true);

                if (String.IsNullOrEmpty(primaryKeyId))
                {
                    SyncAction.UpdateVideo(file, "", mediaDatabaseTable.Rows[0]["pkid"].ToString());
                }
                else
                {
                    SyncAction.UpdateVideo(file, "", primaryKeyId);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recursiveSync"></param>
        public static void SyncRemoveDeletedMediaFromLibrary(string directoryPath = "")
        {
            // Get an updated list of all Audio file entries in the Media Library.
            DataTable audioFilesInMediaLibraryUpdated = SyncAction.SelectFromDatabaseTable("audio");

            // Remove audio files that do not exist anymore in the file system.
            foreach (DataRow tableRow in audioFilesInMediaLibraryUpdated.Rows)
            {
                string filename = String.Format(
                    @"{0}\{1}.{2}",
                    tableRow["path"].ToString(),
                    tableRow["filename"].ToString(),
                    tableRow["file_type"].ToString().ToLower()
                );

                if (!String.IsNullOrEmpty(tableRow["path"].ToString()))
                {
                    if (directoryPath.ToLower().Replace(@"\\", @"\") == tableRow["path"].ToString().ToLower() + @"\")
                    {
                        if (!File.Exists(filename))
                        {
                            //SyncAction.SetSyncStatusAction(@"Deleting """ + tableRow["filename"].ToString() + @""" ... ", tableRow["path"].ToString());

                            SyncAction.DeleteAudio(tableRow["pkid"].ToString());
                        }
                    }
                }
                else
                {
                    if (!File.Exists(filename))
                    {
                        //SyncAction.SetSyncStatusAction(@"Deleting """ + tableRow["filename"].ToString() + @""" ... ", tableRow["path"].ToString());

                        SyncAction.DeleteAudio(tableRow["pkid"].ToString());
                    }
                }
            }

            // Get an updated list of all Pictures file entries in the Media Library.
            DataTable pictureFilesInMediaLibraryUpdated = SyncAction.SelectFromDatabaseTable("pictures");

            // Remove picture files that do not exist anymore in the file system.
            foreach (DataRow tableRow in pictureFilesInMediaLibraryUpdated.Rows)
            {
                string filename = String.Format(
                    @"{0}\{1}.{2}",
                    tableRow["path"].ToString(),
                    tableRow["filename"].ToString(),
                    tableRow["file_type"].ToString().ToLower()
                );

                if (!String.IsNullOrEmpty(tableRow["path"].ToString()))
                {
                    if (directoryPath.ToLower().Replace(@"\\", @"\") == tableRow["path"].ToString().ToLower() + @"\")
                    {
                        if (!File.Exists(filename))
                        {
                            //SyncAction.SetSyncStatusAction(@"Deleting """ + tableRow["filename"].ToString() + @""" ... ", tableRow["path"].ToString());

                            SyncAction.DeletePicture(tableRow["pkid"].ToString());
                        }
                    }
                }
                else
                {
                    if (!File.Exists(filename))
                    {
                        //SyncAction.SetSyncStatusAction(@"Deleting """ + tableRow["filename"].ToString() + @""" ... ", tableRow["path"].ToString());

                        SyncAction.DeletePicture(tableRow["pkid"].ToString());
                    }
                }
            }

            // Get an updated list of all Video file entries in the Media Library.
            DataTable videoFilesInMediaLibraryUpdated = SyncAction.SelectFromDatabaseTable("videos");

            // Remove video files that do not exist anymore in the file system.
            foreach (DataRow tableRow in videoFilesInMediaLibraryUpdated.Rows)
            {
                string filename = String.Format(
                    @"{0}\{1}.{2}",
                    tableRow["path"].ToString(),
                    tableRow["filename"].ToString(),
                    tableRow["file_type"].ToString().ToLower()
                );

                if (!String.IsNullOrEmpty(tableRow["path"].ToString()))
                {
                    if (directoryPath.ToLower().Replace(@"\\", @"\") == tableRow["path"].ToString().ToLower() + @"\")
                    {
                        if (!File.Exists(filename))
                        {
                            //SyncAction.SetSyncStatusAction(@"Deleting """ + tableRow["filename"].ToString() + @""" ... ", tableRow["path"].ToString());

                            SyncAction.DeleteVideo(tableRow["pkid"].ToString());
                        }
                    }
                }
                else
                {
                    if (!File.Exists(filename))
                    {
                        //SyncAction.SetSyncStatusAction(@"Deleting """ + tableRow["filename"].ToString() + @""" ... ", tableRow["path"].ToString());

                        SyncAction.DeleteVideo(tableRow["pkid"].ToString());
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryKeyId"></param>
        /// <param name="tag"></param>
        public static void UpdateAudioId(string primaryKeyId, string tag = "")
        {
            // Get the record details for the specified pimary key ID.
            DataTable mediaDatabaseTable = SyncAction.SelectFromDatabaseTable("audio", 0, 1, false, @"", @"pkid", primaryKeyId);

            // Generate the full filename and path using the record details.
            string fileNameAndPath = String.Format(
                @"{0}\{1}.{2}",
                mediaDatabaseTable.Rows[0]["path"].ToString(),
                mediaDatabaseTable.Rows[0]["filename"].ToString(),
                mediaDatabaseTable.Rows[0]["file_type"].ToString().ToLower()
            );

            // Call the UpdateAudio method using the filename and path instead of the primary key ID.
            SyncAction.UpdateAudio(fileNameAndPath, tag, primaryKeyId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileNameAndPath"></param>
        /// <param name="tag"></param>
        /// <param name="primaryKeyId"></param>
        public static void UpdateAudio(string fileNameAndPath, string tag = "", string primaryKeyId = "")
        {
            // Main variables.
            MediaInfoDll mediaInfo = new MediaInfoDll();

            // Un-escape double back-slashes ("\\" -> "\").
            //fileNameAndPath = fileNameAndPath.Replace(@"\\", @"\");

            try
            {
                // Get file details about the selected media file.
                FileInfo fileInfo = new FileInfo(fileNameAndPath);

                //SyncAction.SetSyncStatusAction(@"Updating Audio: """ + fileInfo.Name.Replace(fileInfo.Extension, "") + @""" ... ", fileInfo.DirectoryName);

                try
                {
                    // Get media details about the selected media file.
                    mediaInfo.Open(fileNameAndPath);
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                        Class:  SyncMediaLibraryLib
                        Method: UpdateAudio(""" + fileNameAndPath + @""")
                        Action: mediaInfo.Open(""" + fileNameAndPath + @""")
                        
                        " + exception.Message
                        , EventLogEntryType.Error, 123);
                }

                // Create a new SQL object.
                SQL sql = new SQL();

                // Set the selected table.
                sql.TableName = "audio";

                // Select values of the columns to update.
                if (mediaInfo != null)
                {
                    /*
                    sql.SqlCommandParameters.Add("bit_rate", mediaInfo.Get(StreamKind.Audio, 0, @"BitRate/String"));
                    */

                    string audio         = "";
                    string audioBitRate  = mediaInfo.Get(MediaInfoLib.StreamKind.Audio, 0, @"BitRate/String").Replace(" ", "");
                    string audioChannels = mediaInfo.Get(MediaInfoLib.StreamKind.Audio, 0, @"Channel(s)");
                    string audioCodec    = mediaInfo.Get(MediaInfoLib.StreamKind.Audio, 0, @"Format");

                    if (audioBitRate.Contains(@"/"))
                    {
                        audioBitRate = audioBitRate.Substring(0, audioBitRate.IndexOf(@"/"));
                    }

                    switch (audioChannels)
                    {
                        case "1":
                            audioChannels = @"1.0";
                            break;
                        case "2":
                            audioChannels = @"2.0";
                            break;
                        case "3":
                            audioChannels = @"2.1";
                            break;
                        case "6":
                            audioChannels = @"5.1";
                            break;
                        default:
                            break;
                    }

                    if (audioCodec.Contains("AC-3"))
                    {
                        audio = @"DD " + audioChannels + @" " + audioBitRate;
                    }
                    else if (audioCodec.Contains("DTS"))
                    {
                        audio = @"DTS " + audioChannels + @" " + audioBitRate;
                    }
                    else if (audioCodec.Contains("AAC"))
                    {
                        audio = @"AAC " + audioChannels + @" " + audioBitRate;
                    }
                    else if (audioCodec.Contains("MPEG"))
                    {
                        audio = @"MP3 " + audioChannels + @" " + audioBitRate;
                    }
                    else if (audioCodec.Contains("WMA"))
                    {
                        audio = @"WMA " + audioChannels + @" " + audioBitRate;
                    }
                    else
                    {
                        audio = audioCodec + @" " + audioChannels;
                    }

                    sql.SqlCommandParameters.Add("audio",        audio);
                    sql.SqlCommandParameters.Add("album",        mediaInfo.Get(StreamKind.General, 0, @"Album"));
                    sql.SqlCommandParameters.Add("artist",       mediaInfo.Get(StreamKind.General, 0, @"Artist"));
                    sql.SqlCommandParameters.Add("cover",        mediaInfo.Get(StreamKind.General, 0, @"Cover_Data"));
                    sql.SqlCommandParameters.Add("duration",     mediaInfo.Get(StreamKind.Audio,   0, @"Duration/String"));
                    sql.SqlCommandParameters.Add("file_size",    mediaInfo.Get(StreamKind.General, 0, @"FileSize/String"));
                    sql.SqlCommandParameters.Add("genre",        mediaInfo.Get(StreamKind.General, 0, @"Genre"));
                    sql.SqlCommandParameters.Add("release_year", mediaInfo.Get(StreamKind.General, 0, @"Recorded_Date"));
                    sql.SqlCommandParameters.Add("track",        mediaInfo.Get(StreamKind.General, 0, @"Track"));
                }

                sql.SqlCommandParameters.Add("unique_id", Guid.NewGuid().ToString());
                sql.SqlCommandParameters.Add("filename",  fileInfo.Name.Replace(fileInfo.Extension, ""));
                sql.SqlCommandParameters.Add("path",      fileInfo.DirectoryName);
                sql.SqlCommandParameters.Add("file_type", fileInfo.Extension.Substring(1).ToUpper());

                // Update the Tag column if a valid value is inputted.
                if (!String.IsNullOrEmpty(tag))
                {
                    sql.SqlCommandParameters.Add("tag", tag);
                }

                // Select which table records to update.
                sql.SqlWhereParameters.Add("where_pkid", primaryKeyId);

                // Try to write the data to the database table.
                sql.UpdateData();

                //SyncAction.SetSyncStatusAction(@"System is currently being scanned for valid Audio, Pictures and Videos ... ", fileInfo.DirectoryName);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: UpdateAudio(""" + fileNameAndPath + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            finally
            {
                // Dispose/Close the mediaInfo object.
                mediaInfo.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryKeyId"></param>
        /// <param name="tag"></param>
        public static void UpdatePictureId(string primaryKeyId, string tag = "", string comment = "")
        {
            // Get the record details for the specified pimary key ID.
            DataTable mediaDatabaseTable = SyncAction.SelectFromDatabaseTable("pictures", 0, 1, false, @"", @"pkid", primaryKeyId);

            // Generate the full filename and path using the record details.
            //string fileNameAndPath = @"" + mediaDatabaseTable.Rows[0]["path"].ToString() + @"\" + mediaDatabaseTable.Rows[0]["filename"].ToString() + @"." + mediaDatabaseTable.Rows[0]["file_type"].ToString().ToLower() + @"";
            string fileNameAndPath = String.Format(
                @"{0}\{1}.{2}",
                mediaDatabaseTable.Rows[0]["path"].ToString(),
                mediaDatabaseTable.Rows[0]["filename"].ToString(),
                mediaDatabaseTable.Rows[0]["file_type"].ToString().ToLower()
            );

            // Call the UpdatePicture method using the filename and path instead of the primary key ID.
            SyncAction.UpdatePicture(fileNameAndPath, tag, comment, primaryKeyId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileNameAndPath"></param>
        /// <param name="tag"></param>
        /// <param name="comment"></param>
        /// <param name="primaryKeyId"></param>
        public static void UpdatePicture(string fileNameAndPath, string tag = "", string comment = "", string primaryKeyId = "")
        {
            // Main variables.
            MediaInfoDll mediaInfo = new MediaInfoDll();

            // Un-escape double back-slashes ("\\" -> "\").
            //fileNameAndPath = fileNameAndPath.Replace(@"\\", @"\");

            try
            {
                // Get file details about the selected media file.
                FileInfo fileInfo = new FileInfo(fileNameAndPath);

                //SyncAction.SetSyncStatusAction(@"Updating Picture: """ + fileInfo.Name.Replace(fileInfo.Extension, "") + @""" ... ", fileInfo.DirectoryName);

                try
                {
                    // Get media details about the selected media file.
                    mediaInfo.Open(fileNameAndPath);
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                        Class:  SyncMediaLibraryLib
                        Method: UpdatePicture(""" + fileNameAndPath + @""")
                        Action: mediaInfo.Open(""" + fileNameAndPath + @""")
                        
                        " + exception.Message
                        , EventLogEntryType.Error, 123);
                }

                // Create a new SQL object.
                SQL sql = new SQL();

                // Set the selected table.
                sql.TableName = "pictures";

                // Select values of the columns to update.
                if (mediaInfo != null)
                {
                    sql.SqlCommandParameters.Add("file_size",  mediaInfo.Get(StreamKind.General, 0, @"FileSize/String"));
                    sql.SqlCommandParameters.Add("dimensions", mediaInfo.Get(StreamKind.Image,   0, @"Width") + @" x " + mediaInfo.Get(StreamKind.Image, 0, @"Height/String"));
                }

                sql.SqlCommandParameters.Add("unique_id",  Guid.NewGuid().ToString());
                sql.SqlCommandParameters.Add("filename",   fileInfo.Name.Replace(fileInfo.Extension, ""));
                sql.SqlCommandParameters.Add("path",       fileInfo.DirectoryName);
                sql.SqlCommandParameters.Add("file_type",  fileInfo.Extension.Substring(1).ToUpper());

                // Update the Comment column if a valid value is inputted.
                if (!String.IsNullOrEmpty(comment))
                {
                    sql.SqlCommandParameters.Add("comment", comment);
                }

                // Update the Tag column if a valid value is inputted.
                if (!String.IsNullOrEmpty(tag))
                {
                    sql.SqlCommandParameters.Add("tag", tag);
                }

                // Select which table records to update.
                sql.SqlWhereParameters.Add("where_pkid", primaryKeyId);

                // Try to write the data to the database table.
                sql.UpdateData();

                //SyncAction.SetSyncStatusAction(@"System is currently being scanned for valid Audio, Pictures and Videos ... ", fileInfo.DirectoryName);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: UpdatePicture(""" + fileNameAndPath + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            finally
            {
                // Dispose/Close the mediaInfo object.
                mediaInfo.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryKeyId"></param>
        /// <param name="tag"></param>
        public static void UpdateVideoId(string primaryKeyId, string tag = "")
        {
            // Get the record details for the specified pimary key ID.
            DataTable mediaDatabaseTable = SyncAction.SelectFromDatabaseTable("videos", 0, 1, false, @"", @"pkid", primaryKeyId);

            // Generate the full filename and path using the record details.
            string fileNameAndPath = String.Format(
                @"{0}\{1}.{2}", 
                mediaDatabaseTable.Rows[0]["path"].ToString(), 
                mediaDatabaseTable.Rows[0]["filename"].ToString(), 
                mediaDatabaseTable.Rows[0]["file_type"].ToString().ToLower()
            );

            // Call the UpdateVideo method using the filename and path instead of the primary key ID.
            SyncAction.UpdateVideo(fileNameAndPath, tag, primaryKeyId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileNameAndPath"></param>
        /// <param name="tag"></param>
        /// <param name="primaryKeyId"></param>
        public static void UpdateVideo(string fileNameAndPath, string tag = "", string primaryKeyId = "")
        {
            // Un-escape double back-slashes ("\\" -> "\").
            //fileNameAndPath = fileNameAndPath.Replace(@"\\", @"\");

            // Main variables.
            FileInfo                   fileInfo                    = new FileInfo(fileNameAndPath);
            MediaInfoDll               mediaInfo                   = new MediaInfoDll();
            Dictionary<string, string> movieDetails                = new Dictionary<string, string>() { };
            string                     movieThumbnailerApplication = "";
            string                     movieThumbnailerArguments   = "";
            string                     movieThumbnailerOutputPath  = "";
            Dictionary<string, string> tvShowEpisodeDetails        = new Dictionary<string, string>() { };

            try
            {
                // Set the Movie Thumbnailer arguments to create a 1x1 screenshot image.
                movieThumbnailerApplication = DirectoryManagement.GetRuntimeExecutingPath() + @"\3rd\mtn\mtn.exe";
                movieThumbnailerOutputPath  = DirectoryManagement.GetRuntimeExecutingPath() + @"\images\Screenshots";
                movieThumbnailerArguments   = String.Format(@"-r 1 -c 1 -a 1.78 -w 300 -i -n -t -W -o ""_{0}.jpg"" -O ""{1}"" ""{2}"" -P", primaryKeyId, movieThumbnailerOutputPath, fileNameAndPath);

                // Run the Movie Thumbnailer with the above arguments.
                if (!File.Exists(String.Format(@"{0}\{1}_{2}.jpg", movieThumbnailerOutputPath, fileInfo.Name.Replace(fileInfo.Extension, ""), primaryKeyId)))
                {
                    ProcessManagement.StartHiddenShellProcess(movieThumbnailerApplication, movieThumbnailerArguments);
                }

                // Wait 100 milliseconds before continuing.
                Thread.Sleep(100);

                // Set the Movie Thumbnailer arguments to create a 2x2 screenshot image.
                movieThumbnailerApplication = DirectoryManagement.GetRuntimeExecutingPath() + @"\3rd\mtn\mtn.exe";
                movieThumbnailerOutputPath  = DirectoryManagement.GetRuntimeExecutingPath() + @"\images\Backdrops";
                movieThumbnailerArguments   = String.Format(@"-r 2 -c 2 -a 1.78 -w 1920 -i -n -t -W -o ""_{0}.jpg"" -O ""{1}"" ""{2}"" -P", primaryKeyId, movieThumbnailerOutputPath, fileNameAndPath);

                // Run the Movie Thumbnailer with the above arguments.
                if (!File.Exists(String.Format(@"{0}\{1}_{2}.jpg", movieThumbnailerOutputPath, fileInfo.Name.Replace(fileInfo.Extension, ""), primaryKeyId)))
                {
                    ProcessManagement.StartHiddenShellProcess(movieThumbnailerApplication, movieThumbnailerArguments);
                }

                // Wait 100 milliseconds before continuing.
                Thread.Sleep(100);

                //SyncAction.SetSyncStatusAction(@"Updating Video: """ + fileInfo.Name.Replace(fileInfo.Extension, "") + @""" ... ", fileInfo.DirectoryName);

                // Get existing media library details about the selected media file.
                DataTable existingMediaDetails = SyncAction.SelectFromDatabaseTable("videos", 0, 1, false, "", "pkid", primaryKeyId);

                // Create a new SQL object.
                SQL sql = new SQL();

                // Set the selected table.
                sql.TableName = "videos";
                
                if (String.IsNullOrEmpty(existingMediaDetails.Rows[0]["file_size"].ToString()))
                {
                    try
                    {
                        // Get media details about the selected media file.
                        mediaInfo.Open(fileNameAndPath);
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                            Class:  SyncMediaLibraryLib
                            Method: AddVideo(""" + fileNameAndPath + @""")
                            Action: mediaInfo.Open(""" + fileNameAndPath + @""")
                        
                            " + exception.Message
                            , EventLogEntryType.Error, 123);
                    }

                    // Select values of the columns to add.
                    if (mediaInfo != null)
                    {
                        // Main variables.
                        string audio          = "";
                        string audioBitRate   = "";
                        string audioChannels  = "";
                        string audioCodec     = "";
                        int    height         = 0;
                        int    width          = 0;
                        string video          = "";
                        string videoCodec     = "";
                        string videoBitRate   = "";
                        string videoFrameRate = "";
                        string videoHeight    = "";
                        string videoWidth     = "";
                        string videoType      = "";
                    
                        // Get Audio Codec details.
                        audioBitRate  = mediaInfo.Get(StreamKind.Audio, 0, @"BitRate/String").Replace(" ", "");
                        audioChannels = mediaInfo.Get(StreamKind.Audio, 0, @"Channel(s)");
                        audioCodec    = mediaInfo.Get(StreamKind.Audio, 0, @"Format");

                        // Only use the first bitrate if the audio track contains various bitrates.
                        if (audioBitRate.Contains(@"/"))
                        {
                            audioBitRate = audioBitRate.Substring(0, audioBitRate.IndexOf(@"/"));
                        }

                        // Get the number of audio channels in the audio track.
                        switch (audioChannels)
                        {
                            case "1":
                                audioChannels = @"1.0";
                                break;
                            case "2":
                                audioChannels = @"2.0";
                                break;
                            case "3":
                                audioChannels = @"2.1";
                                break;
                            case "6":
                                audioChannels = @"5.1";
                                break;
                            default:
                                break;
                        }

                        // Get the audio codec in the audio track.
                        if (audioCodec.Contains("AC-3"))
                        {
                            audio = @"DD " + audioChannels + @" " + audioBitRate;
                        }
                        else if (audioCodec.Contains("DTS"))
                        {
                            audio = @"DTS " + audioChannels + @" " + audioBitRate;
                        }
                        else if (audioCodec.Contains("AAC"))
                        {
                            audio = @"AAC " + audioChannels + @" " + audioBitRate;
                        }
                        else if (audioCodec.Contains("MPEG"))
                        {
                            audio = @"MP3 " + audioChannels + @" " + audioBitRate;
                        }
                        else if (audioCodec.Contains("WMA"))
                        {
                            audio = @"WMA " + audioChannels + @" " + audioBitRate;
                        }
                        else
                        {
                            audio = audioCodec + @" " + audioChannels;
                        }

                        // Get video codec details.
                        videoBitRate   = mediaInfo.Get(StreamKind.Video, 0, @"BitRate/String").Replace(" ", "");
                        videoCodec     = mediaInfo.Get(StreamKind.Video, 0, @"Encoded_Library/Name");
                        videoFrameRate = mediaInfo.Get(StreamKind.Video, 0, @"FrameRate/String").Replace(" ", "").Replace(@".000", "");
                        videoHeight    = mediaInfo.Get(StreamKind.Video, 0, @"Height").Replace(" ", "");
                        videoWidth     = mediaInfo.Get(StreamKind.Video, 0, @"Width").Replace(" ", "");

                        // Get the video resolution.
                        if (!String.IsNullOrEmpty(videoHeight) && !String.IsNullOrEmpty(videoWidth))
                        {
                            height = Convert.ToInt32(videoHeight);
                            width  = Convert.ToInt32(videoWidth);

                            if (height > 721)
                            {
                                videoType = @"1080p";
                            }
                            if (width > 1281)
                            {
                                videoType = @"1080p";
                            }
                            else if (height > 577)
                            {
                                videoType = @"720p";
                            }
                            else if (width > 721)
                            {
                                videoType = @"720p";
                            }
                            else
                            {
                                videoType = @"DVD";
                            }
                        }

                        // Concatenate the video codec details into one string.
                        video = videoCodec + @" " + videoType + @" " + videoBitRate + @" " + videoFrameRate;

                        // Add the media details to the SQL command parameter values.
                        sql.SqlCommandParameters.Add("audio",     audio);
                        sql.SqlCommandParameters.Add("duration",  mediaInfo.Get(StreamKind.Video,   0, @"Duration/String"));
                        sql.SqlCommandParameters.Add("file_size", mediaInfo.Get(StreamKind.General, 0, @"FileSize/String"));
                        sql.SqlCommandParameters.Add("video",     video);
                    }
                }
                
                // Update the Tag column if a valid value is inputted.
                if (!String.IsNullOrEmpty(tag))
                {
                    sql.SqlCommandParameters.Add("tag", tag);
                }

                if (String.IsNullOrEmpty(existingMediaDetails.Rows[0]["episode_details_title"].ToString()) && String.IsNullOrEmpty(existingMediaDetails.Rows[0]["movie_details_name"].ToString()))
                {
                    // Get details from TVRage.com if the file is a TV Show Episode.
                    if (TvInfo.IsTvShow(fileInfo.Name.Replace(fileInfo.Extension, ""), fileInfo.DirectoryName))
                    {
                        //SyncAction.SetSyncStatusAction(@"Getting TV Show Details for """ + fileInfo.Name.Replace(fileInfo.Extension, "") + @"""", fileInfo.DirectoryName);

                        string tvShowSeasonEpisode = TvInfo.GetSeasonEpisodeNumbers(fileInfo.Name.Replace(fileInfo.Extension, ""), fileInfo.DirectoryName);

                        // Get the tv show episode details about the selected media file.
                        tvShowEpisodeDetails = TvInfo.GetEpisodeDetails(fileInfo.Name.Replace(fileInfo.Extension, ""), fileInfo.DirectoryName, tvShowSeasonEpisode);

                        //SyncAction.SetSyncStatusAction(@"System is currently being scanned for valid Audio, Pictures and Videos ... ", fileInfo.DirectoryName);
                    }
                    // Otherwise get details from TheMovieDB.org if the file is a Movie.
                    else
                    {
                        //SyncAction.SetSyncStatusAction(@"Getting Movie Details for """ + fileInfo.Name.Replace(fileInfo.Extension, "") + @"""", fileInfo.DirectoryName);

                        // Get the movie details about the selected media file.
                        movieDetails = MediaInfoApi.MovieInfo.GetMovieDetails(fileInfo.Name.Replace(fileInfo.Extension, ""));

                        //SyncAction.SetSyncStatusAction(@"System is currently being scanned for valid Audio, Pictures and Videos ... ", fileInfo.DirectoryName);
                    }

                    if (tvShowEpisodeDetails.ContainsKey("title") && !String.IsNullOrEmpty(tvShowEpisodeDetails["title"]))
                    {
                        sql.SqlCommandParameters.Add("episode_details_airdate",      tvShowEpisodeDetails["airdate"]);
                        sql.SqlCommandParameters.Add("episode_details_cast",         tvShowEpisodeDetails["cast"]);
                        sql.SqlCommandParameters.Add("episode_details_cover",        tvShowEpisodeDetails["image"]);
                        sql.SqlCommandParameters.Add("episode_details_director",     tvShowEpisodeDetails["director"]);
                        sql.SqlCommandParameters.Add("episode_details_genre",        tvShowEpisodeDetails["genre"]);
                        sql.SqlCommandParameters.Add("episode_details_name",         tvShowEpisodeDetails["name"]);
                        sql.SqlCommandParameters.Add("episode_details_next_airdate", tvShowEpisodeDetails["next_airdate"]);
                        sql.SqlCommandParameters.Add("episode_details_next_number",  tvShowEpisodeDetails["next_number"]);
                        sql.SqlCommandParameters.Add("episode_details_next_title",   tvShowEpisodeDetails["next_title"]);
                        sql.SqlCommandParameters.Add("episode_details_number",       tvShowEpisodeDetails["number"]);
                        sql.SqlCommandParameters.Add("episode_details_summary",      tvShowEpisodeDetails["summary"]);
                        sql.SqlCommandParameters.Add("episode_details_title",        tvShowEpisodeDetails["title"]);
                        sql.SqlCommandParameters.Add("episode_details_writers",      tvShowEpisodeDetails["writers"]);
                    }
                    else if (movieDetails.ContainsKey("name") && !String.IsNullOrEmpty(movieDetails["name"]))
                    {
                        sql.SqlCommandParameters.Add("movie_details_backdrop", movieDetails["backdrop"]);
                        sql.SqlCommandParameters.Add("movie_details_cast",     movieDetails["cast"]);
                        sql.SqlCommandParameters.Add("movie_details_cover",    movieDetails["cover"]);
                        sql.SqlCommandParameters.Add("movie_details_director", movieDetails["director"]);
                        sql.SqlCommandParameters.Add("movie_details_genre",    movieDetails["genre"]);
                        sql.SqlCommandParameters.Add("movie_details_id",       movieDetails["id"]);
                        sql.SqlCommandParameters.Add("movie_details_name",     movieDetails["name"]);
                        sql.SqlCommandParameters.Add("movie_details_released", movieDetails["released"]);
                        sql.SqlCommandParameters.Add("movie_details_summary",  movieDetails["summary"]);
                        sql.SqlCommandParameters.Add("movie_details_trailer",  movieDetails["trailer"]);
                        sql.SqlCommandParameters.Add("movie_details_writers",  movieDetails["writers"]);
                    }
                    else
                    {
                        sql.SqlCommandParameters.Add("movie_details_backdrop",       "DISABLED");
                        sql.SqlCommandParameters.Add("movie_details_cast",           "DISABLED");
                        sql.SqlCommandParameters.Add("movie_details_cover",          "DISABLED");
                        sql.SqlCommandParameters.Add("movie_details_director",       "DISABLED");
                        sql.SqlCommandParameters.Add("movie_details_genre",          "DISABLED");
                        sql.SqlCommandParameters.Add("movie_details_id",             "DISABLED");
                        sql.SqlCommandParameters.Add("movie_details_name",           "DISABLED");
                        sql.SqlCommandParameters.Add("movie_details_released",       "DISABLED");
                        sql.SqlCommandParameters.Add("movie_details_summary",        "DISABLED");
                        sql.SqlCommandParameters.Add("movie_details_trailer",        "DISABLED");
                        sql.SqlCommandParameters.Add("movie_details_writers",        "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_airdate",      "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_cast",         "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_cover",        "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_director",     "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_genre",        "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_next_airdate", "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_next_number",  "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_next_title",   "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_number",       "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_summary",      "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_title",        "DISABLED");
                        sql.SqlCommandParameters.Add("episode_details_writers",      "DISABLED");
                    }
                }

                // Select which table records to update.
                sql.SqlWhereParameters.Add("where_pkid", primaryKeyId);

                // Try to write the data to the database table.
                sql.UpdateData();

                //SyncAction.SetSyncStatusAction(@"System is currently being scanned for valid Audio, Pictures and Videos ... ", fileInfo.DirectoryName);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: UpdateVideo(""" + fileNameAndPath + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            finally
            {
                // Dispose/Close the mediaInfo object.
                mediaInfo.Close();
            }
        }

        /// <summary>
        /// Updates the media library with the media's completion status, the date it was last played and how much of the media completed.
        /// </summary>
        /// <param name="mediaType"></param>
        /// <param name="primaryKeyId"></param>
        /// <param name="completed"></param>
        /// <param name="lastSeenDate"></param>
        /// <param name="lastSeenDuration"></param>
        public static void UpdateMediaLastPlayed(string mediaType, string primaryKeyId, bool completed, string lastPlayedDate, int lastPlayedDuration)
        {
            try
            {
                // Create a new SQL object.
                SQL sql = new SQL();

                // Set the selected table.
                sql.TableName = mediaType;

                // Get existing media library details about the selected media file.
                DataTable existingMediaDetails = SyncAction.SelectFromDatabaseTable(mediaType, 0, 1, false, "", "pkid", primaryKeyId);

                // Set the completed column value.
                if (completed)
                {
                    sql.SqlCommandParameters.Add("completed", "1");

                    // Increase the number of times the media has been played.
                    if (!String.IsNullOrEmpty(existingMediaDetails.Rows[0]["nr_of_times_played"].ToString()))
                    {
                        try
                        {
                            // Get the current number of times played column value, and add 1 to the existing value.
                            int numberOfTimesPlayed = (int)(Convert.ToInt32(existingMediaDetails.Rows[0]["nr_of_times_played"].ToString()) + 1);

                            sql.SqlCommandParameters.Add("nr_of_times_played", numberOfTimesPlayed.ToString());
                        }
                        catch (Exception exception)
                        {
                            EventLog.WriteEntry(resourceManager.GetString("application_title"),
                                @"
                                Class:  SyncMediaLibraryLib
                                Method: UpdateMediaLastPlayed(""" + mediaType + @""", """ + primaryKeyId + @""", """ + completed + @""", """ + lastPlayedDate + @""", """ + lastPlayedDuration + @""")
                                Action: int numberOfTimesPlayed = (int)(Convert.ToInt32(" + existingMediaDetails.Rows[0]["nr_of_times_played"].ToString() + @") + 1);
                                
                                " + exception.Message
                                , EventLogEntryType.Error, 123);
                        }
                    }
                }
                else
                {
                    sql.SqlCommandParameters.Add("completed", "0");
                }

                // Set the last seen date column value.
                sql.SqlCommandParameters.Add("last_played_date", lastPlayedDate);

                // Set the last seen duration column value.
                sql.SqlCommandParameters.Add("last_played_duration", lastPlayedDuration.ToString());

                // Select which table records to update.
                sql.SqlWhereParameters.Add("where_pkid", primaryKeyId);

                // Try to write the data to the database table.
                sql.UpdateData();
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SyncMediaLibraryLib
                    Method: UpdateMediaLastPlayed(""" + mediaType + @""", """ + primaryKeyId + @""", """ + completed + @""", """ + lastPlayedDate + @""", """ + lastPlayedDuration + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

    }
}
