using FileSystem;
using Microsoft.Win32;
using SortingAlgorithms;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace VoyaMedia
{
    /// <summary>
    /// Interaction logic for MainApp.xaml
    /// </summary>
    public partial class MainApp : Application
    {
        // MenuLevel structure.
        public struct MenuLevel
        {
            public const int Home         = -4;
            public const int HomeTags     = -3;
            public const int HomeBrowse   = -2;
            public const int HomeSearch   = -1;
            public const int Browse       = 0;
            public const int SearchResult = 100;
            public const int TagsResult   = 200;
        }

        // List structure.
        public struct List
        {
            public const int MainMenu                   = 0;
            public const int MediaBrowser               = 1;
            public const int MediaAudioTracks           = 2;
            public const int MediaBrowserAlternative    = 3;
            public const int MediaBrowserRightClickMenu = 4;
            public const int MediaSubtitleTracks        = 5;
            public const int Tags                       = 6;
        }
        
        // Public Member Properties.
        public static bool            AudioPlaylistExited        = false;
        public static DispatcherTimer BackgroundTimer            = new DispatcherTimer();
        public static DispatcherTimer CurrentTimeTimer           = new DispatcherTimer();
        public static string          FileBrowserMediaType       = "";
        public static int             FileBrowserMenuLevel       = MenuLevel.Home;
        public static string          FileBrowserSearchString    = "";
        public static string          FileBrowserOldSearchString = "";
        public static double          LastMediaPlayerPosition    = 0;
        public static int             MediaPlayerHaltedCount     = 0;
        public static DateTime        LastMouseMove              = new DateTime();
        public static bool            LastMouseMoveTimerStarted  = false;
        public static ListView        MediaBrowserOldAudioList   = new ListView();
        public static bool            MediaIsPlaying             = false;
        public static string          MediaOldName               = "";
        public static bool            MediaPlayNext              = false;
        public static bool            MediaPlayRandom            = false;
        public static bool            MediaPicturesRandom        = false;
        public static bool            MovieTrailerIsPlaying      = false;
        public static int             OpenedMediaListIndex       = -1;
        public static ResourceManager ResourceManager            = new ResourceManager("VoyaMedia.Properties.Resources", Assembly.GetExecutingAssembly());
        public static DispatcherTimer SyncTimer                  = new DispatcherTimer();
        public static IntPtr          VlcPlayer                  = new IntPtr();
        public static double          WindowCurrentLeft          = 0;
        public static double          WindowCurrentTop           = 0;
        public static double          WindowCurrentWidth         = 0;
        public static double          WindowCurrentHeight        = 0;
        public static WindowState     WindowCurrentState         = WindowState.Maximized;
        public static WindowStyle     WindowCurrentStyle         = WindowStyle.ThreeDBorderWindow;

        /// <summary>
        /// Backs up the media library database files.
        /// </summary>
        public static void Backup()
        {
            // Create a Folder Browser Dialog that allows the user to select a backup path.
            System.Windows.Forms.FolderBrowserDialog backupDialog = new System.Windows.Forms.FolderBrowserDialog();

            // Open and display the dialog.
            backupDialog.Description = @"Select where you want to backup the Media Library";
            backupDialog.ShowDialog();

            // Check if the user cancelled or selected a valid path.
            if (!String.IsNullOrEmpty(backupDialog.SelectedPath) && Directory.Exists(backupDialog.SelectedPath))
            {
                // Backup the Media Library database file.
                File.Copy(
                    String.Format(@"{0}\data\MediaLibrary.sdf", DirectoryManagement.GetRuntimeExecutingPath()),
                    String.Format(@"{0}\MediaLibrary.sdf",      backupDialog.SelectedPath),
                    true
                );

                // Backup the Media Organizer database file.
                File.Copy(
                    String.Format(@"{0}\data\MediaOrganizer.sdf", DirectoryManagement.GetRuntimeExecutingPath()),
                    String.Format(@"{0}\MediaOrganizer.sdf",      backupDialog.SelectedPath),
                    true
                );
            }
        }

        /// <summary>
        /// Clears the local images downloaded for movies and tv shows, and resets the media library.
        /// </summary>
        public static void ClearCache()
        {
            // Start the AJAX loader.
            if (Process.GetProcessesByName(@"AjaxLoader").Length < 1)
            {
                ProcessManagement.StartHiddenShellProcess(@"AjaxLoader.exe");
            }

            // Get the current runtime executing path.
            string runtimePath = DirectoryManagement.GetRuntimeExecutingPath();

            // Get a list of Backdrop image files.
            string[] backdrops = Directory.GetFiles(String.Format(@"{0}\images\Backdrops", runtimePath));

            // Delete the backdrops.
            foreach (string backdrop in backdrops)
            {
                File.Delete(backdrop);
            }

            // Get a list of Cover image files.
            string[] covers = Directory.GetFiles(String.Format(@"{0}\images\Covers", runtimePath));

            // Delete the covers.
            foreach (string cover in covers)
            {
                File.Delete(cover);
            }

            // Get a list of Screenshot image files.
            string[] screenshots = Directory.GetFiles(String.Format(@"{0}\images\Screenshots", runtimePath));

            // Delete the screenshots.
            foreach (string screenshot in screenshots)
            {
                File.Delete(screenshot);
            }

            // Delete the Media Library files.
            File.Delete(String.Format(@"{0}\data\MediaLibrary.sdf",   DirectoryManagement.GetRuntimeExecutingPath()));
            File.Delete(String.Format(@"{0}\data\MediaOrganizer.sdf", DirectoryManagement.GetRuntimeExecutingPath()));

            // Start scanning the filesystems and re-create the Media Library.
            Thread threadSyncJob = new Thread(MainApp.SyncJobRun);
            threadSyncJob.Start();

            // Wait for the sync job to start before continuing.
            Thread.Sleep(100);

            // Close the AJAX loader.
            ProcessManagement.StartHiddenShellProcess(@"TASKKILL.EXE", @"/IM AjaxLoader.exe /F");
        }

        /// <summary>
        /// Returns the current time and date in a nicely formatted way.
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentTime()
        {
            // Example: Monday, 14 May 2012 9:38 PM
            return String.Format("{0:dddd}, {0:dd} {0:MMMM} {0:yyyy} {0:t}", DateTime.Now);
        }

        /// <summary>
        /// Sorts the inputted data table result set and removes duplicates.
        /// </summary>
        /// <param name="mediaFileResultSet"></param>
        /// <returns>Returns a dictionary containing the available media files in the selected media path.</returns>
        public static SortedDictionary<string, string> GetUniqueMenuFileItems(DataTable mediaFileResultSet)
        {
            // Reserve a dictionary of file items.
            SortedDictionary<string, string> uniqueMenuFileItems = new SortedDictionary<string, string>(new NaturalSort()) { };

            try
            {
                // Add menu items from the sql table to the unique menu item dictionary.
                foreach (DataRow tableRow in mediaFileResultSet.Rows)
                {
                    // Format the item key and value strings.
                    string itemKey   = @"FILENAME:" + tableRow["filename"].ToString() + @"." + tableRow["file_type"].ToString() + @":DETAILS:" + tableRow["pkid"].ToString() + @":SEARCH:" + tableRow["path"].ToString();
                    string itemValue = tableRow["filename"].ToString() + @"." + tableRow["file_type"].ToString();

                    // Skip duplicates.
                    if (!uniqueMenuFileItems.ContainsKey(itemKey))
                    {
                        // Make sure the file exists.
                        if (File.Exists(tableRow["path"].ToString() + @"\" + tableRow["filename"].ToString() + @"." + tableRow["file_type"].ToString()))
                        {
                            // Add all the files as media browser items.
                            if (mediaFileResultSet.Columns.Contains("completed") && (tableRow["completed"].ToString() != "0"))
                            {
                                // Add a checkmark to the left of each media item (audio/video) that has been completely played.
                                uniqueMenuFileItems.Add(itemKey, "\u2713 " + itemValue);
                            }
                            else
                            {
                                uniqueMenuFileItems.Add(itemKey, itemValue);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                    @"
                        Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                        Method: GetUniqueMenuFileItems(DataTable mediaFileResultSet)
                        
                        " + exception.Message,
                    EventLogEntryType.Error, 123);
            }

            // Return a uniquely sorted dictionary containing the menu items.
            return uniqueMenuFileItems;
        }

        /// <summary>
        /// Sorts the inputted data table result set and removes duplicates.
        /// </summary>
        /// <param name="mediaPathsResultSet"></param>
        /// <returns>Returns a dictionary containing the available media paths.</returns>
        //public static SortedDictionary<string, string> GetUniqueMenuPathItems(DataTable mediaPathsResultSet)
        public static SortedDictionary<string, string> GetUniqueMenuPathItems()
        {
            // A dictionary of unique menu item strings.
            SortedDictionary<string, string> uniqueMenuItems = new SortedDictionary<string, string>(new NaturalSort()) { };

            try
            {
                // Get a list of all accessible disk drive letters.
                string[] driveLetters = Directory.GetLogicalDrives();

                // Get a list of all accessible SMB/Network shares.
                List<string> networkShares = DirectoryManagement.GetWindowsNetworkShares();

                // List drive letters and network shares.
                if (MainApp.FileBrowserMenuLevel == MainApp.MenuLevel.Browse)
                {
                    foreach (string driveLetter in driveLetters)
                    {
                        try
                        {
                            // Only list drive letters that are accessible.
                            if ((Directory.GetDirectories(driveLetter).Length > 0) || (Directory.GetFiles(driveLetter).Length > 0))
                            {
                                if (!uniqueMenuItems.ContainsKey(driveLetter))
                                {
                                    uniqueMenuItems.Add(driveLetter, driveLetter);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                                @"
                                    Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                                    Method: GetUniqueMenuPathItems(DataTable mediaPathsResultSet)
                                    Action: // Only list drive letters that are accessible.
                        
                                    " + exception.Message,
                                EventLogEntryType.Error, 123);
                        }
                    }

                    foreach (string networkShare in networkShares)
                    {
                        try
                        {
                            // Only list network shares that are accessible.
                            if ((Directory.GetDirectories(networkShare).Length > 0) || (Directory.GetFiles(networkShare).Length > 0))
                            {
                                if (!uniqueMenuItems.ContainsKey(networkShare))
                                {
                                    uniqueMenuItems.Add(networkShare, networkShare);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                                @"
                                    Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                                    Method: GetUniqueMenuPathItems(DataTable mediaPathsResultSet)
                                    Action: // Only list network shares that are accessible.
                        
                                    " + exception.Message,
                                EventLogEntryType.Error, 123);
                        }
                    }
                }
                // List sub-directories.
                else if (MainApp.FileBrowserSearchString.Contains(@":") || MainApp.FileBrowserSearchString.Contains(@"\\"))
                {
                    string[] subDirectories = System.IO.Directory.GetDirectories(MainApp.FileBrowserSearchString, @"*", SearchOption.TopDirectoryOnly);

                    foreach (string subDirectory in subDirectories)
                    {
                        try
                        {
                            // Only list sub-directories that are accessible.
                            if (
                                !subDirectory.ToUpper().Contains(@".SVN") &&
                                !subDirectory.ToUpper().Contains(@"$RECYCLE.BIN") &&
                                !subDirectory.ToUpper().Contains(@"RECYCLER")
                            )
                            {
                                if ((Directory.GetDirectories(subDirectory).Length > 0) || (Directory.GetFiles(subDirectory).Length > 0))
                                {
                                    if (!uniqueMenuItems.ContainsKey(subDirectory))
                                    {
                                        uniqueMenuItems.Add(subDirectory, subDirectory.Substring(subDirectory.LastIndexOf(@"\") + 1));
                                    }
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                                @"
                                    Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                                    Method: GetUniqueMenuPathItems(DataTable mediaPathsResultSet)
                                    Action: // Only list sub-directories that are accessible.
                        
                                    " + exception.Message,
                                EventLogEntryType.Error, 123);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                    @"
                        Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                        Method: GetUniqueMenuPathItems(DataTable mediaPathsResultSet)
                        
                        " + exception.Message,
                    EventLogEntryType.Error, 123);
            }
            
            // Return a uniquely sorted dictionary containing the menu items.
            return uniqueMenuItems;
        }

        /// <summary>
        /// Imports media library database files that have been backed up.
        /// </summary>
        public static void Import()
        {
            // Create an Open File Dialog that allows the user to select the library file to import.
            OpenFileDialog importDialog = new OpenFileDialog();

            // Set dialog properties.
            importDialog.CheckFileExists = true;
            importDialog.CheckPathExists = true;
            importDialog.Filter          = @"Voya|MediaLibrary.sdf";
            importDialog.Multiselect     = false;
            importDialog.Title           = @"Import Voya Media Library Database File";
            importDialog.ValidateNames   = true;

            // Open and display the dialog.
            importDialog.ShowDialog();

            // Check if the user cancelled or selected a valid file.
            if (!String.IsNullOrEmpty(importDialog.FileName) && File.Exists(importDialog.FileName))
            {
                // Import the Media Library database file.
                File.Copy(
                    importDialog.FileName,
                    String.Format(@"{0}\data\MediaLibrary.sdf", DirectoryManagement.GetRuntimeExecutingPath()),
                    true
                );

                // Import the Media Organizer database file.
                File.Copy(
                    importDialog.FileName.Replace(@"MediaLibrary.sdf", @"MediaOrganizer.sdf"),
                    String.Format(@"{0}\data\MediaOrganizer.sdf", DirectoryManagement.GetRuntimeExecutingPath()),
                    true
                );
            }
        }

        /// <summary>
        /// Runs the media library synchronizer.
        /// </summary>
        public static void SyncJobRun()
        {
            // Main variables.
            string[]     driveLetters  = new string[] { };
            List<string> networkShares = new List<string>() { };

            if (Process.GetProcessesByName(@"SyncMediaLibrary").Length < 1)
            {
                try
                {
                    // Get a list of all accessible disk drive letters.
                    driveLetters = Directory.GetLogicalDrives();
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                        @"
                        Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                        Method: SyncJobRun()
                        Action: Scanning Disk Drives
                        
                        " + exception.Message,
                        EventLogEntryType.Error,
                        123
                    );
                }

                // Iterate all accessible disk drive letters (in the format "C:\").
                foreach (string driveLetter in driveLetters)
                {
                    try
                    {
                        // Synchronize the current disk drive and all its sub-directories and files.
                        // The Argument is passed in a shell environment so we need to escape slashes and quote the string:
                        // Example: C:\ => "C:\\"
                        ProcessManagement.StartHiddenShellProcess(@"SyncMediaLibrary.exe", @"""" + driveLetter.Replace(@"\", @"\\") + @"""");

                        // Let the CPU rest for one second before continuing to the next iteration.
                        Thread.Sleep(1000);
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                            @"
                            Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                            Method: SyncJobRun()
                            Action: Synchronizing Disk Drives
                            
                            " + exception.Message,
                            EventLogEntryType.Error,
                            123
                        );
                    }
                }

                try
                {
                    // Get a list of all accessible SMB/Network shares.
                    networkShares = DirectoryManagement.GetWindowsNetworkShares();
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                        @"
                        Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                        Method: SyncJobRun()
                        Action: Scanning Network Shares
                        
                        " + exception.Message,
                        EventLogEntryType.Error,
                        123
                    );
                }

                // Iterate all accessible SMB/Network shares.
                foreach (string networkShare in networkShares)
                {
                    try
                    {
                        // Synchronize the current network share and all its sub-directories and files.
                        ProcessManagement.StartHiddenShellProcess(@"SyncMediaLibrary.exe", @"""" + networkShare.Replace(@"\", @"\\") + @"""");

                        // Let the CPU rest for one second before continuing to the next iteration.
                        Thread.Sleep(1000);
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                            @"
                            Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                            Method: SyncJobRun()
                            Action: Synchronizing Network Shares
                            
                            " + exception.Message,
                            EventLogEntryType.Error,
                            123
                        );
                    }
                }

                /*
                try
                {
                    // Get movie or tv show details for each video file in the media library.
                    ProcessManagement.StartHiddenShellProcess(@"SyncMediaLibrary.exe", @"-UpdateMovieAndTvShows");
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                        @"
                            Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                            Method: SyncJobRun()
                            Action: Getting Movie and TV Show details.
                        
                            " + exception.Message,
                        EventLogEntryType.Error,
                        123
                    );
                }
                */
            }
        }
        
    }
}
