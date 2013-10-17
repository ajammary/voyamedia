using FileSystem;
using MediaInfoApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VoyaMedia
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Public properties.
        /// </summary>
        public static MainWindow Instance;

        /// <summary>
        /// Main Executable Method (VoyaMedia.exe).
        /// </summary>
        public MainWindow()
        {   
            // Create a Windows Event Log Source.
            if (!EventLog.SourceExists(MainApp.ResourceManager.GetString("application_title")))
            {
                EventLog.CreateEventSource(MainApp.ResourceManager.GetString("application_title"), "Application");
            }

            try
            {
                // Render the default WPF/XAML Window and all controls.
                InitializeComponent();
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                    @"
                        Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                        Method: MainWindow()
                        Action: InitializeComponent()
                        
                        " + exception.Message,
                    EventLogEntryType.Error, 123);
            }
            
            // Assign this instantiated window to a publicly available static member.
            MainWindow.Instance = this;
            
            // Register Event Handlers.
            this.RegisterEventHandlers();
            
            // Render the Main Menu.
            MainMenu.Instance.mainMenu_border.Visibility = Visibility.Visible;
            MainMenu.Instance.mainMenu_listView.Focus();
            MainMenu.Instance.mainMenu_listView.SelectedIndex = -1;

            // Write the Application Revision number to the top status bar area.
            this.RenderRevision();
            
            // Prepare and start the Background Image Timer.
            MainApp.BackgroundTimer.Tick    += new EventHandler(backgroundTimer_Tick);
            MainApp.BackgroundTimer.Interval = new TimeSpan(0, 0, 60);
            MainApp.BackgroundTimer.Start();
            
            // Write the initial instance of the current time to the top left status bar area.
            this.RenderCurrentTime();
            
            // Prepare and start the Current Time Timer.
            MainApp.CurrentTimeTimer.Tick    += new EventHandler(currentTimeTimer_Tick);
            MainApp.CurrentTimeTimer.Interval = new TimeSpan(0, 0, 1);
            MainApp.CurrentTimeTimer.Start();
            
            // Prepare and start the Sync Timer.
            MainApp.SyncTimer.Tick    += new EventHandler(syncTimer_Tick);
            MainApp.SyncTimer.Interval = new TimeSpan(0, 0, 3600);
            MainApp.SyncTimer.Start();
            
            // Wait for the sync job to start before continuing.
            //Thread.Sleep(100);

            // Show a welcome message if this is the first time the user has opened the application.
            string initFile = MainApp.ResourceManager.GetString("init_file");

            if (File.Exists(initFile))
            {
                MessageBox.Show(
                    MainApp.ResourceManager.GetString("welcome_message").Replace("\\n", Environment.NewLine),
                    MainApp.ResourceManager.GetString("application_title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Delete the init file.
                File.Delete(initFile);

                // Run the initial instance of the Media Library Synchronizer.
                Thread threadSyncJob = new Thread(MainApp.SyncJobRun);
                threadSyncJob.Start();

                // Wait for the sync job to start before continuing.
                //Thread.Sleep(100);
            }

            // Run the updater in the background.
            this.Update(true);

            // Render the top status bar.
            StatusTop.Instance.RenderStatusTop();
        }

        /// <summary>
        /// The Background Image Timer method changes the background image and re-renders the media browser.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="events"></param>
        private void backgroundTimer_Tick(object sender, EventArgs events)
        {
            // Change and render the background image.
            //this.RenderBackgroundImage();

            // Update and Render the Media Browser.
            if (MediaBrowser.Instance.mediaBrowser_listView.IsFocused)
            {
                MediaBrowser.Instance.RenderMediaBrowser();
            }

            // Update and render the top status bar.
            StatusTop.Instance.RenderStatusTop();
        }

        /// <summary>
        /// The Current Time Timer method writes the current time to the top left status bar.
        /// It also updates the top status bar with sync action messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="events"></param>
        private void currentTimeTimer_Tick(object sender, EventArgs events)
        {
            // Write the current time to the top-left corner area.
            this.RenderCurrentTime();

            // Render/Display the media playback progress.
            if (MainApp.MediaIsPlaying)
            {
                MediaPlayer.Instance.RenderMediaPlaybackProgress();
            }

            // Hide the mouse cursor when the video is in fullscreen mode.
            if (
                (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Video) &&
                (MediaPlayer.Instance.playMedia_border.Visibility     == Visibility.Visible) &&
                (MediaPlayer.Instance.mediaTitle_textBlock.Visibility == Visibility.Collapsed)
            ) {
                // Check if the mouse cursor is above this application window.
                if (this.IsMouseOver)
                {
                    // Save the current time to set the time when the mouse was first moved.
                    if (MainApp.LastMouseMoveTimerStarted)
                    {
                        MainApp.LastMouseMove.AddTicks(DateTime.Now.Ticks);
                    }

                    // Hide the mouse cursor if it has been inactive for the last 5 seconds.
                    if (DateTime.Now.Subtract(MainApp.LastMouseMove).TotalSeconds > 5)
                    {
                        MainApp.LastMouseMoveTimerStarted = false;

                        Mouse.OverrideCursor = Cursors.None;
                    }
                }
            }
        }
        
        /// <summary>
        /// The Sync Timer method runs the media library synchronizer periodically.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="events"></param>
        void syncTimer_Tick(object sender, EventArgs events)
        {
            //Thread threadSyncJobRun = new Thread(MainApp.SyncJobRun);
            //threadSyncJobRun.Start();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowMain_window_Closed(object sender, EventArgs e)
        {
            // Close the Ajax Loader.
            ProcessManagement.StartHiddenShellProcess(@"TASKKILL.EXE", @"/IM AjaxLoader.exe /F");

            // Close Voya Media.
            this.Close();
        }

        /// <summary>
        /// Triggered when the user presses a keyboard key.
        /// </summary>
        /// <param name="sender">The source where the action was triggered.</param>
        /// <param name="keyEvents">Details about the key that was pressed.</param>
        private void WindowMain_window_KeyDown(object sender, KeyEventArgs keyEvents)
        {
            // List of valid alpha characters.
            List<string> alphaCharacters = new List<string>()
                {
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
            };

            // List of valid numeric characters.
            List<string> numericCharacters = new List<string>()
            {
                "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9",
                "NumPad0", "NumPad1", "NumPad2", "NumPad3", "NumPad4", "NumPad5", "NumPad6", "NumPad7", "NumPad8", "NumPad9"
            };

            // Check if the user pressed the SPACE key.
            if (keyEvents.Key == Key.Space)
            {
                // If the media is currently playing, toggle play/pause.
                if ((MediaPlayer.Instance.playMedia_border.Visibility == Visibility.Visible) && (MainApp.FileBrowserMediaType != MediaInfo.MediaType.Picture))
                {
                    MediaPlayer.Instance.ToggleVideoPlayPause();
                }
                // Otherwise write a space character to a text input field.
                else if (MediaPlayer.Instance.mediaTag_stackPanel.Visibility == Visibility.Visible)
                {
                    MediaPlayer.Instance.mediaTag_textBox.Text += " ";
                }
                else
                {
                    // Write the space character to the search input box.
                    SearchPanel.Instance.KeyboardEnterSpaceInSearchInputBox();
                }
            }
            // Check if the user pressed the ESCAPE or BACKSPACE key.
            else if ((keyEvents.Key == Key.Escape) || (keyEvents.Key == Key.Back))
            {
                // If the media tag text box is visible, trim away the last character written.
                if ((MediaPlayer.Instance.mediaTag_stackPanel.Visibility == Visibility.Visible) && (keyEvents.Key == Key.Back))
                {
                    // Make sure the string has at least one character.
                    if (MediaPlayer.Instance.mediaTag_textBox.Text.Length > 0)
                    {
                        // Trim away the last character of the string.
                        MediaPlayer.Instance.mediaTag_textBox.Text = MediaPlayer.Instance.mediaTag_textBox.Text.Substring(0, (MediaPlayer.Instance.mediaTag_textBox.Text.Length - 1));
                    }
                }
                // If the current window is the search screen, 
                // delete the last character written to the search input box.
                else if ((MainApp.FileBrowserMenuLevel == MainApp.MenuLevel.HomeSearch) && (keyEvents.Key == Key.Back))
                {
                    SearchPanel.Instance.KeyboardDeleteCharacterFromSearchInputBox();
                }
                // If the media browser right-click menu is visible.
                else if (MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_border.Visibility == Visibility.Visible)
                {
                    // Hide/Collapse the media browser right-click menu.
                    MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_border.Visibility = Visibility.Collapsed;

                    // Focus the media browser.
                    MediaBrowser.Instance.MediaBrowserFocus();
                }
                // If the media browser rename menu is visible.
                else if (MediaBrowserRename.Instance.mediaBrowserRename_border.Visibility == Visibility.Visible)
                {
                    // Hide/Collapse the media browser rename menu.
                    MediaBrowserRename.Instance.mediaBrowserRename_border.Visibility = Visibility.Collapsed;

                    // Focus the media browser.
                    MediaBrowser.Instance.MediaBrowserFocus();
                }
                // If the alternative movie details list is visible.
                else if (MediaBrowserAlternativeMovieDetailsMenu.Instance.mediaBrowserAlternative_border.Visibility == Visibility.Visible)
                {
                    // Hide/Collapse the alternative movie details list.
                    MediaBrowserAlternativeMovieDetailsMenu.Instance.mediaBrowserAlternative_border.Visibility = Visibility.Collapsed;

                    // Focus the media browser.
                    MediaBrowser.Instance.MediaBrowserFocus();
                }
                // If the current window is the media player and the media is in fullscreen mode, return to normal mode.
                else if (
                    (this.WindowStyle == WindowStyle.None) ||
                    ((MediaPlayer.Instance.playMedia_border.Visibility == Visibility.Visible) && (MediaPlayer.Instance.mediaTitle_textBlock.Visibility == Visibility.Collapsed))
                )
                {
                    MediaPlayer.Instance.MediaPlayerFullScreenExit();
                }
                // If the current window is the media player, stop playing the media and return back to the media browser/search results.
                else if (MediaPlayer.Instance.playMedia_border.Visibility == Visibility.Visible)
                {
                    MediaPlayer.Instance.MediaPlayerClose();
                }
                // If the current window is the media browser, and the user is browsing directories, navigate back one level.
                else if ((MainApp.FileBrowserMenuLevel > MainApp.MenuLevel.Browse) && (MainApp.FileBrowserMenuLevel < MainApp.MenuLevel.SearchResult))
                {
                    MediaBrowser.Instance.MediaBrowserNavigateBack();
                    MediaBrowser.Instance.RenderMediaBrowser();
                }
                // Otherwise navigate home.
                else
                {
                    // Set the new image brush object as the main window background image.
                    this.RenderBackgroundImage(@"background_003.jpg");

                    // Navigate home.
                    MediaBrowser.Instance.MediaBrowserNavigateHome();
                }
            }
            // Check if the user pressed the PLAY or PAUSE button on their remote control.
            else if (keyEvents.Key == Key.MediaPlayPause)
            {
                if ((MediaPlayer.Instance.playMedia_border.Visibility == Visibility.Visible) && (MainApp.FileBrowserMediaType != MediaInfo.MediaType.Picture))
                {
                    // Pause the media.
                    if (MainApp.MediaIsPlaying)
                    {
                        MediaPlayer.Instance.MediaPlayerPause();

                        // Exit fullscreen mode.
                        if (
                            (MediaPlayer.Instance.playMedia_border.Visibility     == Visibility.Visible) &&
                            (MediaPlayer.Instance.mediaTitle_textBlock.Visibility == Visibility.Collapsed) &&
                            (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Video)
                        )
                        {
                            MediaPlayer.Instance.MediaPlayerFullScreenExit();
                        }
                    }
                    else
                    {
                        // Play the media.
                        MediaPlayer.Instance.MediaPlayerPlay();

                        // Enter fullscreen mode.
                        if (
                            (MediaPlayer.Instance.playMedia_border.Visibility     == Visibility.Visible) &&
                            (MediaPlayer.Instance.mediaTitle_textBlock.Visibility == Visibility.Visible) &&
                            (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Video)
                        )
                        {
                            MediaPlayer.Instance.MediaPlayerFullScreen();
                        }
                    }
                }
                else if (MainApp.AudioPlaylistExited && (StatusTop.Instance.mediaControlButtonsBackgroundAudio_stackPanel.Visibility == Visibility.Visible))
                {
                    // Pause the media.
                    if (MainApp.MediaIsPlaying)
                    {
                        MediaPlayer.Instance.MediaPlayerPause();
                    }
                    else
                    {
                        // Play the media.
                        MediaPlayer.Instance.MediaPlayerPlay();
                    }
                }
            }
            // Check if the user pressed the STOP button on their remote control.
            else if (keyEvents.Key == Key.MediaStop)
            {
                if ((MediaPlayer.Instance.playMedia_border.Visibility == Visibility.Visible) && (MainApp.FileBrowserMediaType != MediaInfo.MediaType.Picture))
                {
                    // Stop the media.
                    if (MainApp.MediaIsPlaying)
                    {
                        MediaPlayer.Instance.MediaPlayerStop();
                    }

                    // Exit fullscreen mode.
                    if (
                        (MediaPlayer.Instance.playMedia_border.Visibility     == Visibility.Visible) &&
                        (MediaPlayer.Instance.mediaTitle_textBlock.Visibility == Visibility.Collapsed) &&
                        (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Video)
                    )
                    {
                        MediaPlayer.Instance.MediaPlayerFullScreenExit();
                    }
                }
                else if (MainApp.AudioPlaylistExited && (StatusTop.Instance.mediaControlButtonsBackgroundAudio_stackPanel.Visibility == Visibility.Visible))
                {
                    // Stop the media.
                    if (MainApp.MediaIsPlaying)
                    {
                        MediaPlayer.Instance.MediaPlayerStop();
                    }
                }
            }
            // Check if the user pressed the MEDIA NEXT button on their remote control.
            else if (keyEvents.Key == Key.MediaNextTrack)
            {
                if (MainApp.AudioPlaylistExited && (StatusTop.Instance.mediaControlButtonsBackgroundAudio_stackPanel.Visibility == Visibility.Visible))
                {
                    MediaPlayer.Instance.MediaPlayerNext(true);
                }
                else if (MediaPlayer.Instance.playMedia_border.Visibility == System.Windows.Visibility.Visible)
                {
                    MediaPlayer.Instance.MediaPlayerNext();
                }
            }
            // Check if the user pressed the MEDIA PREVIOUS button on their remote control.
            else if (keyEvents.Key == Key.MediaPreviousTrack)
            {
                MediaPlayer.Instance.MediaPlayerPrevious();
            }
            // Check if the user typed in an alphabetic character on their keyboard.
            else if (alphaCharacters.Contains(keyEvents.Key.ToString()))
            {
                // Write the character to the search input box.
                if (keyEvents.Key.ToString().Length == 1)
                {
                    if (MediaPlayer.Instance.playMedia_border.Visibility == Visibility.Visible)
                    {
                        MediaPlayer.Instance.mediaTag_textBox.Text += keyEvents.Key.ToString().ToLower();
                    }
                    else
                    {
                        SearchPanel.Instance.KeyboardEnterKeyInSearchInputBox(Convert.ToChar(keyEvents.Key.ToString().ToLower()), "Return");
                    }
                }
            }
            // Check if the user typed in an numerical character on their keyboard.
            else if (numericCharacters.Contains(keyEvents.Key.ToString()))
            {
                // Write the numerical character to the search input box.
                if (keyEvents.Key.ToString().Length == 2)
                {
                    if (MediaPlayer.Instance.mediaTag_stackPanel.Visibility == Visibility.Visible)
                    {
                        MediaPlayer.Instance.mediaTag_textBox.Text += keyEvents.Key.ToString().ToLower().Substring(1, 1);
                    }
                    else
                    {
                        SearchPanel.Instance.KeyboardEnterKeyInSearchInputBox(Convert.ToChar(keyEvents.Key.ToString().ToLower().Substring(1, 1)), "Return");
                    }
                }
                // Write the numerical character entered using the NumPad to the search input box.
                else if (keyEvents.Key.ToString().Length == 7)
                {
                    if (MediaPlayer.Instance.mediaTag_stackPanel.Visibility == Visibility.Visible)
                    {
                        MediaPlayer.Instance.mediaTag_textBox.Text += keyEvents.Key.ToString().ToLower().Substring(6, 1);
                    }
                    else
                    {
                        SearchPanel.Instance.KeyboardEnterKeyInSearchInputBox(Convert.ToChar(keyEvents.Key.ToString().ToLower().Substring(6, 1)), "Return");
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowMain_window_MouseEnter(object sender, MouseEventArgs e)
        {
            // Hide the mouse cursor when the video is in fullscreen mode.
            if (
                (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Video) &&
                (MediaPlayer.Instance.playMedia_border.Visibility     == Visibility.Visible) &&
                (MediaPlayer.Instance.mediaTitle_textBlock.Visibility == Visibility.Collapsed)
            )
            {
                // Save the current time to set the time when the mouse was first moved.
                if (MainApp.LastMouseMoveTimerStarted)
                {
                    MainApp.LastMouseMove.AddTicks(DateTime.Now.Ticks);
                }

                // Hide the mouse cursor if it has been inactive for the last 5 seconds.
                if (DateTime.Now.Subtract(MainApp.LastMouseMove).TotalSeconds > 5)
                {
                    MainApp.LastMouseMoveTimerStarted = false;

                    Mouse.OverrideCursor = Cursors.None;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowMain_window_MouseLeave(object sender, MouseEventArgs e)
        {
            // Reset and display the mouse cursor.
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        /// <summary>
        /// Handles double-clicking the application window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArguments"></param>
        private void WindowMain_window_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs eventArguments)
        {
            // Check if the media player is open.
            if (
                (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Video) &&
                (MediaPlayer.Instance.playMedia_border.Visibility == Visibility.Visible)
            )
            {
                // Enter Fullscreen mode if the current mode is normal.
                if (MediaPlayer.Instance.mediaTitle_textBlock.Visibility == Visibility.Visible)
                {
                    MediaPlayer.Instance.MediaPlayerFullScreen();

                    eventArguments.Handled = true;
                }
                // Return to normal mode if the current mode is fullscreen.
                else
                {
                    MediaPlayer.Instance.MediaPlayerFullScreenExit();

                    eventArguments.Handled = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArguments"></param>
        private void WindowMain_window_PreviewMouseMove(object sender, MouseEventArgs eventArguments)
        {
            // Hide the mouse cursor when the video is in fullscreen mode.
            if (
                (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Video) &&
                (MediaPlayer.Instance.playMedia_border.Visibility     == Visibility.Visible) &&
                (MediaPlayer.Instance.mediaTitle_textBlock.Visibility == Visibility.Collapsed)
            )
            {
                // Check if the mouse cursor is above this application window.
                if (this.IsMouseOver)
                {
                    // Save the current time to set the time when the mouse was first moved.
                    MainApp.LastMouseMoveTimerStarted = true;

                    MainApp.LastMouseMove = DateTime.Now;

                    // Display the mouse cursor.
                    Mouse.OverrideCursor = Cursors.Arrow;

                    eventArguments.Handled = true;
                }
            }
        }

        /// <summary>
        /// Triggered when the window is resized by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArguments"></param>
        private void WindowMain_window_SizeChanged(object sender, SizeChangedEventArgs eventArguments)
        {
            this.WindowResizeHandle();
        }

        /// <summary>
        /// Triggered when the window changes its state between maximized, minimized and normal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArguments"></param>
        private void WindowMain_window_StateChanged(object sender, EventArgs eventArguments)
        {
            this.WindowResizeHandle();
        }
        
        /// <summary>
        /// Builds and returns an Image object based on a bitmap image.
        /// </summary>
        /// <param name="imageFile"></param>
        /// <returns></returns>
        public Image BuildBitmapImage(string imageFile)
        {
            // Create an image source object.
            BitmapImage imageSource = new BitmapImage();

            imageSource.BeginInit();
            imageSource.UriSource = new Uri(@"/" + MainApp.ResourceManager.GetString("application_name") + @";component/Images/" + imageFile, UriKind.Relative);
            imageSource.EndInit();

            // Create an image brush object.
            Image image  = new Image();
            image.Source = imageSource;

            return image;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listItemIndex"></param>
        /// <param name="listName"></param>
        public void HighlightListItem(int listItemIndex = -1, int listName = -1)
        {
            // Main variables.
            ListViewItem   highlightedItem = new ListViewItem();
            ItemCollection listItems;

            try
            {
                // Make sure the input arguments are set.
                if ((listItemIndex != -1) && (listName != -1))
                {
                    // Select the list.
                    switch (listName)
                    {
                        case MainApp.List.MainMenu:
                            listItems = MainMenu.Instance.mainMenu_listView.Items;

                            break;
                        case MainApp.List.MediaBrowser:
                            listItems = MediaBrowser.Instance.mediaBrowser_listView.Items;

                            break;
                        case MainApp.List.MediaAudioTracks:
                            listItems = MediaPlayer.Instance.mediaAudioTracks_listView.Items;

                            break;
                        case MainApp.List.MediaBrowserAlternative:
                            listItems = MediaBrowserAlternativeMovieDetailsMenu.Instance.mediaBrowserAlternative_listView.Items;

                            break;
                        case MainApp.List.MediaBrowserRightClickMenu:
                            listItems = MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView.Items;

                            break;
                        case MainApp.List.MediaSubtitleTracks:
                            listItems = MediaPlayer.Instance.mediaSubtitleTracks_listView.Items;

                            break;
                        case MainApp.List.Tags:
                            listItems = MediaTags.Instance.tags_listView.Items;

                            break;
                        default:
                            listItems = null;

                            break;
                    }

                    // Make sure a valid list has been selected.
                    if (listItems != null)
                    {
                        // Select the highligted list item.
                        highlightedItem = (ListViewItem) listItems[listItemIndex];

                        // Reset the layout of the list items.
                        foreach (ListViewItem listItem in listItems)
                        {
                            listItem.BorderThickness = new Thickness(0);
                            listItem.BorderBrush     = null;
                        }

                        // Set the background and text color of highlighted/selected items in the list.
                        Resources[SystemColors.HighlightBrushKey]     = new SolidColorBrush(Colors.Black);
                        Resources[SystemColors.HighlightTextBrushKey] = new SolidColorBrush(Colors.White);
                        highlightedItem.BorderThickness               = new Thickness(0, 2, 0, 2);
                        highlightedItem.BorderBrush                   = new SolidColorBrush(Colors.Blue);
                    }
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                    @"
                    Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                    Method: MainMenuHighlightItem(" + listItemIndex.ToString() + @")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123
                );
            }
        }
                
        /// <summary>
        /// Changes and Renders the Background Image.
        /// </summary>
        public void RenderBackgroundImage(string imageFile)
        {
            // Create an image source object.
            BitmapImage imageSource = new BitmapImage();
            
            imageSource.BeginInit();
            imageSource.UriSource = new Uri(@"pack://application:,,,/" + MainApp.ResourceManager.GetString("application_name") + @";component/Images/" + imageFile);
            imageSource.EndInit();

            // Convert the image source to an image brush object.
            ImageBrush imageBrush = new ImageBrush(imageSource);

            // Set the new image brush object as the main window background image.
            WindowMain_window.Background = imageBrush;
        }

        /// <summary>
        /// Writes the current time and date to the top left status bar area.
        /// </summary>
        private void RenderCurrentTime()
        {
            // Example: Monday, 14 May 2012 9:38 PM
            StatusTop.Instance.statusTopTimeDate_textBlock.Text = MainApp.GetCurrentTime();
        }
        
        /// <summary>
        /// Writes the Application Revision number to the top status bar area.
        /// </summary>
        private void RenderRevision()
        {
            try
            {
                // Set the current revision text-file path.
                string revisionFile = String.Format(@"{0}\doc\{1}", DirectoryManagement.GetRuntimeExecutingPath(), MainApp.ResourceManager.GetString("revision_file"));

                // Get the Application Revision number.
                if (File.Exists(revisionFile))
                {
                    // Write the Revision number to the top status bar area.
                    StatusTop.Instance.statusTopRevision_textBlock.Text = 
                        MainApp.ResourceManager.GetString("application_title") + 
                        @" v." + 
                        File.ReadAllText(revisionFile).Trim();
                }
                else
                {
                    StatusTop.Instance.statusTopRevision_textBlock.Text = "Voya Media v. 1.1.111.111";
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                    @"
                        Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                        Method: RenderRevision()
                        
                        " + exception.Message,
                    EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Checks for newer versions of Voya Media.
        /// </summary>
        /// <param name="runInBackground"></param>
        public void Update(bool runInBackground = false)
        {
            // Main variables.
            string   availableUpdatesWeb    = "";
            string   availableUpdatesFile   = "";
            string   availableUpdatesText   = "";
            string[] availableUpdates       = new string[] {};
            string   currentRevision        = "";
            int      currentRevisionInteger = 0;
            string   latestRevision         = "";
            int      latestRevisionInteger  = 0;
            string   latestUpdateFile       = "";
            string   revisionFile           = "";
            string[] updateFiles            = new string[] {};

            // Start the AJAX loader.
            if (Process.GetProcessesByName(@"AjaxLoader").Length < 1)
            {
                ProcessManagement.StartHiddenShellProcess(@"AjaxLoader.exe");
            }
            
            // Set the current revision text-file path.
            revisionFile = String.Format(@"{0}\doc\{1}", DirectoryManagement.GetRuntimeExecutingPath(), MainApp.ResourceManager.GetString("revision_file"));
            
            // Get the current revision.
            if (File.Exists(revisionFile))
            {
                currentRevision = File.ReadAllText(revisionFile).Trim();

                // Strip away non-numerical characters from the revision.
                currentRevision = currentRevision.Replace(@".", "");

                try
                {
                    // Convert the revision number from a string to an integer.
                    if (!String.IsNullOrEmpty(currentRevision))
                    {
                        currentRevisionInteger = Convert.ToInt32(currentRevision);
                    }
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                        @"
                        Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                        Method: Update()
                        Action: currentRevisionInteger = Convert.ToInt32(" + currentRevision + @");
                        
                        " + exception.Message
                        , EventLogEntryType.Error, 123
                    );
                }
            }

            // Set the available updates text-file URL.
            availableUpdatesWeb = String.Format(@"{0}/{1}", MainApp.ResourceManager.GetString("update_url"), MainApp.ResourceManager.GetString("update_file"));

            // Set the local available updates text-file path.
            availableUpdatesFile = String.Format(@"{0}\doc\{1}", DirectoryManagement.GetRuntimeExecutingPath(), MainApp.ResourceManager.GetString("update_file"));

            try
            {
                // Delete the text file if it already exists.
                if (File.Exists(availableUpdatesFile))
                {
                    File.Delete(availableUpdatesFile);
                }

                // Download the text file to the local filesystem.
                WebClient webClient = new WebClient();
                webClient.DownloadFile(availableUpdatesWeb, availableUpdatesFile);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                    @"
                        Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                        Method: Update()
                        Action: webClient.DownloadFile(""" + availableUpdatesWeb + @""", """ + availableUpdatesFile + @""")
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            if (File.Exists(availableUpdatesFile))
            {
                // Read the contents of the available updates text file, and trim away unnecessary whitespaces and linebreaks.
                availableUpdatesText = File.ReadAllText(availableUpdatesFile).Trim();
                
                // Split the line to get the revision number and the update link.
                string[] updateRevisionAndUrl = availableUpdatesText.Split(';');

                // Get the revision number.
                latestRevision = updateRevisionAndUrl[0];

                // Get the update link.
                latestUpdateFile = updateRevisionAndUrl[1];

                // Strip away non-numerical characters from the revision number.
                latestRevision = latestRevision.Replace(@".", "");
                
                try
                {
                    // Convert the revision number from a string to an integer.
                    if (!String.IsNullOrEmpty(latestRevision))
                    {
                        latestRevisionInteger = Convert.ToInt32(latestRevision);
                    }
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(MainApp.ResourceManager.GetString("application_title"),
                        @"
                        Class:  MainWindow (" + MainApp.ResourceManager.GetString("application_name") + @".exe)
                        Method: Update()
                        Action: latestRevisionInteger = Convert.ToInt32(" + latestRevision + @");
                        
                        " + exception.Message
                        , EventLogEntryType.Error, 123
                    );
                }

                // Close the AJAX loader.
                ProcessManagement.StartHiddenShellProcess(@"TASKKILL.EXE", @"/IM AjaxLoader.exe /F");

                // Open the update file executable if a newer version is found.
                if (latestRevisionInteger > currentRevisionInteger)
                {
                    if (!String.IsNullOrEmpty(latestUpdateFile) && latestUpdateFile.Contains(@".exe"))
                    {
                        ProcessManagement.StartHiddenShellProcess(latestUpdateFile);

                        // Focus the Main Menu.
                        MainMenu.Instance.mainMenu_listView.Focus();
                    }
                }
                else
                {
                    // Hide the messagebox if the updater is running in the background.
                    if (!runInBackground)
                    {
                        MessageBox.Show(MainApp.ResourceManager.GetString("latest_revision_message"));
                    }
                }

                // Delete the local text file.
                File.Delete(availableUpdatesFile);
            }
            else
            {
                // Close the AJAX loader.
                ProcessManagement.StartHiddenShellProcess(@"TASKKILL.EXE", @"/IM AjaxLoader.exe /F");

                // Hide the messagebox if the updater is running in the background.
                if (!runInBackground)
                {
                    MessageBox.Show(MainApp.ResourceManager.GetString("failed_update_message"), @"Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Resizes the window controls based on the new window size.
        /// </summary>
        public void WindowResizeHandle()
        {
            // Resize the Media Browser list.
            //if ((this.WindowState == WindowState.Maximized) && (!this.fullScreenExit))
            if (this.WindowState == WindowState.Maximized)
            {
                MediaBrowser.Instance.mediaBrowser_border.Height = (double) (SystemParameters.PrimaryScreenHeight - 200);
            }
            else
            {
                MediaBrowser.Instance.mediaBrowser_border.Height = (double) (this.Height - 200);
            }

            // Resize the Media Details area when the window is maximized.
            //if ((this.WindowState == WindowState.Maximized) && (!this.fullScreenExit))
            if (this.WindowState == WindowState.Maximized)
            {
                // Resize the entire media details outer borders.
                MediaDetails.Instance.mediaDetailsArea_border.Width          = (double) (SystemParameters.PrimaryScreenWidth  - 650);
                MediaDetails.Instance.mediaDetailsArea_border.Height         = (double) (SystemParameters.PrimaryScreenHeight - 200);
                MediaDetails.Instance.mediaDetailsPicturesArea_border.Width  = (double) (SystemParameters.PrimaryScreenWidth  - 650);
                MediaDetails.Instance.mediaDetailsPicturesArea_border.Height = (double) (SystemParameters.PrimaryScreenHeight - 200);

                // Resize the bottom media details area for tv shows.
                if (MediaDetails.Instance.tvEpisodeSummary_textBlock.Visibility == Visibility.Visible)
                {
                    MediaDetails.Instance.mediaDetails_stackPanel.Height    = 590;
                    MediaDetails.Instance.tvEpisodeSummary_textBlock.Height = (double) (MediaDetails.Instance.mediaDetails_stackPanel.Height - 250);
                    MediaDetails.Instance.tvDirector_textBlock.Width        = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvDirector_label.Width) - 80);
                    MediaDetails.Instance.tvWriters_textBlock.Width         = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvWriters_label.Width) - 80);
                    MediaDetails.Instance.tvCast_textBlock.Width            = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvCast_label.Width) - 80);
                    MediaDetails.Instance.tvGenre_textBlock.Width           = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvGenre_label.Width) - 80);
                    MediaDetails.Instance.tvEpisode_textBlock.Width         = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvEpisode_label.Width) - 80);
                    MediaDetails.Instance.tvNextEpisode_textBlock.Width     = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvNextEpisode_label.Width) - 80);
                }
                // Resize the bottom media details area for movies and other videos.
                else if (MediaDetails.Instance.movieSummary_textBlock.Visibility == Visibility.Visible)
                {
                    MediaDetails.Instance.mediaDetails_stackPanel.Height = 470;
                    MediaDetails.Instance.movieSummary_textBlock.Height  = MediaDetails.Instance.mediaDetails_stackPanel.Height;
                    MediaDetails.Instance.mediaScreenshot_image.Height   = (double) (MediaDetails.Instance.movieSummary_textBlock.Height - 10);
                    MediaDetails.Instance.movieDirector_textBlock.Width  = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - (MediaDetails.Instance.mediatechDetails_image.Width + MediaDetails.Instance.movieDirector_label.Width)) - 80);
                    MediaDetails.Instance.movieWriters_textBlock.Width   = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - (MediaDetails.Instance.mediatechDetails_image.Width + MediaDetails.Instance.movieWriters_label.Width)) - 80);
                    MediaDetails.Instance.movieCast_textBlock.Width      = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - (MediaDetails.Instance.mediatechDetails_image.Width + MediaDetails.Instance.movieCast_label.Width)) - 80);
                    MediaDetails.Instance.movieGenre_textBlock.Width     = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - (MediaDetails.Instance.mediatechDetails_image.Width + MediaDetails.Instance.movieGenre_label.Width)) - 80);
                }

                // Resize the media tech details area.
                MediaDetails.Instance.mediaTechDetails_stackPanel.Width = 880;
                MediaDetails.Instance.duration_textBlock.Width          = (double) (MediaDetails.Instance.mediaTechDetails_stackPanel.Width - 120);
                MediaDetails.Instance.file_size_textBlock.Width         = (double) (MediaDetails.Instance.mediaTechDetails_stackPanel.Width - 120);
                MediaDetails.Instance.filename_textBlock.Width          = (double) (MediaDetails.Instance.mediaTechDetails_stackPanel.Width - 120);
                MediaDetails.Instance.path_textBlock.Width              = (double) (MediaDetails.Instance.mediaTechDetails_stackPanel.Width - 120);
                MediaDetails.Instance.movieName_textBlock.Width         = MediaDetails.Instance.mediaTechDetails_stackPanel.Width;

                // Resize the media picture details area.
                MediaDetails.Instance.mediaDetailsPicturesTitlePath_textBlock.Width = (double) (MediaDetails.Instance.mediaDetailsPicturesArea_border.Width  - 50);
                MediaDetails.Instance.mediaDetailsPicturesTitleFile_textBlock.Width = (double) (MediaDetails.Instance.mediaDetailsPicturesArea_border.Width  - 50);
                MediaDetails.Instance.mediaDetailsPictures_image.Width              = (double) (MediaDetails.Instance.mediaDetailsPicturesArea_border.Width  - 50);
                MediaDetails.Instance.mediaDetailsPictures_image.Height             = (double) (MediaDetails.Instance.mediaDetailsPicturesArea_border.Height - 150);
            }
            // Resize the Media Details area when the window is resized by dragging the edges.
            else
            {
                // Resize the entire media details outer borders.
                MediaDetails.Instance.mediaDetailsArea_border.Width          = (double) (this.Width  - 650);
                MediaDetails.Instance.mediaDetailsArea_border.Height         = (double) (this.Height - 200);
                MediaDetails.Instance.mediaDetailsPicturesArea_border.Width  = (double) (this.Width  - 650);
                MediaDetails.Instance.mediaDetailsPicturesArea_border.Height = (double) (this.Height - 200);

                // Resize the bottom media details area for tv shows.
                if (MediaDetails.Instance.tvEpisodeSummary_textBlock.Visibility == Visibility.Visible)
                {
                    MediaDetails.Instance.mediaDetails_stackPanel.Height    = (double)(MediaDetails.Instance.mediaDetailsArea_border.Height - 290);
                    MediaDetails.Instance.tvEpisodeSummary_textBlock.Height = (double) (MediaDetails.Instance.mediaDetails_stackPanel.Height - 250);
                    MediaDetails.Instance.tvDirector_textBlock.Width        = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvDirector_label.Width) - 80);
                    MediaDetails.Instance.tvWriters_textBlock.Width         = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvWriters_label.Width) - 80);
                    MediaDetails.Instance.tvCast_textBlock.Width            = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvCast_label.Width) - 80);
                    MediaDetails.Instance.tvGenre_textBlock.Width           = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvGenre_label.Width) - 80);
                    MediaDetails.Instance.tvEpisode_textBlock.Width         = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvEpisode_label.Width) - 80);
                    MediaDetails.Instance.tvNextEpisode_textBlock.Width     = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - MediaDetails.Instance.tvNextEpisode_label.Width) - 80);
                }
                // Resize the bottom media details area for movies and other videos.
                else if (MediaDetails.Instance.movieSummary_textBlock.Visibility == Visibility.Visible)
                {
                    MediaDetails.Instance.mediaDetails_stackPanel.Height = (double)(MediaDetails.Instance.mediaDetailsArea_border.Height - 410);
                    MediaDetails.Instance.movieSummary_textBlock.Height  = MediaDetails.Instance.mediaDetails_stackPanel.Height;
                    MediaDetails.Instance.mediaScreenshot_image.Height   = (double) (MediaDetails.Instance.movieSummary_textBlock.Height - 10);
                    MediaDetails.Instance.movieDirector_textBlock.Width  = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - (MediaDetails.Instance.mediatechDetails_image.Width + MediaDetails.Instance.movieDirector_label.Width)) - 80);
                    MediaDetails.Instance.movieWriters_textBlock.Width   = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - (MediaDetails.Instance.mediatechDetails_image.Width + MediaDetails.Instance.movieWriters_label.Width)) - 80);
                    MediaDetails.Instance.movieCast_textBlock.Width      = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - (MediaDetails.Instance.mediatechDetails_image.Width + MediaDetails.Instance.movieCast_label.Width)) - 80);
                    MediaDetails.Instance.movieGenre_textBlock.Width     = (double)((MediaDetails.Instance.mediaDetailsArea_border.Width - (MediaDetails.Instance.mediatechDetails_image.Width + MediaDetails.Instance.movieGenre_label.Width)) - 80);
                }

                // Resize the media tech details area.
                MediaDetails.Instance.mediaTechDetails_stackPanel.Width = (double)(MediaDetails.Instance.mediaDetailsArea_border.Width - (MediaDetails.Instance.mediatechDetails_image.Width + 70));
                MediaDetails.Instance.duration_textBlock.Width          = (double)(MediaDetails.Instance.mediaTechDetails_stackPanel.Width - 120);
                MediaDetails.Instance.file_size_textBlock.Width         = (double)(MediaDetails.Instance.mediaTechDetails_stackPanel.Width - 120);
                MediaDetails.Instance.filename_textBlock.Width          = (double)(MediaDetails.Instance.mediaTechDetails_stackPanel.Width - 120);
                MediaDetails.Instance.path_textBlock.Width              = (double)(MediaDetails.Instance.mediaTechDetails_stackPanel.Width - 120);
                MediaDetails.Instance.movieName_textBlock.Width         = MediaDetails.Instance.mediaTechDetails_stackPanel.Width;

                // Resize the media picture details area.
                MediaDetails.Instance.mediaDetailsPicturesTitlePath_textBlock.Width = (double) (MediaDetails.Instance.mediaDetailsPicturesArea_border.Width  - 50);
                MediaDetails.Instance.mediaDetailsPicturesTitleFile_textBlock.Width = (double) (MediaDetails.Instance.mediaDetailsPicturesArea_border.Width  - 50);
                MediaDetails.Instance.mediaDetailsPictures_image.Width              = (double) (MediaDetails.Instance.mediaDetailsPicturesArea_border.Width  - 50);
                MediaDetails.Instance.mediaDetailsPictures_image.Height             = (double) (MediaDetails.Instance.mediaDetailsPicturesArea_border.Height - 150);

                #if (DEBUG)
                    //MainApp.statusTop_textBlock.Text  = mediatechDetails_image.Width.ToString();
                    //MainApp.statusTop_textBlock.Text += " : " + mediaTechDetails_stackPanel.GetValue(Canvas.LeftProperty).ToString();
                    //MainApp.statusTop_textBlock.Text += " : " + mediaTechDetails_stackPanel.Width;
                    //MainApp.statusTop_textBlock.Text += " : " + filename_textBlock.Width;
                #endif
            }

            // Resize the window when the Media Player is in fullscreen mode.
            if (
                (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Video) &&
                (MediaPlayer.Instance.playMedia_border.Visibility     == Visibility.Visible) &&
                (MediaPlayer.Instance.mediaTitle_textBlock.Visibility == Visibility.Collapsed) //&& 
                //(!this.fullScreenExit)
            )
            {
                MediaPlayer.Instance.MediaPlayerFullScreen();
            }
            // Resize the window when the Media Player is in normal mode.
            else if ((MediaPlayer.Instance.playMedia_border.Visibility == Visibility.Visible) && (MediaPlayer.Instance.mediaTitle_textBlock.Visibility == Visibility.Visible))
            {
                // Resize when the window is maximized.
                if (this.WindowState == WindowState.Maximized)
                {
                    // Resize the Media Player area.
                    MediaPlayer.Instance.playMedia_border.Width  = SystemParameters.PrimaryScreenWidth;
                    MediaPlayer.Instance.playMedia_border.Height = SystemParameters.PrimaryScreenHeight;

                    // Resize the media header title area.
                    if (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Picture)
                    {
                        MediaPlayer.Instance.mediaTitle_textBlock.Width = (double) (SystemParameters.PrimaryScreenWidth - 50);
                    }
                    else
                    {
                        MediaPlayer.Instance.mediaTitle_textBlock.Width = SystemParameters.PrimaryScreenWidth;
                    }

                    // Resize the audio media image cover size.
                    if (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Audio)
                    {
                        MediaPlayer.Instance.mediaPlayer_image.Width  = (double) (SystemParameters.PrimaryScreenWidth  - 200);
                        MediaPlayer.Instance.mediaPlayer_image.Height = (double) (SystemParameters.PrimaryScreenHeight - 400);
                    }
                    // Resize the picture image size.
                    else if (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Picture)
                    {
                        MediaPlayer.Instance.mediaPlayer_image.Width  = (double) (SystemParameters.PrimaryScreenWidth  - 50);
                        MediaPlayer.Instance.mediaPlayer_image.Height = (double) (SystemParameters.PrimaryScreenHeight - 300);
                    }
                    // Resize the vlc media player size.
                    else if (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Video)
                    {
                        MediaPlayer.Instance.vlcMediaPlayer_grid.Width  = (double) (SystemParameters.PrimaryScreenWidth  - 200);
                        MediaPlayer.Instance.vlcMediaPlayer_grid.Height = (double) (SystemParameters.PrimaryScreenHeight - 400);
                    }
                }
                // Resize when the window is resized by dragging the edges.
                else
                {
                    // Resize the Media Player area.
                    MediaPlayer.Instance.playMedia_border.Width  = this.Width;
                    MediaPlayer.Instance.playMedia_border.Height = this.Height;

                    // Resize the media header title area.
                    if (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Picture)
                    {
                        MediaPlayer.Instance.mediaTitle_textBlock.Width = (double) (this.Width - 50);
                    }
                    else
                    {
                        MediaPlayer.Instance.mediaTitle_textBlock.Width = this.Width;
                    }

                    // Resize the audio media image cover size.
                    if (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Audio)
                    {
                        MediaPlayer.Instance.mediaPlayer_image.Width  = (double) (this.Width  - 200);
                        MediaPlayer.Instance.mediaPlayer_image.Height = (double) (this.Height - 400);
                    }
                    // Resize the picture image size.
                    else if (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Picture)
                    {
                        MediaPlayer.Instance.mediaPlayer_image.Width  = (double) (this.Width  - 50);
                        MediaPlayer.Instance.mediaPlayer_image.Height = (double) (this.Height - 300);
                    }
                    // Resize the vlc media player size.
                    else if (MainApp.FileBrowserMediaType == MediaInfo.MediaType.Video)
                    {
                        MediaPlayer.Instance.vlcMediaPlayer_grid.Width  = (double) (this.Width  - 200);
                        MediaPlayer.Instance.vlcMediaPlayer_grid.Height = (double) (this.Height - 400);
                    }
                }
            }
        }

        /// <summary>
        /// Registers Event Handlers for the Media Player Controls.
        /// </summary>
        private void RegisterEventHandlersMediaPlayerControls()
        {
            // Register Event Handlers for the Media Player Controls.
            MediaPlayer.Instance.mediaMute_button.PreviewKeyDown   += MediaPlayer.Instance.mediaMute_button_PreviewKeyDown;
            MediaPlayer.Instance.mediaMute_button.PreviewMouseDown += MediaPlayer.Instance.mediaMute_button_PreviewMouseDown;

            MediaPlayer.Instance.mediaNext_button.Click          += MediaPlayer.Instance.mediaNext_button_Click;
            MediaPlayer.Instance.mediaNext_button.PreviewKeyDown += MediaPlayer.Instance.mediaNext_button_PreviewKeyDown;

            MediaPlayer.Instance.mediaPause_button.PreviewKeyDown   += MediaPlayer.Instance.mediaPause_button_PreviewKeyDown;
            MediaPlayer.Instance.mediaPause_button.PreviewMouseDown += MediaPlayer.Instance.mediaPause_button_PreviewMouseDown;

            MediaPlayer.Instance.mediaPlay_button.PreviewKeyDown   += MediaPlayer.Instance.mediaPlay_button_PreviewKeyDown;
            MediaPlayer.Instance.mediaPlay_button.PreviewMouseDown += MediaPlayer.Instance.mediaPlay_button_PreviewMouseDown;

            MediaPlayer.Instance.mediaStop_button.PreviewKeyDown   += MediaPlayer.Instance.mediaStop_button_PreviewKeyDown;
            MediaPlayer.Instance.mediaStop_button.PreviewMouseDown += MediaPlayer.Instance.mediaStop_button_PreviewMouseDown;

            MediaPlayer.Instance.mediaRandomDisable_button.PreviewKeyDown   += MediaPlayer.Instance.mediaRandomDisable_button_PreviewKeyDown;
            MediaPlayer.Instance.mediaRandomDisable_button.PreviewMouseDown += MediaPlayer.Instance.mediaRandomDisable_button_PreviewMouseDown;

            MediaPlayer.Instance.mediaRandomEnable_button.PreviewKeyDown   += MediaPlayer.Instance.mediaRandomEnable_button_PreviewKeyDown;
            MediaPlayer.Instance.mediaRandomEnable_button.PreviewMouseDown += MediaPlayer.Instance.mediaRandomEnable_button_PreviewMouseDown;

            MediaPlayer.Instance.mediaUnMute_button.PreviewKeyDown   += MediaPlayer.Instance.mediaUnMute_button_PreviewKeyDown;
            MediaPlayer.Instance.mediaUnMute_button.PreviewMouseDown += MediaPlayer.Instance.mediaUnMute_button_PreviewMouseDown;
        }

        /// <summary>
        /// Registers Event Handlers for the Media Player (Background Audio) Controls.
        /// </summary>
        private void RegisterEventHandlersMediaPlayerBackgroundAudioControls()
        {
            // Register Event Handlers for the Media Player (Background Audio) Controls.
            StatusTop.Instance.mediaMuteBackgroundAudio_button.PreviewKeyDown   += MediaPlayer.Instance.mediaMute_button_PreviewKeyDown;
            StatusTop.Instance.mediaMuteBackgroundAudio_button.PreviewMouseDown += MediaPlayer.Instance.mediaMute_button_PreviewMouseDown;

            StatusTop.Instance.mediaNextBackgroundAudio_button.Click          += MediaPlayer.Instance.mediaNext_button_Click;
            StatusTop.Instance.mediaNextBackgroundAudio_button.PreviewKeyDown += MediaPlayer.Instance.mediaNext_button_PreviewKeyDown;

            StatusTop.Instance.mediaPauseBackgroundAudio_button.PreviewKeyDown   += MediaPlayer.Instance.mediaPause_button_PreviewKeyDown;
            StatusTop.Instance.mediaPauseBackgroundAudio_button.PreviewMouseDown += MediaPlayer.Instance.mediaPause_button_PreviewMouseDown;

            StatusTop.Instance.mediaPlayBackgroundAudio_button.PreviewKeyDown   += MediaPlayer.Instance.mediaPlay_button_PreviewKeyDown;
            StatusTop.Instance.mediaPlayBackgroundAudio_button.PreviewMouseDown += MediaPlayer.Instance.mediaPlay_button_PreviewMouseDown;

            StatusTop.Instance.mediaStopBackgroundAudio_button.PreviewKeyDown   += MediaPlayer.Instance.mediaStop_button_PreviewKeyDown;
            StatusTop.Instance.mediaStopBackgroundAudio_button.PreviewMouseDown += MediaPlayer.Instance.mediaStop_button_PreviewMouseDown;

            StatusTop.Instance.mediaRandomDisableBackgroundAudio_button.PreviewKeyDown   += MediaPlayer.Instance.mediaRandomDisable_button_PreviewKeyDown;
            StatusTop.Instance.mediaRandomDisableBackgroundAudio_button.PreviewMouseDown += MediaPlayer.Instance.mediaRandomDisable_button_PreviewMouseDown;

            StatusTop.Instance.mediaRandomEnableBackgroundAudio_button.PreviewKeyDown   += MediaPlayer.Instance.mediaRandomEnable_button_PreviewKeyDown;
            StatusTop.Instance.mediaRandomEnableBackgroundAudio_button.PreviewMouseDown += MediaPlayer.Instance.mediaRandomEnable_button_PreviewMouseDown;

            StatusTop.Instance.mediaUnMuteBackgroundAudio_button.PreviewKeyDown   += MediaPlayer.Instance.mediaUnMute_button_PreviewKeyDown;
            StatusTop.Instance.mediaUnMuteBackgroundAudio_button.PreviewMouseDown += MediaPlayer.Instance.mediaUnMute_button_PreviewMouseDown;
        }

        /// <summary>
        /// Registers Event Handlers for the Main Menu.
        /// </summary>
        private void RegisterEventHandlersMainMenu()
        {
            // Register Event Handlers for the Main Menu.
            MainMenu.Instance.mainMenu_listView.SelectionChanged                 += MainMenu.Instance.mainMenu_listView_SelectionChanged;
            
            MainMenu.Instance.mainMenu_Audio_listViewItem.KeyDown                += MainMenu.Instance.mainMenu_Audio_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Audio_listViewItem.MouseLeftButtonUp      += MainMenu.Instance.mainMenu_Audio_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Audio_listViewItem.MouseEnter             += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Backup_listViewItem.KeyDown               += MainMenu.Instance.mainMenu_Backup_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Backup_listViewItem.MouseLeftButtonUp     += MainMenu.Instance.mainMenu_Backup_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Backup_listViewItem.MouseEnter            += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Browse_listViewItem.KeyDown               += MainMenu.Instance.mainMenu_Browse_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Browse_listViewItem.MouseLeftButtonUp     += MainMenu.Instance.mainMenu_Browse_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Browse_listViewItem.MouseEnter            += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_ClearCache_listViewItem.KeyDown           += MainMenu.Instance.mainMenu_ClearCache_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_ClearCache_listViewItem.MouseLeftButtonUp += MainMenu.Instance.mainMenu_ClearCache_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_ClearCache_listViewItem.MouseEnter        += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Exit_listViewItem.KeyDown                 += MainMenu.Instance.mainMenu_Exit_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Exit_listViewItem.MouseLeftButtonUp       += MainMenu.Instance.mainMenu_Exit_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Exit_listViewItem.MouseEnter              += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Home_listViewItem.KeyDown                 += MainMenu.Instance.mainMenu_Home_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Home_listViewItem.MouseLeftButtonUp       += MainMenu.Instance.mainMenu_Home_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Home_listViewItem.MouseEnter              += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Import_listViewItem.KeyDown               += MainMenu.Instance.mainMenu_Import_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Import_listViewItem.MouseLeftButtonUp     += MainMenu.Instance.mainMenu_Import_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Import_listViewItem.MouseEnter            += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Pictures_listViewItem.KeyDown             += MainMenu.Instance.mainMenu_Pictures_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Pictures_listViewItem.MouseLeftButtonUp   += MainMenu.Instance.mainMenu_Pictures_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Pictures_listViewItem.MouseEnter          += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Search_listViewItem.KeyDown               += MainMenu.Instance.mainMenu_Search_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Search_listViewItem.MouseLeftButtonUp     += MainMenu.Instance.mainMenu_Search_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Search_listViewItem.MouseEnter            += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Settings_listViewItem.KeyDown             += MainMenu.Instance.mainMenu_Settings_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Settings_listViewItem.MouseLeftButtonUp   += MainMenu.Instance.mainMenu_Settings_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Settings_listViewItem.MouseEnter          += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Tags_listViewItem.KeyDown                 += MainMenu.Instance.mainMenu_Tags_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Tags_listViewItem.MouseLeftButtonUp       += MainMenu.Instance.mainMenu_Tags_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Tags_listViewItem.MouseEnter              += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Update_listViewItem.KeyDown               += MainMenu.Instance.mainMenu_Update_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Update_listViewItem.MouseLeftButtonUp     += MainMenu.Instance.mainMenu_Update_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Update_listViewItem.MouseEnter            += MainMenu.Instance.mainMenu_listView_MouseEnter;
            
            MainMenu.Instance.mainMenu_Video_listViewItem.KeyDown                += MainMenu.Instance.mainMenu_Video_listViewItem_KeyDown;
            MainMenu.Instance.mainMenu_Video_listViewItem.MouseLeftButtonUp      += MainMenu.Instance.mainMenu_Video_listViewItem_MouseLeftButtonUp;
            MainMenu.Instance.mainMenu_Video_listViewItem.MouseEnter             += MainMenu.Instance.mainMenu_listView_MouseEnter;
        }

        /// <summary>
        /// Registers Event Handlers for the Search Panel.
        /// </summary>
        private void RegisterEventHandlersSearchPanel()
        {
            // Register Event Handlers for the Search Panel.
            SearchPanel.Instance.keyboardAudioSearch_button.PreviewKeyDown    += SearchPanel.Instance.keyboardAudioSearch_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardAudioSearch_button.PreviewMouseUp    += SearchPanel.Instance.keyboardAudioSearch_button_PreviewMouseDown;
            
            SearchPanel.Instance.keyboardDEL_button.PreviewMouseDown          += SearchPanel.Instance.keyboardDEL_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardDEL_button.PreviewKeyDown            += SearchPanel.Instance.keyboardDEL_button_PreviewKeyDown;

            SearchPanel.Instance.keyboardHome_button.PreviewKeyDown           += SearchPanel.Instance.keyboardHome_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardHome_button.PreviewMouseUp           += SearchPanel.Instance.keyboardHome_button_PreviewMouseUp;
           
            SearchPanel.Instance.keyboardPicturesSearch_button.PreviewKeyDown += SearchPanel.Instance.keyboardPicturesSearch_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardPicturesSearch_button.PreviewMouseUp += SearchPanel.Instance.keyboardPicturesSearch_button_PreviewMouseDown;
            
            SearchPanel.Instance.keyboardSPACE_button.PreviewMouseDown        += SearchPanel.Instance.keyboardSPACE_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardSPACE_button.PreviewKeyDown          += SearchPanel.Instance.keyboardSPACE_button_PreviewKeyDown;
            
            SearchPanel.Instance.keyboardVideosSearch_button.PreviewKeyDown   += SearchPanel.Instance.keyboardVideosSearch_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardVideosSearch_button.PreviewMouseUp   += SearchPanel.Instance.keyboardVideosSearch_button_PreviewMouseDown;

            SearchPanel.Instance.keyboardA_button.PreviewMouseDown += SearchPanel.Instance.keyboardA_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardA_button.PreviewKeyDown   += SearchPanel.Instance.keyboardA_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardB_button.PreviewMouseDown += SearchPanel.Instance.keyboardB_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardB_button.PreviewKeyDown   += SearchPanel.Instance.keyboardB_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardC_button.PreviewMouseDown += SearchPanel.Instance.keyboardC_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardC_button.PreviewKeyDown   += SearchPanel.Instance.keyboardC_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardD_button.PreviewMouseDown += SearchPanel.Instance.keyboardD_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardD_button.PreviewKeyDown   += SearchPanel.Instance.keyboardD_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardE_button.PreviewMouseDown += SearchPanel.Instance.keyboardE_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardE_button.PreviewKeyDown   += SearchPanel.Instance.keyboardE_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardF_button.PreviewMouseDown += SearchPanel.Instance.keyboardF_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardF_button.PreviewKeyDown   += SearchPanel.Instance.keyboardF_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardG_button.PreviewMouseDown += SearchPanel.Instance.keyboardG_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardG_button.PreviewKeyDown   += SearchPanel.Instance.keyboardG_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardH_button.PreviewMouseDown += SearchPanel.Instance.keyboardH_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardH_button.PreviewKeyDown   += SearchPanel.Instance.keyboardH_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardI_button.PreviewMouseDown += SearchPanel.Instance.keyboardI_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardI_button.PreviewKeyDown   += SearchPanel.Instance.keyboardI_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardJ_button.PreviewMouseDown += SearchPanel.Instance.keyboardJ_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardJ_button.PreviewKeyDown   += SearchPanel.Instance.keyboardJ_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardK_button.PreviewMouseDown += SearchPanel.Instance.keyboardK_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardK_button.PreviewKeyDown   += SearchPanel.Instance.keyboardK_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardL_button.PreviewMouseDown += SearchPanel.Instance.keyboardL_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardL_button.PreviewKeyDown   += SearchPanel.Instance.keyboardL_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardM_button.PreviewMouseDown += SearchPanel.Instance.keyboardM_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardM_button.PreviewKeyDown   += SearchPanel.Instance.keyboardM_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardN_button.PreviewMouseDown += SearchPanel.Instance.keyboardN_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardN_button.PreviewKeyDown   += SearchPanel.Instance.keyboardN_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardO_button.PreviewMouseDown += SearchPanel.Instance.keyboardO_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardO_button.PreviewKeyDown   += SearchPanel.Instance.keyboardO_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardP_button.PreviewMouseDown += SearchPanel.Instance.keyboardP_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardP_button.PreviewKeyDown   += SearchPanel.Instance.keyboardP_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardQ_button.PreviewMouseDown += SearchPanel.Instance.keyboardQ_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardQ_button.PreviewKeyDown   += SearchPanel.Instance.keyboardQ_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardR_button.PreviewMouseDown += SearchPanel.Instance.keyboardR_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardR_button.PreviewKeyDown   += SearchPanel.Instance.keyboardR_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardS_button.PreviewMouseDown += SearchPanel.Instance.keyboardS_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardS_button.PreviewKeyDown   += SearchPanel.Instance.keyboardS_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardT_button.PreviewMouseDown += SearchPanel.Instance.keyboardT_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardT_button.PreviewKeyDown   += SearchPanel.Instance.keyboardT_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardU_button.PreviewMouseDown += SearchPanel.Instance.keyboardU_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardU_button.PreviewKeyDown   += SearchPanel.Instance.keyboardU_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardV_button.PreviewMouseDown += SearchPanel.Instance.keyboardV_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardV_button.PreviewKeyDown   += SearchPanel.Instance.keyboardV_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardW_button.PreviewMouseDown += SearchPanel.Instance.keyboardW_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardW_button.PreviewKeyDown   += SearchPanel.Instance.keyboardW_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardX_button.PreviewMouseDown += SearchPanel.Instance.keyboardX_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardX_button.PreviewKeyDown   += SearchPanel.Instance.keyboardX_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardY_button.PreviewMouseDown += SearchPanel.Instance.keyboardY_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardY_button.PreviewKeyDown   += SearchPanel.Instance.keyboardY_button_PreviewKeyDown;
            SearchPanel.Instance.keyboardZ_button.PreviewMouseDown += SearchPanel.Instance.keyboardZ_button_PreviewMouseDown;
            SearchPanel.Instance.keyboardZ_button.PreviewKeyDown   += SearchPanel.Instance.keyboardZ_button_PreviewKeyDown;

            SearchPanel.Instance.keyboard0_button.PreviewMouseDown += SearchPanel.Instance.keyboard0_button_PreviewMouseDown;
            SearchPanel.Instance.keyboard0_button.PreviewKeyDown   += SearchPanel.Instance.keyboard0_button_PreviewKeyDown;
            SearchPanel.Instance.keyboard1_button.PreviewMouseDown += SearchPanel.Instance.keyboard1_button_PreviewMouseDown;
            SearchPanel.Instance.keyboard1_button.PreviewKeyDown   += SearchPanel.Instance.keyboard1_button_PreviewKeyDown;
            SearchPanel.Instance.keyboard2_button.PreviewMouseDown += SearchPanel.Instance.keyboard2_button_PreviewMouseDown;
            SearchPanel.Instance.keyboard2_button.PreviewKeyDown   += SearchPanel.Instance.keyboard2_button_PreviewKeyDown;
            SearchPanel.Instance.keyboard3_button.PreviewMouseDown += SearchPanel.Instance.keyboard3_button_PreviewMouseDown;
            SearchPanel.Instance.keyboard3_button.PreviewKeyDown   += SearchPanel.Instance.keyboard3_button_PreviewKeyDown;
            SearchPanel.Instance.keyboard4_button.PreviewMouseDown += SearchPanel.Instance.keyboard4_button_PreviewMouseDown;
            SearchPanel.Instance.keyboard4_button.PreviewKeyDown   += SearchPanel.Instance.keyboard4_button_PreviewKeyDown;
            SearchPanel.Instance.keyboard5_button.PreviewMouseDown += SearchPanel.Instance.keyboard5_button_PreviewMouseDown;
            SearchPanel.Instance.keyboard5_button.PreviewKeyDown   += SearchPanel.Instance.keyboard5_button_PreviewKeyDown;
            SearchPanel.Instance.keyboard6_button.PreviewMouseDown += SearchPanel.Instance.keyboard6_button_PreviewMouseDown;
            SearchPanel.Instance.keyboard6_button.PreviewKeyDown   += SearchPanel.Instance.keyboard6_button_PreviewKeyDown;
            SearchPanel.Instance.keyboard7_button.PreviewMouseDown += SearchPanel.Instance.keyboard7_button_PreviewMouseDown;
            SearchPanel.Instance.keyboard7_button.PreviewKeyDown   += SearchPanel.Instance.keyboard7_button_PreviewKeyDown;
            SearchPanel.Instance.keyboard8_button.PreviewMouseDown += SearchPanel.Instance.keyboard8_button_PreviewMouseDown;
            SearchPanel.Instance.keyboard8_button.PreviewKeyDown   += SearchPanel.Instance.keyboard8_button_PreviewKeyDown;
            SearchPanel.Instance.keyboard9_button.PreviewMouseDown += SearchPanel.Instance.keyboard9_button_PreviewMouseDown;
            SearchPanel.Instance.keyboard9_button.PreviewKeyDown   += SearchPanel.Instance.keyboard9_button_PreviewKeyDown;
        }

        /// <summary>
        /// Registers Event Handlers for the Media Browser.
        /// </summary>
        private void RegisterEventHandlersMediaBrowser()
        {
            // Register Event Handlers for the Media Browser.
            MediaBrowser.Instance.mediaBrowser_listView.KeyDown            += MediaBrowser.Instance.mediaBrowser_listView_KeyDown;
            MediaBrowser.Instance.mediaBrowser_listView.MouseDoubleClick   += MediaBrowser.Instance.mediaBrowser_listView_MouseDoubleClick;
            MediaBrowser.Instance.mediaBrowser_listView.MouseEnter         += MediaBrowser.Instance.mediaBrowser_listView_MouseEnter;
            MediaBrowser.Instance.mediaBrowser_listView.MouseLeftButtonUp  += MediaBrowser.Instance.mediaBrowser_listView_MouseLeftButtonUp;
            MediaBrowser.Instance.mediaBrowser_listView.MouseRightButtonUp += MediaBrowser.Instance.mediaBrowser_listView_MouseRightButtonUp;
            MediaBrowser.Instance.mediaBrowser_listView.SelectionChanged   += MediaBrowser.Instance.mediaBrowser_listView_SelectionChanged;
        }

        /// <summary>
        /// Registers Event Handlers for the Right-Click Menu.
        /// </summary>
        private void RegisterEventHandlersRightClickMenu()
        {
            // Register Event Handlers for the Right-Click Menu.
            MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView.SelectionChanged       += MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView_SelectionChanged;

            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Play_listViewItem.KeyDown                  += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Play_listViewItem_KeyDown;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Play_listViewItem.MouseEnter               += MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView_MouseEnter;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Play_listViewItem.MouseLeftButtonUp        += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Play_listViewItem_MouseLeftButtonUp;

            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_PlayTrailer_listViewItem.KeyDown           += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_PlayTrailer_listViewItem_KeyDown;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_PlayTrailer_listViewItem.MouseEnter        += MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView_MouseEnter;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_PlayTrailer_listViewItem.MouseLeftButtonUp += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_PlayTrailer_listViewItem_MouseLeftButtonUp;

            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Disable_listViewItem.KeyDown               += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Disable_listViewItem_KeyDown;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Disable_listViewItem.MouseEnter            += MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView_MouseEnter;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Disable_listViewItem.MouseLeftButtonUp     += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Disable_listViewItem_MouseLeftButtonUp;

            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Enable_listViewItem.KeyDown                += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Enable_listViewItem_KeyDown;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Enable_listViewItem.MouseEnter             += MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView_MouseEnter;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Enable_listViewItem.MouseLeftButtonUp      += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Enable_listViewItem_MouseLeftButtonUp;

            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Alternative_listViewItem.KeyDown           += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Alternative_listViewItem_KeyDown;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Alternative_listViewItem.MouseEnter        += MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView_MouseEnter;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Alternative_listViewItem.MouseLeftButtonUp += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Alternative_listViewItem_MouseLeftButtonUp;

            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_EditMp3Tags_listViewItem.KeyDown           += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_EditMp3Tags_listViewItem_KeyDown;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_EditMp3Tags_listViewItem.MouseEnter        += MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView_MouseEnter;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_EditMp3Tags_listViewItem.MouseLeftButtonUp += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_EditMp3Tags_listViewItem_MouseLeftButtonUp;

            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Rename_listViewItem.KeyDown                += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Rename_listViewItem_KeyDown;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Rename_listViewItem.MouseEnter             += MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView_MouseEnter;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Rename_listViewItem.MouseLeftButtonUp      += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Rename_listViewItem_MouseLeftButtonUp;

            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Cancel_listViewItem.KeyDown                += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Cancel_listViewItem_KeyDown;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Cancel_listViewItem.MouseEnter             += MediaBrowserRightClickMenu.Instance.mediaBrowserRightClickMenu_listView_MouseEnter;
            MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Cancel_listViewItem.MouseLeftButtonUp      += MediaBrowserRightClickMenu.Instance.mediaBrowserRCM_Cancel_listViewItem_MouseLeftButtonUp;
        }

        /// <summary>
        /// Registers Event Handlers for the Edit Mp3 Tags Dialog.
        /// </summary>
        private void RegisterEventHandlersEditMp3Tags()
        {
            // Register Event Handlers for the Edit Mp3 Tags Dialog.
            MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsArtist_textBox.KeyDown         += MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3Tags_textBox_KeyDown;
            MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsTrack_textBox.KeyDown          += MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3Tags_textBox_KeyDown;
            MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsAlbum_textBox.KeyDown          += MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3Tags_textBox_KeyDown;
            MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsGenre_textBox.KeyDown          += MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3Tags_textBox_KeyDown;
            MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsReleased_textBox.KeyDown       += MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3Tags_textBox_KeyDown;
            MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsBrowse_textBox_button.Click    += MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsBrowse_textBox_Click;
            MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsNoImage_textBox_button.Click   += MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsNoImage_textBox_Click;
            MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsCancel_image.MouseLeftButtonUp += MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3TagsCancel_image_MouseLeftButtonUp;
            MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3Tags_image.MouseLeftButtonUp       += MediaBrowserEditMp3Tags.Instance.mediaBrowserEditMp3Tags_image_MouseLeftButtonUp;
        }

        /// <summary>
        /// Registers Event Handlers for the Rename Dialog.
        /// </summary>
        private void RegisterEventHandlersRename()
        {
            // Register Event Handlers for the Rename Dialog.
            MediaBrowserRename.Instance.mediaBrowserRename_textBox.KeyDown               += MediaBrowserRename.Instance.mediaBrowserRename_textBox_KeyDown;
            MediaBrowserRename.Instance.mediaBrowserRename_image.MouseLeftButtonUp       += MediaBrowserRename.Instance.mediaBrowserRename_image_MouseLeftButtonUp;
            MediaBrowserRename.Instance.mediaBrowserRenameCancel_image.MouseLeftButtonUp += MediaBrowserRename.Instance.mediaBrowserRenameCancel_image_MouseLeftButtonUp;
        }

        /// <summary>
        /// Registers Event Handlers for the Alternative Movie Details Menu.
        /// </summary>
        private void RegisterEventHandlersAlternativeMovieDetails()
        {
            // Register Event Handlers for the Alternative Movie Details Menu.
            MediaBrowserAlternativeMovieDetailsMenu.Instance.mediaBrowserAlternative_listView.KeyDown           += MediaBrowserAlternativeMovieDetailsMenu.Instance.mediaBrowserAlternative_listView_KeyDown;
            MediaBrowserAlternativeMovieDetailsMenu.Instance.mediaBrowserAlternative_listView.MouseLeftButtonUp += MediaBrowserAlternativeMovieDetailsMenu.Instance.mediaBrowserAlternative_listView_MouseLeftButtonUp;
            MediaBrowserAlternativeMovieDetailsMenu.Instance.mediaBrowserAlternative_listView.SelectionChanged  += MediaBrowserAlternativeMovieDetailsMenu.Instance.mediaBrowserAlternative_listView_SelectionChanged;
        }

        /// <summary>
        /// Registers Event Handlers for the Media Tags.
        /// </summary>
        private void RegisterEventHandlersMediaTags()
        {
            // Register Event Handlers for the Media Tags.
            MediaTags.Instance.tags_listView.KeyDown           += MediaTags.Instance.tags_listView_KeyDown;
            MediaTags.Instance.tags_listView.MouseLeftButtonUp += MediaTags.Instance.tags_listView_MouseLeftButtonUp;
            MediaTags.Instance.tags_listView.SelectionChanged  += MediaTags.Instance.tags_listView_SelectionChanged;
        }

        /// <summary>
        /// Registers Event Handlers.
        /// </summary>
        private void RegisterEventHandlers()
        {
            // Media Player Controls.
            this.RegisterEventHandlersMediaPlayerControls();

            // Media Player (Background Audio) Controls.
            this.RegisterEventHandlersMediaPlayerBackgroundAudioControls();

            // Main Menu.
            this.RegisterEventHandlersMainMenu();

            // Search Panel.
            this.RegisterEventHandlersSearchPanel();

            // Media Browser.
            this.RegisterEventHandlersMediaBrowser();

            // Media Browser Right-Click Menu.
            this.RegisterEventHandlersRightClickMenu();

            // Media Browser Edit Mp3 Tags.
            this.RegisterEventHandlersEditMp3Tags();

            // Media Browser Rename.
            this.RegisterEventHandlersRename();
            
            // Media Browser Alternative Movie Details Menu.
            this.RegisterEventHandlersAlternativeMovieDetails();

            // Media Tags.
            this.RegisterEventHandlersMediaTags();
        }

    }
}

