using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using FileSystem;
using MediaInfoLib;
using MediaManagement;
using SyncMediaLibraryLib;

namespace SyncMediaLibrary
{
    /// <summary>
    /// Main Class contains class properties and methods for SyncMediaLibrary.exe
    /// </summary>
    public class MainClass
    {
        // Private class member properties.
        private static ResourceManager resourceManager = new ResourceManager("SyncMediaLibrary.Properties.Resources", Assembly.GetExecutingAssembly());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootDirectory"></param>
        /// <param name="audioFilesInMediaLibrary"></param>
        /// <param name="pictureFilesInMediaLibrary"></param>
        /// <param name="videoFilesInMediaLibrary"></param>
        /// <param name="recursiveSync"></param>
        private static void SyncFilesInDirectory(string rootDirectory, DataTable audioFilesInMediaLibrary, DataTable pictureFilesInMediaLibrary, DataTable videoFilesInMediaLibrary, bool recursiveSync = true)
        {
            // Skip certain system directories.
            if (
                !rootDirectory.ToUpper().Contains(@".SVN") &&
                !rootDirectory.ToUpper().Contains(@"$RECYCLE.BIN") &&
                !rootDirectory.ToUpper().Contains(@"RECYCLER")
            ) {
                #if (DEBUG)
                    Console.Clear();
                    Console.WriteLine(@"Synchronizing """ + rootDirectory + @""" ... ");
                    Console.WriteLine();
                    //SyncAction.SetSyncStatusAction(@"Synchronizing """ + rootDirectory + @""" ... ");
                #endif

                // Get a list of all files in the root directory and all sub directories.
                HashSet<string> filesInDirectory              = new HashSet<string>() { };
                string[]        filesInRootDirectory          = new string[] { };
                string[]        filesInSubDirectories         = new string[] { };
                string[]        subDirectoriesInRootDirectory = new string[] { };

                try
                {
                    // Get a list of all files in the root directory.
                    filesInRootDirectory = Directory.GetFiles(rootDirectory, @"*", SearchOption.TopDirectoryOnly);

                    // Get a list of all sub-directories.
                    if (recursiveSync)
                    {
                        subDirectoriesInRootDirectory = Directory.GetDirectories(rootDirectory);
                    }
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                        Class:  MainClass (" + resourceManager.GetString("application_name") + @".exe)
                        Method: SyncFilesInDirectory(""" + rootDirectory + @""", DataTable audioFilesInMediaLibrary, DataTable pictureFilesInMediaLibrary, DataTable videoFilesInMediaLibrary, " + recursiveSync.ToString() + @")
                        Action: // Get a list of all files in the root directory.
                        
                        " + exception.Message
                        , EventLogEntryType.Error, 123
                    );
                }

                // Add all root files to the list.
                foreach (string rootFile in filesInRootDirectory)
                {
                    filesInDirectory.Add(rootFile);
                }

                #if (DEBUG)
                    Console.WriteLine("ROOT DIR: " + rootDirectory + " - ROOT FILES: " + filesInRootDirectory.Length.ToString() + " - TOTAL FILES: " + filesInDirectory.Count.ToString());
                    Console.WriteLine();
                #endif

                // Get a list of all files in the sub-directories.
                if (recursiveSync)
                {
                    foreach (string subDirectory in subDirectoriesInRootDirectory)
                    {
                        try
                        {
                            filesInSubDirectories = Directory.GetFiles(subDirectory, @"*", SearchOption.AllDirectories);
                        }
                        catch (Exception exception)
                        {
                            EventLog.WriteEntry(resourceManager.GetString("application_title"),
                                @"
                                Class:  MainClass (" + resourceManager.GetString("application_name") + @".exe)
                                Method: SyncFilesInDirectory(""" + rootDirectory + @""", DataTable audioFilesInMediaLibrary, DataTable pictureFilesInMediaLibrary, DataTable videoFilesInMediaLibrary, " + recursiveSync.ToString() + @")
                                Action: // Get a list of all files in the sub-directories.
                                
                                " + exception.Message
                                , EventLogEntryType.Error, 123
                            );
                        }

                        // Add all sub-directory files to the list.
                        foreach (string subDirectoryFile in filesInSubDirectories)
                        {
                            filesInDirectory.Add(subDirectoryFile);
                        }

                        #if (DEBUG)
                            Console.WriteLine("SUBDIR: " + subDirectory + " - SUBDIR FILES: " + filesInSubDirectories.Length.ToString() + " - TOTAL FILES: " + filesInDirectory.Count.ToString());
                        #endif
                    }
                }

                // Initialize sorted lists that will make it quicker to lookup a media file.
                HashSet<string> audioFilesInLibrary   = new HashSet<string>() { };
                HashSet<string> pictureFilesInLibrary = new HashSet<string>() { };
                HashSet<string> videoFilesInLibrary   = new HashSet<string>() { };

                // Fill the sorted list above with the data from the audio table.
                foreach (DataRow tableRow in audioFilesInMediaLibrary.Rows)
                {
                    audioFilesInLibrary.Add(String.Format(@"{0}\{1}.{2}", tableRow["path"].ToString(), tableRow["filename"].ToString(), tableRow["file_type"].ToString()).ToLower());
                }

                // Fill the sorted list above with the data from the picture table.
                foreach (DataRow tableRow in pictureFilesInMediaLibrary.Rows)
                {
                    pictureFilesInLibrary.Add(String.Format(@"{0}\{1}.{2}", tableRow["path"].ToString(), tableRow["filename"].ToString(), tableRow["file_type"].ToString()).ToLower());
                }

                // Fill the sorted list above with the data from the video table.
                foreach (DataRow tableRow in videoFilesInMediaLibrary.Rows)
                {
                    videoFilesInLibrary.Add(String.Format(@"{0}\{1}.{2}", tableRow["path"].ToString(), tableRow["filename"].ToString(), tableRow["file_type"].ToString()).ToLower());
                }

                // Iterate all files in the disk drive.
                foreach (string file in filesInDirectory)
                {
                    try
                    {
                        // Get the media files in the media library.
                        HashSet<string> mediaFilesInMediaLibrary = new HashSet<string>() { };

                        if (MediaTypeValidation.IsAudio(file))
                        {
                            mediaFilesInMediaLibrary = audioFilesInLibrary;
                        }
                        else if (MediaTypeValidation.IsPicture(file))
                        {
                            mediaFilesInMediaLibrary = pictureFilesInLibrary;
                        }
                        else if (MediaTypeValidation.IsVideo(file))
                        {
                            mediaFilesInMediaLibrary = videoFilesInLibrary;
                        }

                        // Get the details and properties of the file.
                        FileInfo fileInfo = null;

                        // Check if the fully qualified file name is less than MAX_CHARS of 260 characters.
                        if (file.Length < 260)
                        {
                            fileInfo = new FileInfo(file);
                        }
                        
                        // Skip small system files that are smaller than 100KB, SVN files and non-Media files.
                        if ((fileInfo != null) &&
                            (fileInfo.Length > 102400) &&
                            !fileInfo.Extension.Contains(@".svn") &&
                            (
                                (MediaTypeValidation.IsAudio(file)) ||
                                (MediaTypeValidation.IsPicture(file)) ||
                                (MediaTypeValidation.IsVideo(file))
                            )
                        )
                        {
                            // Add the file to the the Media Library if it's new.
                            if (!mediaFilesInMediaLibrary.Contains(String.Format(@"{0}\{1}", fileInfo.DirectoryName.TrimEnd('\\'), fileInfo.Name).ToLower()))
                            {
                                // Make sure the file is a valid file.
                                if (File.Exists(file))
                                {
                                    // Add the file to the Media Library.
                                    SyncAction.SyncAddMediaToLibrary(file);
                                }
                            }
                            /*
                            // Otherwise update the existing media entry.
                            else
                            {
                                // Make sure the file is a valid file.
                                if (File.Exists(file))
                                {
                                    // Update the file details in the Media Library.
                                    SyncAction.SyncUpdateMediaInLibrary(file);
                                }
                            }
                            */
                        }
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                                Class:  MainClass (" + resourceManager.GetString("application_name") + @".exe)
                                Method: SyncFilesInDirectory(""" + rootDirectory + @""", DataTable audioFilesInMediaLibrary, DataTable pictureFilesInMediaLibrary, DataTable videoFilesInMediaLibrary, " + recursiveSync.ToString() + @")
                                Action: // Get the media files in the media library. (" + file + @")
                                
                                " + exception.Message
                            , EventLogEntryType.Error, 123
                        );
                    }
                }

                #if (DEBUG)
                    Console.WriteLine();
                    Console.WriteLine("TOTAL FILES: " + filesInDirectory.Count.ToString());
                    Console.WriteLine();
                    //Console.Read();
                    //Console.Read();
                #endif
            }
        }

        /// <summary>
        /// Main Entrypoint for SyncMediaLibrary.exe
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            #if (DEBUG)
                // Timer variables.
                DateTime deleteStart  = new DateTime();
                TimeSpan deleteEnd    = new TimeSpan();
                DateTime addStart     = new DateTime();
                TimeSpan addEnd       = new TimeSpan();
                DateTime vidInfoStart = new DateTime();
                TimeSpan vidInfoEnd   = new TimeSpan();
                
                deleteStart = DateTime.Now;
            #endif

            // Create a Windows Event Log Source.
            if (!EventLog.SourceExists(resourceManager.GetString("application_title")))
            {
                EventLog.CreateEventSource(resourceManager.GetString("application_title"), "Application");
            }

            // Assign the new GUID to the class property of the SyncAction class.
            SyncAction.SyncJobId = Guid.NewGuid().ToString();

            // Set job date and status to started using syncJobId as unique ID.
            if (args.Length > 0)
            {
                SyncAction.SetSyncStatusAction(@"started", args[0]);
            }
            else
            {
                SyncAction.SetSyncStatusAction(@"started", @"FULL");
            }
            
            #if (DEBUG)
                Console.Clear();
                Console.Write("Preparing to remove deleted media files from the media library ... ");
                //SyncAction.SetSyncStatusAction(@"Preparing to remove deleted media files from the media library ... ");
            #endif

            // Remove files from the media library that have been deleted from the filesystem.
            try
            {
                if (args.Length > 1)
                {
                    if (args[1] == @"-norecursive")
                    {
                        SyncAction.SyncRemoveDeletedMediaFromLibrary(args[0]);
                    }
                    else
                    {
                        SyncAction.SyncRemoveDeletedMediaFromLibrary();
                    }
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  MainClass (" + resourceManager.GetString("application_name") + @".exe)
                        Method: Main()
                        Action: SyncRemoveDeletedMediaFromLibrary()
                                
                        " + exception.Message
                    , EventLogEntryType.Error, 123
                );
            }

            #if (DEBUG)
                deleteEnd = DateTime.Now.Subtract(deleteStart);
                
                Console.Write(" Done");
                
                addStart = DateTime.Now;
                
                Console.Clear();
                Console.WriteLine("Preparing to add new media files to the media library ... ");
                //SyncAction.SetSyncStatusAction(@"Preparing to add new media files to the media library ... ");

                Console.Clear();
                Console.Write("Getting a list of existing Audio files ...");
                //SyncAction.SetSyncStatusAction(@"Getting a list of existing Audio files ...");
            #endif

            // Get a list of all Audio file entries in the Media Library.
            DataTable audioFilesInMediaLibrary = SyncAction.SelectFromDatabaseTable("audio");

            #if (DEBUG)
                Console.Write(" Done");
                Console.Clear();
                Console.Write("Getting a list of existing Picture files ...");
                //SyncAction.SetSyncStatusAction(@"Getting a list of existing Picture files ...");
            #endif

            // Get a list of all Picture file entries in the Media Library.
            DataTable pictureFilesInMediaLibrary = SyncAction.SelectFromDatabaseTable("pictures");

            #if (DEBUG)
                Console.Write(" Done");
                Console.Clear();
                Console.Write("Getting a list of existing Video files ...");
                //SyncAction.SetSyncStatusAction(@"Getting a list of existing Video files ...");
            #endif

            // Get a list of all Video file entries in the Media Library.
            DataTable videoFilesInMediaLibrary = SyncAction.SelectFromDatabaseTable("videos");
            
            // Sync the specific directory path if a command line argument was provided.
            if (args.Length > 0)
            {
                if (args[0] == @"-UpdateMovieAndTvShows")
                {
                    // Get a list of all videos that should not have movie details.
                    DataTable disabledMovies = SyncAction.SelectFromDatabaseTable("disabled_movie_details");

                    // Get movie or tv show details for each video file in the media library.
                    foreach (DataRow tableRow in videoFilesInMediaLibrary.Rows)
                    {
                        // Only update media that does not already have any movie or tv details.
                        if (String.IsNullOrEmpty(tableRow["episode_details_title"].ToString()) && String.IsNullOrEmpty(tableRow["movie_details_name"].ToString()))
                        {
                            // Initialize disabled status to false.
                            bool mediaIsDisabled = false;

                            // Check if the current media file has been disabled for movie details.
                            foreach (DataRow tableRowDisabled in disabledMovies.Rows)
                            {
                                // If the file and path combination, or the entire path exists in the disabled table, set disabled status to true.
                                if (
                                    ((tableRow["filename"].ToString() == tableRowDisabled["filename"].ToString()) && (tableRow["path"].ToString() == tableRowDisabled["path"].ToString())) ||
                                    ((tableRow["path"].ToString().Contains(tableRowDisabled["path"].ToString())) && (String.IsNullOrEmpty(tableRowDisabled["filename"].ToString())))
                                ) {
                                    mediaIsDisabled = true;

                                    break;
                                }
                            }

                            // Continue with getting movie or tv details.
                            if (!mediaIsDisabled)
                            {
                                // Skip system directories.
                                if (
                                    !tableRow["path"].ToString().ToUpper().Contains(@".SVN") &&
                                    !tableRow["path"].ToString().ToUpper().Contains(@"$RECYCLE.BIN") &&
                                    !tableRow["path"].ToString().ToUpper().Contains(@"RECYCLER")
                                )
                                {
                                    #if (DEBUG)
                                        Console.Clear();
                                        Console.Write("Getting movie or tv show details for " + tableRow["filename"].ToString() + " ...");
                                    #endif

                                    SyncAction.SyncUpdateMediaInLibrary(String.Format(@"{0}\{1}.{2}", tableRow["path"].ToString(), tableRow["filename"].ToString(), tableRow["file_type"].ToString().ToLower()), tableRow["pkid"].ToString());
                                }
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        // Synchronize the specified directory path and all its sub-directories and files.
                        if (!String.IsNullOrEmpty(args[0]) && Directory.Exists(args[0]))
                        {
                            if ((args.Length > 1) && (args[1] == @"-norecursive"))
                            {
                                MainClass.SyncFilesInDirectory(args[0], audioFilesInMediaLibrary, pictureFilesInMediaLibrary, videoFilesInMediaLibrary, false);
                            }
                            else
                            {
                                MainClass.SyncFilesInDirectory(args[0], audioFilesInMediaLibrary, pictureFilesInMediaLibrary, videoFilesInMediaLibrary);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                            Class:  MainClass (" + resourceManager.GetString("application_name") + @".exe)
                            Method: Main()
                            Action: SyncFilesInDirectory(" + args[0] + @", audioFilesInMediaLibrary, pictureFilesInMediaLibrary, videoFilesInMediaLibrary);
                                
                            " + exception.Message
                                , EventLogEntryType.Error, 123
                        );
                    }
                }
            }
            // Otherwise sync all disk drivers and network shares if no command line arguments were provided.
            else
            {
                #if (DEBUG)
                    Console.Write(" Done");
                    Console.WriteLine("");
                    Console.Write("Getting a list of Disk Drives ...");
                    //SyncAction.SetSyncStatusAction(@"Getting a list of Disk Drives ...");
                #endif
                
                // Initialize a string array to hold the drive letters.
                string[] driveLetters = new string[] { };

                try
                {
                    // Get a list of all accessible disk drive letters.
                    driveLetters = Directory.GetLogicalDrives();
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                        Class:  MainClass (" + resourceManager.GetString("application_name") + @".exe)
                        Method: Main()
                        Action: Scanning Disk Drives
                                
                        " + exception.Message
                            , EventLogEntryType.Error, 123
                    );
                }

                #if (DEBUG)
                    Console.Write(" Done");
                #endif
                
                // Iterate all accessible disk drive letters.
                foreach (string driveLetter in driveLetters)
                {
                    try
                    {
                        // Synchronize the current disk drive and all its sub-directories and files.
                        MainClass.SyncFilesInDirectory(driveLetter, audioFilesInMediaLibrary, pictureFilesInMediaLibrary, videoFilesInMediaLibrary);
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                            Class:  MainClass (" + resourceManager.GetString("application_name") + @".exe)
                            Method: Main()
                            Action: Synchronizing Disk Drives
                                
                            " + exception.Message
                                , EventLogEntryType.Error, 123
                        );
                    }
                }
                
                #if (DEBUG)
                    Console.Clear();
                    Console.Write("Getting a list of Network Shares ...");
                    //SyncAction.SetSyncStatusAction(@"Getting a list of Network Shares ...");
                #endif

                // Initialize a string list to hold the network shares.
                List<string> networkShares = new List<string>() { };

                try
                {
                    // Get a list of all accessible SMB/Network shares.
                    networkShares = DirectoryManagement.GetWindowsNetworkShares();
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                            Class:  MainClass (" + resourceManager.GetString("application_name") + @".exe)
                            Method: Main()
                            Action: Scanning Network Shares
                                
                            " + exception.Message
                            , EventLogEntryType.Error, 123
                    );
                }

                #if (DEBUG)
                    Console.Write(" Done");
                #endif

                // Iterate all accessible SMB/Network shares.
                foreach (string networkShare in networkShares)
                {
                    try
                    {
                        // Synchronize the current network share and all its sub-directories and files.
                        MainClass.SyncFilesInDirectory(networkShare, audioFilesInMediaLibrary, pictureFilesInMediaLibrary, videoFilesInMediaLibrary);
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                            Class:  MainClass (" + resourceManager.GetString("application_name") + @".exe)
                            Method: Main()
                            Action: Synchronizing Network Shares
                                
                            " + exception.Message
                                , EventLogEntryType.Error, 123
                        );
                    }
                }
            }
            
            #if (DEBUG)
                addEnd       = DateTime.Now.Subtract(addStart);
                vidInfoStart = DateTime.Now;
                vidInfoEnd   = DateTime.Now.Subtract(vidInfoStart);
                
                // Get a list of all media files we have added to the media library.
                audioFilesInMediaLibrary   = SyncAction.SelectFromDatabaseTable("audio");
                pictureFilesInMediaLibrary = SyncAction.SelectFromDatabaseTable("pictures");
                videoFilesInMediaLibrary   = SyncAction.SelectFromDatabaseTable("videos");
                
                Console.Clear();
                Console.WriteLine(@"Audio Files Added: "   + audioFilesInMediaLibrary.Rows.Count.ToString());
                Console.WriteLine(@"Picture Files Added: " + pictureFilesInMediaLibrary.Rows.Count.ToString());
                Console.WriteLine(@"Videos Files Added: "  + videoFilesInMediaLibrary.Rows.Count.ToString());
                Console.WriteLine("");
                Console.WriteLine(@"Delete Time: "     + deleteEnd.ToString());
                Console.WriteLine(@"Insert Time: "     + addEnd.ToString());
                Console.WriteLine(@"Video Info Time: " + vidInfoEnd.ToString());
                Console.WriteLine("");
                Console.WriteLine(@"Total Time: " + DateTime.Now.Subtract(deleteStart).ToString());
                Console.WriteLine("");
                
                //Console.WriteLine("Press a key to exit.");
                //Console.Read();
            #endif

            // Set job status to completed.
            if (args.Length > 0)
            {
                SyncAction.SetSyncStatusAction(@"completed", args[0]);
            }
            else
            {
                SyncAction.SetSyncStatusAction(@"completed", @"FULL");
            }
        }
    }
}
