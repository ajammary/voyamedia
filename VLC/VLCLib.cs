using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;

namespace VLC
{
    public static class VLCLib
    {
        #region audio
            // Audio Get Track
            [DllImport("libvlc.dll")]
            private static extern int libvlc_audio_get_track(IntPtr player);
            
            // Audio Get Track Count
            [DllImport("libvlc.dll")]
            private static extern int libvlc_audio_get_track_count(IntPtr player);
            
            // Audio Mute
            [DllImport("libvlc.dll")]
            private static extern void libvlc_audio_set_mute(IntPtr player, bool mute);
            
            // Audio Set Track
            [DllImport("libvlc.dll")]
            private static extern int libvlc_audio_set_track(IntPtr player, int track);
        #endregion

        #region core
            // New
            [DllImport("libvlc.dll")]
            private static extern IntPtr libvlc_new(int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] argv);
            
            // Release
            [DllImport("libvlc.dll")]
            private static extern void libvlc_release(IntPtr instance);
        #endregion

        #region exception
            // VLC Exception Clear Error
            [DllImport("libvlc.dll")]
            private static extern void libvlc_clearerr();

            // VLC Exception Error Message
            [DllImport("libvlc.dll")]
            private static extern IntPtr libvlc_errmsg();
        #endregion
            
        #region media
            // Media Get Duration
            [DllImport("libvlc.dll")]
            private static extern Int64 libvlc_media_get_duration(IntPtr media);
            
            // Media New Location
            [DllImport("libvlc.dll")]
            private static extern IntPtr libvlc_media_new_location(IntPtr p_instance, [MarshalAs(UnmanagedType.LPStr)] string psz_mrl);
            
            // Media Release
            [DllImport("libvlc.dll")]
            private static extern void libvlc_media_release(IntPtr p_meta_desc);
        #endregion

        #region media_player
            // Media Player Get Length/Duration
            [DllImport("libvlc.dll")]
            private static extern Int64 libvlc_media_player_get_length(IntPtr player);

            // Media Player Get Media
            [DllImport("libvlc.dll")]
            private static extern IntPtr libvlc_media_player_get_media(IntPtr player);

            // Media Player Get Playback Position
            [DllImport("libvlc.dll")]
            private static extern Int64 libvlc_media_player_get_time(IntPtr player);

            // Media Player Is Playing?
            [DllImport("libvlc.dll")]
            private static extern bool libvlc_media_player_is_playing(IntPtr player);

            // Media Player New From Media
            [DllImport("libvlc.dll")]
            private static extern IntPtr libvlc_media_player_new_from_media(IntPtr media);

            // Media Player Pause
            [DllImport("libvlc.dll")]
            private static extern void libvlc_media_player_pause(IntPtr player);

            // Media Player Play
            [DllImport("libvlc.dll")]
            private static extern int libvlc_media_player_play(IntPtr player);
            
            // Media Player Release
            [DllImport("libvlc.dll")]
            private static extern void libvlc_media_player_release(IntPtr player);

            // Media Player Set Drawable Handle Window
            [DllImport("libvlc.dll")]
            private static extern void libvlc_media_player_set_hwnd(IntPtr player, IntPtr drawable);
            
            // Media Player Set FullScreen On/Off.
            [DllImport("libvlc.dll")]
            private static extern void libvlc_set_fullscreen(IntPtr player, int fullscreenEnabled);
            
            // Media Player Set Media
            [DllImport("libvlc.dll")]
            private static extern void libvlc_media_player_set_media(IntPtr player, IntPtr media);

            // Media Player Playback Rate/Speed
            [DllImport("libvlc.dll")]
            private static extern int libvlc_media_player_set_rate(IntPtr player, float rate);

            // Media Player Set Playback Position
            [DllImport("libvlc.dll")]
            private static extern void libvlc_media_player_set_time(IntPtr player, Int64 time);

            // Media Player Stop
            [DllImport("libvlc.dll")]
            private static extern void libvlc_media_player_stop(IntPtr player);
        #endregion

        #region video
            // Video Get Subtitle
            [DllImport("libvlc.dll")]
            private static extern int libvlc_video_get_spu(IntPtr player);
            
            // Video Get Subtitle Count
            [DllImport("libvlc.dll")]
            private static extern int libvlc_video_get_spu_count(IntPtr player);
            
            // Video Set Aspect Ratio
            [DllImport("libvlc.dll")]
            private static extern void libvlc_video_set_aspect_ratio(IntPtr player, string ratio);
            
            // Video Set Key Input
            [DllImport("libvlc.dll")]
            private static extern void libvlc_video_set_key_input(IntPtr player, bool key_input);
            
            // Video Set Mouse Input
            [DllImport("libvlc.dll")]
            private static extern void libvlc_video_set_mouse_input(IntPtr player, bool key_input);
            
            // Video Set Scale Factor
            [DllImport("libvlc.dll")]
            private static extern void libvlc_video_set_scale(IntPtr player, float scaleFactor);
            
            // Video Set Built-In Subtitle
            [DllImport("libvlc.dll")]
            private static extern int libvlc_video_set_spu(IntPtr player, int subtitle);
            
            // Video Set Subtitle from a file
            [DllImport("libvlc.dll")]
            private static extern int libvlc_video_set_subtitle_file(IntPtr player, string subtitle);
        #endregion

        // Private class member properties.
        private static ResourceManager resourceManager = new ResourceManager("VLC.Properties.Resources", Assembly.GetExecutingAssembly());

        /// <summary>
        /// Creates a Windows Event Log Source for the application if it doesn't already exist.
        /// </summary>
        private static void InitWindowsEventLog()
        {
            // Create a Windows Event Log Source.
            if (!EventLog.SourceExists(resourceManager.GetString("application_title")))
            {
                EventLog.CreateEventSource(resourceManager.GetString("application_title"), "Application");
            }
        }

        /// <summary>
        /// Returns the complete duration of the media file loaded in the media player.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        /// <returns>A long integer specifying the duration of the media in milliseconds</returns>
        public static Int64 GetDuration(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            // Main variables.
            Int64 duration = 0;

            try
            {
                duration = VLCLib.libvlc_media_player_get_length(vlcPlayer);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: GetDuration(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            return duration;
        }

        /// <summary>
        /// Returns the index of the currently selected audio track.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        /// <returns>An integer specifying the current audio track.</returns>
        public static int GetAudioTrack(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            // Main variables.
            int track = 0;

            try
            {
                track = VLCLib.libvlc_audio_get_track(vlcPlayer);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: GetAudioTrack(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            return track;
        }

        /// <summary>
        /// Returns the index of the currently selected subtitle track.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        /// <returns>An integer specifying the current subtitle track.</returns>
        public static int GetSubtitleTrack(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            // Main variables.
            int track = 0;

            try
            {
                track = VLCLib.libvlc_video_get_spu(vlcPlayer);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: GetSubtitleTrack(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            return track;
        }

        /// <summary>
        /// Returns the current playback position of the media file loaded in the media player.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        /// <returns>A long integer specifying the current position of the media in milliseconds.</returns>
        public static Int64 GetPlaybackPosition(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            // Main variables.
            Int64 position = 0;

            try
            {
                position = VLCLib.libvlc_media_player_get_time(vlcPlayer);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: GetPlaybackPosition(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            return position;
        }

        /// <summary>
        /// Checks if the media is currently playing.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        /// <returns>A boolean, true if the media is playing, false otherwise.</returns>
        public static bool IsPlaying(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            // Main variables.
            bool isPlaying = false;

            try
            {
                isPlaying = VLCLib.libvlc_media_player_is_playing(vlcPlayer);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: IsPlaying(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            return isPlaying;
        }

        /// <summary>
        /// Mutes the audio volume of the loaded media file.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        public static void Mute(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Mute the volume.
                VLCLib.libvlc_audio_set_mute(vlcPlayer, true);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: Mute(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Opens the media file.
        /// </summary>
        /// <param name="mediaFile">File to open.</param>
        /// <param name="drawableAreaHandle">A handle to the window or area where the video should be rendered.</param>
        public static IntPtr Open(string mediaFile, IntPtr drawableAreaHandle)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            // Main variables.
            IntPtr vlcInstance = new IntPtr();
            IntPtr vlcMedia    = new IntPtr();
            IntPtr vlcPlayer   = new IntPtr();

            // Command-Line arguments to send to the VLC instance.
            string[] args = new string[] {
                @"--plugin-path=3rd\vlc",
                @"--no-video-title-show",
                @"--no-qt-system-tray",
                @"--qt-volume-complete",
                @"--volume=512"
            };

            try
            {
                // Initialize a new VLC instance.
                vlcInstance = VLCLib.libvlc_new(args.Length, args);

                // Select the media file.
                vlcMedia = VLCLib.libvlc_media_new_location(vlcInstance, mediaFile);

                // Open the media file in the player.
                vlcPlayer = VLCLib.libvlc_media_player_new_from_media(vlcMedia);

                // Render the media to the drawable area.
                VLCLib.libvlc_media_player_set_hwnd(vlcPlayer, drawableAreaHandle);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: Open(""" + mediaFile + @""", IntPtr drawableAreaHandle)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            finally
            {
                // Release reserved resources used.
                VLCLib.libvlc_release(vlcInstance);
                VLCLib.libvlc_media_release(vlcMedia);
            }

            return vlcPlayer;
        }

        /// <summary>
        /// Starts playing the media loaded in the media player.
        /// </summary>
        /// <param name="vlcPlayer">A handle to a VLC media player object.</param>
        public static void Play(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Remove the black borders by scaling the video using proper widescreen (16:9) ratio.
                VLCLib.libvlc_video_set_aspect_ratio(vlcPlayer, "16:9");

                // Start playing the media.
                VLCLib.libvlc_media_player_play(vlcPlayer);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: Play(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Pauses the media loaded in the media player.
        /// </summary>
        /// <param name="vlcPlayer">A handle to a VLC media player object.</param>
        public static void Pause(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Pause the media.
                VLCLib.libvlc_media_player_pause(vlcPlayer);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: Pause(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Releases all reserved resources for the VLC media player.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        public static void ReleaseMediaPlayerResources(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Release media player resources.
                VLCLib.libvlc_media_player_release(vlcPlayer);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: ReleaseMediaPlayerResources(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Seeks and starts playback from the selected position.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        /// <param name="position">An integer specifying the playback position in milliseconds.</param>
        public static void SeekToPosition(IntPtr vlcPlayer, Int64 position)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Seek to the specified position.
                VLCLib.libvlc_media_player_set_time(vlcPlayer, position);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: SeekToPosition(IntPtr vlcPlayer, " + position.ToString() + @")
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Changes the current audio track to the specified track.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        /// <param name="track">An integer specifying the audio track to play.</param>
        public static void SetAudioTrack(IntPtr vlcPlayer, int track)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Set the selected track as long as it is valid.
                if (
                    (track > 0) && 
                    (track < VLCLib.libvlc_audio_get_track_count(vlcPlayer)) && 
                    (track != VLCLib.libvlc_audio_get_track(vlcPlayer))
                ) {
                    VLCLib.libvlc_audio_set_track(vlcPlayer, track);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: SetAudioTrack(IntPtr vlcPlayer, " + track.ToString() + @")
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }
        
        /// <summary>
        /// Changes the playback speed of the media file.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        /// <param name="speed"></param>
        public static void SetPlaybackSpeed(IntPtr vlcPlayer, float speed)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Set the playback speed.
                VLCLib.libvlc_media_player_set_rate(vlcPlayer, speed);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: SetPlaybackSpeed(IntPtr vlcPlayer, " + speed.ToString() + @")
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Changes the current subtitle track to the specified track.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        /// <param name="track">An integer specifying the subtitle track to play.</param>
        public static void SetSubtitleTrack(IntPtr vlcPlayer, int track)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Set the selected track as long as it is valid.
                if (
                    (track >= 0) && 
                    (track < VLCLib.libvlc_video_get_spu_count(vlcPlayer)) && 
                    (track != VLCLib.libvlc_video_get_spu(vlcPlayer))
                ) {
                    VLCLib.libvlc_video_set_spu(vlcPlayer, track);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: SetSubtitleTrack(IntPtr vlcPlayer, " + track.ToString() + @")
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Changes the current subtitle track to the specified subtitle file.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        /// <param name="subtitleFile">A string specifying the subtitle file to play.</param>
        public static void SetSubtitleTrack(IntPtr vlcPlayer, string subtitleFile)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Set the selected track as long as it is valid.
                if (!String.IsNullOrEmpty(subtitleFile) && File.Exists(subtitleFile))
                {
                    VLCLib.libvlc_video_set_subtitle_file(vlcPlayer, subtitleFile);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: SetSubtitleTrack(IntPtr vlcPlayer, " + subtitleFile + @")
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Stops playing the media loaded in the media player.
        /// </summary>
        /// <param name="vlcPlayer">A handle to a VLC media player object.</param>
        public static void Stop(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Stop playing the media.
                VLCLib.libvlc_media_player_stop(vlcPlayer);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: Stop(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }

        /// <summary>
        /// Returns the audio volume of the loaded media file back to normal.
        /// </summary>
        /// <param name="vlcPlayer"></param>
        public static void UnMute(IntPtr vlcPlayer)
        {
            // Initialize windows event log source.
            VLCLib.InitWindowsEventLog();

            try
            {
                // Mute the volume.
                VLCLib.libvlc_audio_set_mute(vlcPlayer, false);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  VLCLib
                        Method: UnMute(IntPtr vlcPlayer)
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
        }
    }

}
