using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using FileSystem;
using Network;

namespace MediaInfoApi
{
    public static class TvInfo
    {
        // Private class member properties.
        private static ResourceManager resourceManager = new ResourceManager("MediaInfoApi.Properties.Resources", Assembly.GetExecutingAssembly());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tvShowEpisodeRawData"></param>
        /// <param name="seasonEpisode"></param>
        /// <returns></returns>
        private static Dictionary<string, string> GetEpisodeDetailsInternal(string tvShowEpisodeRawData, string seasonEpisode)
        {
            // The dictionary to contain all the episode details to return.
            Dictionary<string, string> episodeDetails = new Dictionary<string, string>() { };

            // Initialize dictionary values to blank strings.
            episodeDetails["airdate"]      = @"";
            episodeDetails["cast"]         = @"";
            episodeDetails["director"]     = @"";
            episodeDetails["genre"]        = @"";
            episodeDetails["image"]        = @"";
            episodeDetails["name"]         = @"";
            episodeDetails["number"]       = seasonEpisode;
            episodeDetails["next_airdate"] = @"";
            episodeDetails["next_number"]  = @"";
            episodeDetails["next_title"]   = @"";
            episodeDetails["summary"]      = @"";
            episodeDetails["title"]        = @"";
            episodeDetails["url"]          = @"";
            episodeDetails["writers"]      = @"";

            // Get the TV Show Name of the specified episode.
            if (tvShowEpisodeRawData.Contains(@"<name>"))
            {
                episodeDetails["name"] = TvInfo.GetEpisodeProperty(tvShowEpisodeRawData, @"show", @"name");
            }

            // Get the TV Show Genre of the specified episode.
            if (tvShowEpisodeRawData.Contains(@"<genres>"))
            {
                string tvShowGenre = TvInfo.GetEpisodeProperty(tvShowEpisodeRawData, @"show", @"genres");

                // Remove the <genre> tags.
                tvShowGenre = tvShowGenre.Replace(@"<genre>", "");
                tvShowGenre = tvShowGenre.Replace(@"</genre>", ", ");

                // Save the TV Show Genre.
                episodeDetails["genre"] = tvShowGenre.Trim().TrimEnd(',');
            }

            // Get the Title, Airdate and TVRage.com URL of the specified episode.
            if (tvShowEpisodeRawData.Contains(@"<episode>"))
            {
                episodeDetails["title"]   = TvInfo.GetEpisodeProperty(tvShowEpisodeRawData, @"episode", @"title");
                episodeDetails["airdate"] = TvInfo.GetEpisodeProperty(tvShowEpisodeRawData, @"episode", @"airdate");
                episodeDetails["url"]     = TvInfo.GetEpisodeProperty(tvShowEpisodeRawData, @"episode", @"url");
            }

            // Get the Episode Number, Airdate and Title of the next episode in the specified season if the show has not ended.
            if (tvShowEpisodeRawData.Contains(@"<nextepisode>"))
            {
                episodeDetails["next_number"]  = TvInfo.GetEpisodeProperty(tvShowEpisodeRawData, @"nextepisode", @"number");
                episodeDetails["next_airdate"] = TvInfo.GetEpisodeProperty(tvShowEpisodeRawData, @"nextepisode", @"airdate");
                episodeDetails["next_title"]   = TvInfo.GetEpisodeProperty(tvShowEpisodeRawData, @"nextepisode", @"title");
            }

            // Query TVRage.com and return the full episode details page.
            tvShowEpisodeRawData = Network.DataCommunication.GetHttpWebRequestResponse(episodeDetails["url"]);

            // Get the TV Show Director.
            if (tvShowEpisodeRawData.Contains(@"<b>Director: </b>"))
            {
                string tvShowDirectorPattern = @"<b>Director: </b>(?<tvShowDirector>.+)<br><b>Writer: </b>";
                Match  tvShowDirectorMatch   = Regex.Match(tvShowEpisodeRawData, tvShowDirectorPattern);
                string tvShowDirector        = Regex.Replace(tvShowDirectorMatch.ToString(), tvShowDirectorPattern, "${tvShowDirector}");
                
                // Clean up the result.
                tvShowDirector = Regex.Replace(tvShowDirector, @"\s*\(\d+\)", "");
                tvShowDirector = MediaInfo.RemoveHtmlTags(tvShowDirector).Trim();

                // Return the final result.
                episodeDetails["director"] = tvShowDirector;
            }

            // Get the TV Show Writers.
            if (tvShowEpisodeRawData.Contains(@"<b>Writer: </b>"))
            {
                string tvShowWritersPattern = @"<b>Writer: </b>(?<tvShowWriters>.+)</a></i><br></div>";
                Match  tvShowWritersMatch   = Regex.Match(tvShowEpisodeRawData, tvShowWritersPattern);
                string tvShowWriters        = Regex.Replace(tvShowWritersMatch.ToString(), tvShowWritersPattern, "${tvShowWriters}");
                
                // Clean up the result.
                tvShowWriters               = Regex.Replace(tvShowWriters, @"\s*\(\d+\)", "");
                tvShowWriters               = MediaInfo.RemoveHtmlTags(tvShowWriters).Trim();
                
                // Return the final result.
                episodeDetails["writers"]   = tvShowWriters;
            }
            
            // Get the TV Show Cast.
            if (tvShowEpisodeRawData.Contains(@"<span class='content_title'>Main Cast</span>"))
            {
                // Remove all data before the Main Cast part.
                string tvShowCastRawData = tvShowEpisodeRawData.Substring(tvShowEpisodeRawData.IndexOf(@"<span class='content_title'>Main Cast</span>"));

                string tvShowCastPattern = @"<b><a href='(.*)' >(?<tvShowCastMatch>.+)</a></b><br />(As|voiced)<i>";
                MatchCollection tvShowCastMatches = Regex.Matches(tvShowCastRawData, tvShowCastPattern);
                string          tvShowCast = "";

                // Check all matching results.
                foreach (Match tvShowCastMatch in tvShowCastMatches)
                {
                    // Split strings with double-matches.
                    string[] doubleMatches = Regex.Split(tvShowCastMatch.ToString(), "</td><td");

                    // Check each single-match.
                    foreach (string singleMatch in doubleMatches)
                    {
                        // Remove all data before and after the actor's name.
                        string match = singleMatch.Substring(singleMatch.LastIndexOf(@"' >") + 3);
                        match        = match.Substring(0, match.IndexOf(@"</a></b><br />"));

                        // Clean up the result.
                        match = MediaInfo.RemoveHtmlTags(match).Trim();

                        // Append the match to the cast string.
                        tvShowCast += match + ", ";
                    }
                }

                // Strip away the last comma seperator.
                tvShowCast = tvShowCast.Trim().TrimEnd(',');

                // Return the final result.
                episodeDetails["cast"] = tvShowCast;
            }
            
            // Get the Episode image, will be a screenshot from the actual episode.
            if (tvShowEpisodeRawData.Contains(@"http://images.tvrage.com/screencaps/"))
            {
                // Main variables.
                string imageFile = "";
                string imageUrl  = tvShowEpisodeRawData.Substring(
                    tvShowEpisodeRawData.IndexOf(@"http://images.tvrage.com/screencaps/"),
                    tvShowEpisodeRawData.IndexOf(@"style='max-width:280px'>") - (tvShowEpisodeRawData.IndexOf(@"http://images.tvrage.com/screencaps/") + 2)
                ).Trim();

                if (!String.IsNullOrEmpty(imageUrl))
                {
                    imageFile = DirectoryManagement.GetRuntimeExecutingPath() + @"\images\Screenshots\" + imageUrl.Substring(imageUrl.LastIndexOf(@"/") + 1);
                }

                try
                {
                    // Download the image to the local filesystem.
                    if (!String.IsNullOrEmpty(imageUrl) && !File.Exists(imageFile))
                    {
                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(imageUrl, imageFile);
                    }
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                        Class:  TvInfo
                        Method: GetEpisodeDetailsInternal(string tvShowEpisodeRawData, string seasonEpisode)
                        Action: webClient.DownloadFile(""" + imageUrl + @""", """ + imageFile + @""")
                        
                        " + exception.Message
                        , EventLogEntryType.Error, 123);
                }

                // Save the image path.
                if (File.Exists(imageFile))
                {
                    episodeDetails["image"] = imageFile;
                }
                else
                {
                    episodeDetails["image"] = imageUrl;
                }
            }

            // Get the Episode Summary.
            string episodeSummary = @"";

            // The HTML code to parse will be different if the episode contains an image or not.
            if (episodeDetails["image"] != @"")
            {
                if (tvShowEpisodeRawData.Contains(@"<div class='show_synopsis'>"))
                {
                    episodeSummary = tvShowEpisodeRawData.Substring(
                        tvShowEpisodeRawData.IndexOf(@"<div class='show_synopsis'>") + 27,
                        tvShowEpisodeRawData.IndexOf(@"<span class='left'></span>") - (tvShowEpisodeRawData.IndexOf(@"<div class='show_synopsis'>") + 27)
                    ).Trim();
                }
            }
            else
            {
                if (tvShowEpisodeRawData.Contains(@"</script></div><div>"))
                {
                    episodeSummary = tvShowEpisodeRawData.Substring(
                        tvShowEpisodeRawData.IndexOf(@"</script></div><div>") + 20,
                        tvShowEpisodeRawData.IndexOf(@"<span class='left'></span>") - (tvShowEpisodeRawData.IndexOf(@"</script></div><div>") + 20)
                    ).Trim();
                }
            }

            // After getting the substring needed for the Episode Summary, extract the text before the below HTML tag.
            if (episodeSummary.IndexOf(@"<b>Source: </b>") > -1)
            {
                episodeSummary = episodeSummary.Substring(0, episodeSummary.IndexOf(@"<b>Source: </b>"));
            }

            // Lastly we need to trim/remove some HTML linebreak, bold and italic tags.
            if (tvShowEpisodeRawData.Contains(@"<div class='show_synopsis'>") || tvShowEpisodeRawData.Contains(@"</script></div><div>"))
            {
                episodeSummary = MediaInfo.RemoveHtmlTags(episodeSummary);

                // If no summary exists for the current episode, 
                // display a descriptive message instead of the default "a href" HTML tag.
                if (episodeSummary.ToLower().Contains(@"click here to add a summary"))
                {
                    episodeDetails["summary"] = @"No summary is available.";
                }
                else
                {
                    episodeDetails["summary"] = episodeSummary.Trim();
                }
            }

            // Return the Episode Details.
            return episodeDetails;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        private static string GetEpisodeNumber(string searchString)
        {
            // Main variables.
            bool   episodeMatchFound = false;
            string tvShowEpisode     = @"";

            // Regular Expression Search Pattern for episode number.
            string[] episodePatterns = { 
                @"e(?<episode>\d+)", 
                @"ep(?<episode>\d+)", 
                @"episode(?<episode>\d+)", 
                @"episode (?<episode>\d+)"
            };

            // Check the search string for the episode.
            // Example: "Simpsons EP 01 Homer goes crazy" => "01" => Episode="01".
            // Example: "C:\Simpsons\Simpsons EP 01" => "01" => Episode="01".
            // Valid patterns: 
            // "E1", "E01", "EP1", "EP01", 
            // "Episode1", "Episode01", "Episode 1", "Episode 01".
            foreach (string pattern in episodePatterns)
            {
                if (!episodeMatchFound)
                {
                    if (Regex.IsMatch(searchString.ToLower(), pattern))
                    {
                        Match episodeSeasonRegExMatch = Regex.Match(searchString.ToLower(), pattern);
                        tvShowEpisode = Regex.Replace(episodeSeasonRegExMatch.ToString(), pattern, "${episode}");

                        episodeMatchFound = true;
                    }
                }
            }

            // Return the Episode Number.
            return tvShowEpisode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tvShowEpisodeRawData"></param>
        /// <param name="propertyTag"></param>
        /// <returns></returns>
        private static string GetEpisodeProperty(string tvShowEpisodeRawData, string rawDataSection, string propertyTag)
        {
            // Get the index locations of the episode tags.
            int indexOfEpisodeStartTag = tvShowEpisodeRawData.IndexOf(@"<" + rawDataSection + @"");
            int indexOfEpisodeEndTag   = tvShowEpisodeRawData.IndexOf(@"</" + rawDataSection + @">");

            // Get the substring containing the episode details.
            string episodeSubString = tvShowEpisodeRawData.Substring(indexOfEpisodeStartTag, (indexOfEpisodeEndTag - indexOfEpisodeStartTag));

            // Get the index locations of the property tags.
            int indexOfPropertyStartTag = episodeSubString.IndexOf(@"<" + propertyTag + @">") + propertyTag.Length + 2;
            int indexOfPropertyEndTag   = episodeSubString.IndexOf(@"</" + propertyTag + @">");

            // Get the substring containing the episode property.
            string episodeProperty = episodeSubString.Substring(indexOfPropertyStartTag, (indexOfPropertyEndTag - indexOfPropertyStartTag)).Trim();

            // Return the episode property.
            return episodeProperty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchString"></param>
        /// <param name="seasonEpisode"></param>
        /// <returns></returns>
        private static string GetEpisodeRawData(string searchString, string seasonEpisode)
        {
            // Main variables.
            string   httpResponseDataString = @"";
            bool     matchFound             = false;
            string[] searchStringArray;

            // Split the search string word by word.
            // Example: "Simpsons S01E01 XVID" => arr[0]="Simpsons", arr[1]="S01E01", arr[2]="XVID".
            searchStringArray = searchString.Split(' ');

            // We will parse the search string for a TV Show Name in the following way:
            // Search String: "Simpsons S1 EP1" => arr[0]="Simpsons", arr[1]="S1", arr[2]="EP1".
            // Search 1: "Simpsons S1 EP1" => arr[0]="Simpsons", arr[1]="S1", arr[2]="EP1".
            // Search 2: "Simpsons S1" => arr[0]="Simpsons", arr[1]="S1".
            // Search 3: "Simpsons" => arr[0]="Simpsons".
            for (int loopCounter = (searchStringArray.Length - 1); loopCounter >= 0; loopCounter--)
            {
                // Search only as long as we haven't yet found a match.
                if (!matchFound)
                {
                    // Main variables.
                    int    substringLength = 0;
                    string searchSubstring = @"";

                    // Set the correct substring length of the search string based on 
                    // which iteration of the loop we are currently in.
                    if (loopCounter == (searchStringArray.Length - 1))
                    {
                        substringLength = searchString.Length;
                    }
                    else if (loopCounter == 0)
                    {
                        substringLength = searchStringArray[0].Length;
                    }
                    else
                    {
                        for (int innerLoopCounter = 0; innerLoopCounter <= loopCounter; innerLoopCounter++)
                        {
                            substringLength += searchStringArray[innerLoopCounter].Length + 1;
                        }

                        //substringLength = searchString.LastIndexOf(searchStringArray[loopCounter + 1]);
                    }

                    // Set the correct search string.
                    searchSubstring = searchString.Substring(0, substringLength).Trim();

                    // Skip searching for the tv show "Season" as it will find a match when the directory name is "Season 2" for example.
                    if (!searchSubstring.ToLower().Contains(@"season"))
                    {
                        try
                        {
                            // Query TVRage.com for episode details.
                            httpResponseDataString = Network.DataCommunication.GetHttpWebRequestResponse(@"http://services.tvrage.com/feeds/episodeinfo.php?show=" + searchSubstring + @"&exact=1&ep=" + seasonEpisode + @"");

                            EventLog.WriteEntry(resourceManager.GetString("application_title"),
                                @"
                                Class:  TvInfo
                                Method: GetEpisodeRawData(""" + searchString + @""", """ + seasonEpisode + @""")
                                Action: DataCommunication.GetHttpWebRequestResponse(""http://services.tvrage.com/feeds/episodeinfo.php?show=" + searchSubstring + @"&exact=1&ep=" + seasonEpisode + @""")
                                httpResponseDataString: " + httpResponseDataString + @""
                                , EventLogEntryType.Error, 123);
                        }
                        catch (Exception exception)
                        {
                            EventLog.WriteEntry(resourceManager.GetString("application_title"),
                                @"
                                Class:  TvInfo
                                Method: GetEpisodeRawData(""" + searchString + @""", """ + seasonEpisode + @""")
                                Action: DataCommunication.GetHttpWebRequestResponse(""http://services.tvrage.com/feeds/episodeinfo.php?show=" + searchSubstring + @"&exact=1&ep=" + seasonEpisode + @""")
                                httpResponseDataString: " + httpResponseDataString + @"
                                
                                " + exception.Message
                                , EventLogEntryType.Error, 123);
                        }

                        // Let the iterative loop know that we have found a match so we can break out of the loop.
                        if (httpResponseDataString.Contains(@"<show id"))
                        {
                            matchFound = true;
                        }
                        else
                        {
                            httpResponseDataString = @"";
                            matchFound             = false;
                        }
                    }
                }
            }

            // Return the Episode details if a tv show name was found in the search string.
            // Otherwise return a blank string.
            return httpResponseDataString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        private static string GetSeasonEpisodeNumbers(string searchString)
        {
            bool   episodeSeasonMatchFound = false;
            string tvShowEpisode           = @"";
            string tvShowSeason            = @"";
            string tvShowSeasonEpisode     = @"";

            // Regular Expression Search Pattern for season and episode numbers.
            string[] episodeSeasonPatterns = { 
                @"s(?<season>\d+)e(?<episode>\d+)", 
                @"s(?<season>\d+)ep(?<episode>\d+)",
                @"s(?<season>\d+)xe(?<episode>\d+)", 
                @"s(?<season>\d+)xep(?<episode>\d+)",
                @"s(?<season>\d+)\.e(?<episode>\d+)", 
                @"s(?<season>\d+)\.ep(?<episode>\d+)",
                @"s(?<season>\d+)_e(?<episode>\d+)", 
                @"s(?<season>\d+)_ep(?<episode>\d+)",
                @"(?<season>\d+)\.(?<episode>\d+)", 
                @"(?<season>\d+)x(?<episode>\d+)"
            };

            // Check the filename for a keyword that has both season and episode in it.
            // Example: "Simpsons 1x01 Homer goes crazy" => "1x01" => Season="1", Episode="01".
            // Valid patterns: 
            // "S1E1", "S1E01", "S01E1", "S01E01", 
            // "S1EP1", "S1EP01", "S01EP1", "S01EP01",
            // "S1xE1", "S1xE01", "S01xE1", "S01xE01", 
            // "S1xEP1", "S1xEP01", "S01xEP1", "S01xEP01".
            // "S1.E1", "S1.E01", "S01.E1", "S01.E01", 
            // "S1.EP1", "S1.EP01", "S01.EP1", "S01.EP01".
            // "1x1", "1x01", "01x1", "01x01", 
            // "1.1", "1.01", "01.1", "01.01".
            foreach (string pattern in episodeSeasonPatterns)
            {
                if (!episodeSeasonMatchFound)
                {
                    if (Regex.IsMatch(searchString.ToLower(), pattern))
                    {
                        Match episodeSeasonRegExMatch = Regex.Match(searchString.ToLower(), pattern);
                        tvShowSeason = Regex.Replace(episodeSeasonRegExMatch.ToString(), pattern, "${season}");
                        tvShowEpisode = Regex.Replace(episodeSeasonRegExMatch.ToString(), pattern, "${episode}");

                        episodeSeasonMatchFound = true;
                    }
                }
            }

            // Concatenate the season and episode into a SxE string, 
            // Season 1 and Episode 2 will be "1x2".
            // Otherwise leave the return string as a blank string.
            if (!String.IsNullOrEmpty(tvShowSeason) && !String.IsNullOrEmpty(tvShowEpisode))
            {
                tvShowSeasonEpisode = @"" + tvShowSeason + @"x" + tvShowEpisode + @"";
            }

            // Return the Episode and Season string.
            return tvShowSeasonEpisode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        private static string GetSeasonNumber(string searchString)
        {
            bool   seasonMatchFound = false;
            string tvShowSeason     = @"";

            // Regular Expression Search Pattern for season number.
            string[] seasonPatterns = { 
                @"s(?<season>\d+)",
                @"season(?<season>\d+)",
                @"season (?<season>\d+)",
            };

            // Check the search string for the season.
            // Example: "Simpsons S01 Homer goes crazy" => "01" => Season="01".
            // Example: "C:\Simpsons\Season 1" => "1" => Season="1".
            // Valid patterns: 
            // "S1", "S01", "Season1", "Season01", "Season 1", "Season 01".
            foreach (string pattern in seasonPatterns)
            {
                if (!seasonMatchFound)
                {
                    if (Regex.IsMatch(searchString.ToLower(), pattern))
                    {
                        Match episodeSeasonRegExMatch = Regex.Match(searchString.ToLower(), pattern);
                        tvShowSeason = Regex.Replace(episodeSeasonRegExMatch.ToString(), pattern, "${season}");

                        seasonMatchFound = true;
                    }
                }
            }

            // Return the Season Number.
            return tvShowSeason;
        }

        // Public Methods.

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        /// <param name="seasonEpisode"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetEpisodeDetails(string fileName, string filePath, string seasonEpisode)
        {
            // Main variables.
            string   httpResponseDataString = @"";
            string[] filePathArray          = new string[] { };

            // Reserve a dictionary to contain all the episode details to return.
            Dictionary<string, string> episodeDetails = new Dictionary<string, string>() { };

            // Remove special characters.
            fileName = MediaInfo.RemoveSpecialCharacters(fileName);
            filePath = MediaInfo.RemoveSpecialCharacters(filePath);

            try
            {
                // We will search filename for tv show name in the following way:
                // Search String: "Simpsons S1 EP1" => arr[0]="Simpsons", arr[1]="S1", arr[2]="EP1".
                // Search 1: "Simpsons S1 EP1" => arr[0]="Simpsons", arr[1]="S1", arr[2]="EP1".
                // Search 2: "Simpsons S1" => arr[0]="Simpsons", arr[1]="S1".
                // Search 3: "Simpsons" => arr[0]="Simpsons".
                if (!String.IsNullOrEmpty(fileName) && !String.IsNullOrEmpty(seasonEpisode))
                {
                    httpResponseDataString = TvInfo.GetEpisodeRawData(fileName, seasonEpisode);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  TvInfo
                    Method: GetEpisodeDetails(""" + fileName + @""", """ + filePath + @""", """ + seasonEpisode + @""")
                    Action: TvInfo.GetEpisodeRawData(""" + fileName + @""", """ + seasonEpisode + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            // Check the directory name if we couldn't find the tv show name in the filename.
            if (String.IsNullOrEmpty(httpResponseDataString))
            {
                // Split the file path word by word.
                // Example: "C:\Dir\Simpsons\Season 1" => arr[0]="C:", arr[1]="Dir", arr[2]="Simpsons", arr[3]="Season 1".
                filePathArray = filePath.Split('\\');

                // We will search directory name for tv show name in the following way:
                // Search String: "C:\Dir\Simpsons S1 EP1" => 
                // outer_arr[0]="C:", outer_arr[1]="Dir", outer_arr[2]="Simpsons S1 EP1" => 
                // inner_arr1[0]="C:" =>
                // inner_arr2[0]="Dir" =>
                // inner_arr3[0]="Simpsons", inner_arr3[1]="S1", inner_arr3[2]="EP1".
                // outer_arr[0] => Inner Search 1: "C:" => inner_arr1[0]="C:".
                // outer_arr[1] => Inner Search 1: "Dir" => inner_arr1[0]="Dir".
                // outer_arr[2] => Inner Search 1: "Simpsons S1 EP1" => inner_arr3[0]="Simpsons", inner_arr3[1]="S1", inner_arr3[2]="EP1".
                // outer_arr[2] => Inner Search 2: "Simpsons S1" => inner_arr3[0]="Simpsons", inner_arr3[1]="S1".
                // outer_arr[2] => Inner Search 3: "Simpsons" => inner_arr3[0]="Simpsons".
                for (int loopCounter = (filePathArray.Length - 1); loopCounter >= 0; loopCounter--)
                {
                    string subDirectory = filePathArray[loopCounter];

                    try
                    {
                        if (!String.IsNullOrEmpty(subDirectory) && !String.IsNullOrEmpty(seasonEpisode))
                        {
                            httpResponseDataString = TvInfo.GetEpisodeRawData(subDirectory, seasonEpisode);
                        }
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                            Class:  TvInfo
                            Method: GetEpisodeDetails(""" + fileName + @""", """ + filePath + @""", """ + seasonEpisode + @""")
                            Action: TvInfo.GetEpisodeRawData(""" + subDirectory + @""", """ + seasonEpisode + @""")
                            
                            " + exception.Message
                            , EventLogEntryType.Error, 123);
                    }
                }
            }

            try
            {
                // Fill the episode details dictionary with the episode properties if available.
                // If no details are available for the episode all properties will be blank strings.
                if (!String.IsNullOrEmpty(httpResponseDataString) && !String.IsNullOrEmpty(seasonEpisode))
                {
                    episodeDetails = TvInfo.GetEpisodeDetailsInternal(httpResponseDataString, seasonEpisode);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  TvInfo
                    Method: GetEpisodeDetails(""" + fileName + @""", """ + filePath + @""", """ + seasonEpisode + @""")
                    Action: TvInfo.GetEpisodeDetailsInternal(""<httpResponseDataString>"", """ + seasonEpisode + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            // Return the Episode Details.
            return episodeDetails;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetSeasonEpisodeNumbers(string fileName, string filePath)
        {
            // Main variables.
            string tvShowEpisode       = @"";
            string tvShowSeason        = @"";
            string tvShowSeasonEpisode = @"";

            // Check the filename for a keyword that has both season and episode in it.
            // Example: "Simpsons 1x01 Homer goes crazy" => "1x01" => Season="1", Episode="01".
            tvShowSeasonEpisode = TvInfo.GetSeasonEpisodeNumbers(fileName);

            // Check the filename for either the season or the episode.
            // Example: "Simpsons S01 Homer goes crazy"   => "01" => Season="01".
            // Example: "Simpsons EP 01 Homer goes crazy" => "01" => Episode="01".
            if (String.IsNullOrEmpty(tvShowSeasonEpisode))
            {
                tvShowSeason  = TvInfo.GetSeasonNumber(fileName);
                tvShowEpisode = TvInfo.GetEpisodeNumber(fileName);
            }

            // Check the directory path for either the season or the episode.
            if (String.IsNullOrEmpty(tvShowSeasonEpisode) && (String.IsNullOrEmpty(tvShowSeason) || String.IsNullOrEmpty(tvShowEpisode)))
            {
                if (String.IsNullOrEmpty(tvShowSeason))
                {
                    // Check the directory path for the season.
                    // Example: "C:\Simpsons\Season 1" => "1" => Season="1".
                    tvShowSeason = TvInfo.GetSeasonNumber(filePath);
                }

                if (String.IsNullOrEmpty(tvShowEpisode))
                {
                    // Check the directory path for the episode.
                    // Example: "C:\Simpsons\Simpsons EP01" => "01" => Episode="01".
                    tvShowEpisode = TvInfo.GetEpisodeNumber(filePath);
                }
            }

            // Concatenate the season and episode into a SxE string, 
            // Season 1 and Episode 2 will be "1x2".
            // Otherwise leave the return string as a blank string.
            if (String.IsNullOrEmpty(tvShowSeasonEpisode) && !String.IsNullOrEmpty(tvShowSeason) && !String.IsNullOrEmpty(tvShowEpisode))
            {
                tvShowSeasonEpisode = @"" + tvShowSeason + @"x" + tvShowEpisode + @"";
            }

            // Return the Episode and Season string.
            return tvShowSeasonEpisode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool IsTvShow(string fileName, string filePath)
        {
            // Remove known movie filename substrings that can cause a tv show match.
            string stringToMatch = fileName.ToLower();
            stringToMatch = stringToMatch.Replace(@"dd5.1", "");
            stringToMatch = stringToMatch.Replace(@"dd 5.1", "");
            stringToMatch = stringToMatch.Replace(@"dd2.1", "");
            stringToMatch = stringToMatch.Replace(@"dd 2.1", "");
            stringToMatch = stringToMatch.Replace(@"dts5.1", "");
            stringToMatch = stringToMatch.Replace(@"dts 5.1", "");
            stringToMatch = stringToMatch.Replace(@"dts2.1", "");
            stringToMatch = stringToMatch.Replace(@"dts 2.1", "");

            // Check if the filename or path contains any keywords that could identify it as a TV Show.
            if (
                Regex.IsMatch(stringToMatch, @"s\d{1,2}(e|ep|x|xe|xep|\.ep| ep.|_e|_ep)\d{1,2}\D") ||
                Regex.IsMatch(stringToMatch, @"(e|ep)\d{1,2}\D") ||
                Regex.IsMatch(stringToMatch, @"\D\d{1,2}(x|\.)\d{1,2}\D") ||
                stringToMatch.Contains(@"episode") ||
                stringToMatch.Contains(@"season") ||
                stringToMatch.Contains(@"season")
            ) {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
