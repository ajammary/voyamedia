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
using System.Xml;
using FileSystem;
using Network;

namespace MediaInfoApi
{
    public static class MovieInfo
    {
        // Private class member properties.
        private static string          apiKey          = @"14f69dec4b96134333164bb686904504";
        private static ResourceManager resourceManager = new ResourceManager("MediaInfoApi.Properties.Resources", Assembly.GetExecutingAssembly());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public static List<string> GetAlternativeMovieMatches(string searchString)
        {
            // Main variables.
            List<string> matches                = new List<string>() { };
            bool         matchFound             = false;
            string[]     searchArray            = new string[] { };
            string       searchResults          = @"0";
            string       httpResponseDataString = @"";

            // Split the search string word by word.
            // Example: "Back to the Future" => arr[0]="Back", arr[1]="to", arr[2]="the", arr[3]="Future".
            searchArray = searchString.Split(' ');

            // We will search for movies in the following way:
            // Search String: "Back to the Future CD1 XVID" => arr[0]="Back", arr[1]="to", arr[2]="the", arr[3]="Future", arr[4]="CD1", arr[5]="XVID".
            // Search 1: "Back to the Future CD1 XVID" => arr[0]="Back", arr[1]="to", arr[2]="the", arr[3]="Future", arr[4]="CD1", arr[5]="XVID".
            // Search 2: "Back to the Future CD1" => arr[0]="Back", arr[1]="to", arr[2]="the", arr[3]="Future", arr[4]="CD1".
            // Search 3: "Back to the Future" => arr[0]="Back", arr[1]="to", arr[2]="the", arr[3]="Future".
            // Search 4: "Back to the" => arr[0]="Back", arr[1]="to", arr[2]="the".
            // Search 5: "Back to" => arr[0]="Back", arr[1]="to".
            // Search 6: "Back" => arr[0]="Back".
            for (int loopCounter = (searchArray.Length - 1); loopCounter >= 0; loopCounter--)
            {
                // Search only as long as we haven't yet found a match.
                // In the above exmples we need max 6 searches, 
                // but since we have a match on the 3rd search we only need 3 searches.
                if (!matchFound)
                {
                    // Main variables.
                    int    substringLength = 0;
                    string searchSubstring = @"";

                    // Set the correct substring length of the search string based on which iteration of the loop we are currently in.
                    if (loopCounter == (searchArray.Length - 1))
                    {
                        substringLength = searchString.Length;
                    }
                    else if (loopCounter == 0)
                    {
                        substringLength = searchArray[0].Length;
                    }
                    else
                    {
                        substringLength = searchString.LastIndexOf(searchArray[loopCounter + 1]);
                    }

                    // Set the correct search string.
                    searchSubstring = searchString.Substring(0, substringLength);

                    try
                    {
                        if (!String.IsNullOrEmpty(searchSubstring))
                        {
                            // Send an HTTP web request to TheMovieDB.org to search for the specified movie.
                            httpResponseDataString = Network.DataCommunication.GetHttpWebRequestResponse(@"http://api.themoviedb.org/2.1/Movie.search/en/xml/" + MovieInfo.apiKey + @"/" + searchSubstring + @"");
                        }
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                            Class:  MovieInfo
                            Method: GetMovieRawData(""" + searchString + @""")
                            Action: DataCommunication.GetHttpWebRequestResponse(""http://api.themoviedb.org/2.1/Movie.search/en/xml/MovieInfo.apiKey/" + searchSubstring + @""")
                            httpResponseDataString: " + httpResponseDataString + @"
                            
                            " + exception.Message
                            , EventLogEntryType.Error, 123);
                    }

                    // Extract from the result how many movies the search query returned.
                    if (!String.IsNullOrEmpty(httpResponseDataString))
                    {
                        string searchResultsPattern = @"<opensearch:totalResults>(?<searchResults>.+)</opensearch:totalResults>";
                        Match  searchResultsMatch   = Regex.Match(httpResponseDataString, searchResultsPattern);
                        searchResults               = Regex.Replace(searchResultsMatch.ToString(), searchResultsPattern, "${searchResults}");
                    }

                    // Let the iterative loop know that we have found at least one match so we can break out of the loop.
                    if (!String.IsNullOrEmpty(searchResults) && (searchResults != @"0"))
                    {
                        matchFound = true;
                        
	                    // Load in the contents of the Manifest XML file.
	                    System.Xml.XmlDocument manifestXml = new System.Xml.XmlDocument();
	                    manifestXml.LoadXml(httpResponseDataString);

	                    // Add a <OpenSearchDescription> Node-Alias for the <OpenSearchDescription xmlns:opensearch="http://a9.com/-/spec/opensearch/1.1/"> node so the XPATH parsing works.
	                    System.Xml.XmlNamespaceManager xmlNameSpaceManager = new System.Xml.XmlNamespaceManager(manifestXml.NameTable);
	                    xmlNameSpaceManager.AddNamespace("root", manifestXml.NamespaceURI);
	
	                    // Get a collection/list of all the <movies> children nodes in <root>.
                        XmlNodeList rootNodes = manifestXml.SelectNodes("//root:movies", xmlNameSpaceManager);
	                    
	                    // Parse each child node.
                        foreach (XmlNode movieNodes in rootNodes) 
                        {
                            foreach (XmlNode movieNode in movieNodes.ChildNodes)
                            {
                                // Initialize a blank movie title.
                                string movieTitle = "";

                                // Extract the movie name and release date and add them to the movie title.
                                foreach (XmlNode moviePropertyNode in movieNode.ChildNodes)
                                {
                                    if (moviePropertyNode.Name == @"original_name")
                                    {
                                        movieTitle += moviePropertyNode.InnerText;
                                    }
                                    else if (moviePropertyNode.Name == @"name")
                                    {
                                        if (moviePropertyNode.InnerText != movieTitle)
                                        {
                                            movieTitle += @" (" + moviePropertyNode.InnerText + @")";
                                        }
                                    }
                                    else if ((moviePropertyNode.Name == @"released") && (moviePropertyNode.InnerText.Length >= 4))
                                    {
                                        movieTitle += @" (" + moviePropertyNode.InnerText.Substring(0, 4) + @")";
                                    }
                                }

                                // Add the complete movie title string to the movie list.
                                matches.Add(movieTitle);
                            }
	                    }
                    }
                    else
                    {
                        httpResponseDataString = @"";
                        matchFound             = false;
                    }
                }
            }

            // Return a list of matching movies.
            return matches;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="movieRawData"></param>
        /// <returns></returns>
        private static Dictionary<string, string> GetMovieDetailsInternal(string movieRawData)
        {
            // A dictionary to contain all the movie details to return.
            Dictionary<string, string> movieDetails = new Dictionary<string, string>() { };

            // Initialize dictionary values to blank strings.
            movieDetails["backdrop"] = "";
            movieDetails["cast"]     = "";
            movieDetails["cover"]    = "";
            movieDetails["id"]       = "";
            movieDetails["director"] = "";
            movieDetails["genre"]    = "";
            movieDetails["name"]     = "";
            movieDetails["released"] = "";
            movieDetails["summary"]  = "";
            movieDetails["trailer"]  = "";
            movieDetails["writers"]  = "";
            
            // Movie ID in TheMovieDB.org.
            string movieIdPattern = @"<id>(?<movieId>.+)</id>";
            Match  movieIdMatch   = Regex.Match(movieRawData, movieIdPattern);
            movieDetails["id"]    = Regex.Replace(movieIdMatch.ToString(), movieIdPattern, "${movieId}");
            
            // Movie Name.
            string movieNamePattern            = @"<original_name>(?<movieName>.+)</original_name>";
            Match  movieNameMatch              = Regex.Match(movieRawData, movieNamePattern);
            string movieAlternativeNamePattern = @"<name>(?<movieAlternativeName>.+)</name>";
            Match  movieAlternativeNameMatch   = Regex.Match(movieRawData, movieAlternativeNamePattern);
            string movieName                   = Regex.Replace(movieNameMatch.ToString(), movieNamePattern, "${movieName}");
            string movieAlternativeName        = Regex.Replace(movieAlternativeNameMatch.ToString(), movieAlternativeNamePattern, "${movieAlternativeName}");
            movieDetails["name"]               = movieName;

            // Add an alternative movie name for foreign language titles.
            if (!String.IsNullOrEmpty(movieAlternativeName) && (movieName != movieAlternativeName))
            {
                movieDetails["name"] += @" (" + movieAlternativeName + @")";
            }
            
            // Movie Release Date.
            string movieReleasedPattern = @"<released>(?<movieReleased>.+)</released>";
            Match  movieReleasedMatch   = Regex.Match(movieRawData, movieReleasedPattern);
            movieDetails["released"]    = Regex.Replace(movieReleasedMatch.ToString(), movieReleasedPattern, "${movieReleased}");

            // Movie Summary.
            string movieSummaryPattern = @"<overview>(?<movieSummary>.+)</overview>";
            Match  movieSummaryMatch   = Regex.Match(movieRawData, movieSummaryPattern);
            movieDetails["summary"]    = Regex.Replace(movieSummaryMatch.ToString(), movieSummaryPattern, "${movieSummary}");

            // Movie Image Cover.
            string movieCoverPattern = @"<image type=""poster"" url=""(?<movieCover>.+)"" size=""w342"" width=""342"" height=""(\d+)"" id=""(.+)""/>";
            Match  movieCoverMatch   = Regex.Match(movieRawData, movieCoverPattern);

            // If no official movie cover was found, get a screenshot image.
            if (movieCoverMatch.Length < 1)
            {
                movieCoverPattern = @"<image type=""backdrop"" url=""(?<movieCover>.+)"" size=""thumb"" width=""(\d+)"" height=""(\d+)"" id=""(.+)""/>";
                movieCoverMatch   = Regex.Match(movieRawData, movieCoverPattern);
            }

            string movieCoverUrl  = Regex.Replace(movieCoverMatch.ToString(), movieCoverPattern, "${movieCover}");
            string movieCoverFile = "";

            if (!String.IsNullOrEmpty(movieCoverUrl))
            {
                movieCoverFile = DirectoryManagement.GetRuntimeExecutingPath() + @"\images\Covers\" + movieCoverUrl.Substring(movieCoverUrl.LastIndexOf(@"/") + 1);
            }

            try
            {
                // Download the cover image to the local filesystem.
                if (!String.IsNullOrEmpty(movieCoverUrl) && !File.Exists(movieCoverFile))
                {
                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(movieCoverUrl, movieCoverFile);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  MovieInfo
                        Method: GetMovieDetailsInternal(string movieRawData)
                        Action: webClient.DownloadFile(""" + movieCoverUrl + @""", """ + movieCoverFile + @""")
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            // Save the cover path.
            if (File.Exists(movieCoverFile))
            {
                movieDetails["cover"] = movieCoverFile;
            }
            else
            {
                movieDetails["cover"] = movieCoverUrl;
            }
            
            // Movie Backdrop.
            string movieBackdropPattern = @"<image type=""backdrop"" url=""(?<movieBackdrop>.+)"" size=""original"" width=""(\d+)"" height=""(\d+)"" id=""(.+)""/>";
            Match movieBackdropMatch    = Regex.Match(movieRawData, movieBackdropPattern);
            string movieBackdropUrl     = Regex.Replace(movieBackdropMatch.ToString(), movieBackdropPattern, "${movieBackdrop}");
            string movieBackdropFile    = "";

            if (!String.IsNullOrEmpty(movieBackdropUrl))
            {
                movieBackdropFile = DirectoryManagement.GetRuntimeExecutingPath() + @"\images\Backdrops\" + movieBackdropUrl.Substring(movieBackdropUrl.LastIndexOf(@"/") + 1);
            }

            try
            {
                // Download the backdrop image to the local filesystem.
                if (!String.IsNullOrEmpty(movieBackdropUrl) && !File.Exists(movieBackdropFile))
                {
                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(movieBackdropUrl, movieBackdropFile);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                        Class:  MovieInfo
                        Method: GetMovieDetailsInternal(string movieRawData)
                        Action: webClient.DownloadFile(""" + movieBackdropUrl + @""", """ + movieBackdropFile + @""")
                        
                        " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            // Save the backdrop path.
            if (File.Exists(movieBackdropFile))
            {
                movieDetails["backdrop"] = movieBackdropFile;
            }
            else
            {
                movieDetails["backdrop"] = movieBackdropUrl;
            }

            // Query TMDb.org and return the full movie details page.
            string movieDetailsRawData = Network.DataCommunication.GetHttpWebRequestResponse(@"http://api.themoviedb.org/2.1/Movie.getInfo/en/xml/" + MovieInfo.apiKey + @"/" + movieDetails["id"] + @"");

            // Get the Movie Director.
            string movieDirectorPattern  = @"<person name=""(?<movieDirector>.+)"" character=""(.*)"" job=""Director"" id=""(\d+)"" thumb=""(.*)"" department=""Directing"" url=""(.*)"" order=""(\d+)"" cast_id=""(\d+)""/>";
            Match  movieDirectorMatch    = Regex.Match(movieDetailsRawData, movieDirectorPattern);
            movieDetails["director"]     = Regex.Replace(movieDirectorMatch.ToString(), movieDirectorPattern, "${movieDirector}");

            // Get the Movie Writers.
            string          movieWritersPattern = @"<person name=""(?<movieWriters>.+)"" character=""(.*)"" job=""(.*)"" id=""(\d+)"" thumb=""(.*)"" department=""Writing"" url=""(.*)"" order=""(\d+)"" cast_id=""(\d+)""/>";
            MatchCollection movieWritersMatches = Regex.Matches(movieDetailsRawData, movieWritersPattern);
            string          movieWriters        = "";

            // Seperate each writer by a comma character.
            foreach (Match movieWritersMatch in movieWritersMatches)
            {
                string matchString = Regex.Replace(movieWritersMatch.ToString(), movieWritersPattern, "${movieWriters}");

                if (!movieWriters.Contains(matchString))
                {
                    movieWriters += matchString  + @", ";
                }
            }

            // Strip away the last comma seperator.
            movieDetails["writers"] = movieWriters.Trim().TrimEnd(',');

            // Get the Movie Genre.
            string          movieGenrePattern = @"<category type=""genre"" name=""(?<movieGenre>.+)"" url=""(.*)"" id=""(\d+)""/>";
            MatchCollection movieGenreMatches = Regex.Matches(movieDetailsRawData, movieGenrePattern);
            string          movieGenre        = "";

            // Seperate each writer by a comma character.
            foreach (Match movieGenreMatch in movieGenreMatches)
            {
                string matchString = Regex.Replace(movieGenreMatch.ToString(), movieGenrePattern, "${movieGenre}");

                if (!movieWriters.Contains(matchString))
                {
                    movieGenre += matchString + @", ";
                }
            }

            // Strip away the last comma seperator.
            movieDetails["genre"] = movieGenre.Trim().TrimEnd(',');

            // Get the Movie Cast.
            string          movieCastPattern = @"<person name=""(?<movieCast>.+)"" character=""(.*)"" job=""Actor"" id=""(\d+)"" thumb=""(.*)"" department=""Actors"" url=""(.*)"" order=""(\d+)"" cast_id=""(\d+)""/>";
            MatchCollection movieCastMatches = Regex.Matches(movieDetailsRawData, movieCastPattern);
            string          movieCast        = "";

            // Seperate each writer by a comma character.
            foreach (Match movieCastMatch in movieCastMatches)
            {
                string matchString = Regex.Replace(movieCastMatch.ToString(), movieCastPattern, "${movieCast}");

                if (!movieCast.Contains(matchString))
                {
                    movieCast += matchString + @", ";
                }
            }

            // Strip away the last comma seperator.
            movieDetails["cast"] = movieCast.Trim().TrimEnd(',');

            // Movie Trailer.
            bool   foundTrailer        = false;
            string movieTrailerPattern = @"<trailer>(?<movieTrailer>.+)</trailer>";
            Match  movieTrailerMatch   = Regex.Match(movieDetailsRawData, movieTrailerPattern);
            movieDetails["trailer"]    = Regex.Replace(movieTrailerMatch.ToString(), movieTrailerPattern, "${movieTrailer}");

            // Youtube thumbnail: http://i.ytimg.com/vi/<YOUTUBE_ID>/default.jpg

            // Extract Youtube ID from the URL: "http://www.youtube.com/watch?v=A7CBKT0PWFA" -> "A7CBKT0PWFA"
            string trailerId = movieDetails["trailer"].Replace(@"http://www.youtube.com/watch?v=", "");

            // Get full Youtube Trailer details.
            string trailerDetailsRawData = Network.DataCommunication.GetHttpWebRequestResponse(@"http://www.youtube.com/get_video_info?video_id=" + trailerId + @"&asv=3&el=detailpage&hl=en_US");

            // Remove a prepending string.
            trailerDetailsRawData = trailerDetailsRawData.Replace(@"ttsurl=", "");

            // Split the string into the various URLs.
            string[] trailerDetailsSplit = Regex.Split(trailerDetailsRawData, @"http");

            // Parse each splitted string.
            foreach (string splitString in trailerDetailsSplit)
            {
                if (!foundTrailer)
                {
                    string trailerUrl = splitString;

                    // Replace %25 with % to get the correct ASCII code, example: %252C -> %2C
                    trailerUrl = trailerUrl.Replace(@"%25", @"%");

                    // Replace "%3A" with ":".
                    trailerUrl = trailerUrl.Replace(@"%3A", @":");

                    // Replace "%2F" with "/".
                    trailerUrl = trailerUrl.Replace(@"%2F", @"/");

                    // Replace "%3F" with "?".
                    trailerUrl = trailerUrl.Replace(@"%3F", @"?");

                    // Replace "%3D" with "=".
                    trailerUrl = trailerUrl.Replace(@"%3D", @"=");

                    // Replace "%26" with "&".
                    trailerUrl = trailerUrl.Replace(@"%26", @"&");

                    // Replace "sig=" with "signature=".
                    trailerUrl = trailerUrl.Replace(@"sig=", @"signature=");

                    // Replace "%25" with "%".
                    trailerUrl = trailerUrl.Replace(@"%25", @"%");

                    // Append "http" to the trailer URL.
                    trailerUrl = @"http" + trailerUrl;

                    // Check for valid trailer URLs.
                    if (trailerUrl.Contains(@"r7---sn-") || trailerUrl.Contains(@"r1---sn-") || trailerUrl.Contains(@"r3---sn-"))
                    {
                        foundTrailer = true;

                        movieDetails["trailer"] = trailerUrl;
                    }
                }
            }
            
            // Return the Movie Details.
            return movieDetails;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        private static string GetMovieRawData(string searchString)
        {
            // Main variables.
            bool     matchFound             = false;
            string[] searchArray            = new string[] { };
            string   searchResults          = @"0";
            string   httpResponseDataString = @"";

            // Split the search string word by word.
            // Example: "Back to the Future" => arr[0]="Back", arr[1]="to", arr[2]="the", arr[3]="Future".
            searchArray = searchString.Split(' ');

            // We will search for movies in the following way:
            // Search String: "Back to the Future CD1 XVID" => arr[0]="Back", arr[1]="to", arr[2]="the", arr[3]="Future", arr[4]="CD1", arr[5]="XVID".
            // Search 1: "Back to the Future CD1 XVID" => arr[0]="Back", arr[1]="to", arr[2]="the", arr[3]="Future", arr[4]="CD1", arr[5]="XVID".
            // Search 2: "Back to the Future CD1" => arr[0]="Back", arr[1]="to", arr[2]="the", arr[3]="Future", arr[4]="CD1".
            // Search 3: "Back to the Future" => arr[0]="Back", arr[1]="to", arr[2]="the", arr[3]="Future".
            // Search 4: "Back to the" => arr[0]="Back", arr[1]="to", arr[2]="the".
            // Search 5: "Back to" => arr[0]="Back", arr[1]="to".
            // Search 6: "Back" => arr[0]="Back".
            for (int loopCounter = (searchArray.Length - 1); loopCounter >= 0; loopCounter--)
            {
                // Search only as long as we haven't yet found a match.
                // In the above exmples we need max 6 searches, 
                // but since we have a match on the 3rd search we only need 3 searches.
                if (!matchFound)
                {
                    // Main variables.
                    int    substringLength = 0;
                    string searchSubstring = @"";

                    // Set the correct substring length of the search string based on which iteration of the loop we are currently in.
                    if (loopCounter == (searchArray.Length - 1))
                    {
                        substringLength = searchString.Length;
                    }
                    else if (loopCounter == 0)
                    {
                        substringLength = searchArray[0].Length;
                    }
                    else
                    {
                        substringLength = searchString.LastIndexOf(searchArray[loopCounter + 1]);
                    }

                    // Set the correct search string.
                    searchSubstring = searchString.Substring(0, substringLength);

                    try
                    {
                        if (!String.IsNullOrEmpty(searchSubstring))
                        {
                            // Send an HTTP web request to TheMovieDB.org to search for the specified movie.
                            httpResponseDataString = Network.DataCommunication.GetHttpWebRequestResponse(@"http://api.themoviedb.org/2.1/Movie.search/en/xml/" + MovieInfo.apiKey + @"/" + searchSubstring + @"");
                        }
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                            Class:  MovieInfo
                            Method: GetMovieRawData(""" + searchString + @""")
                            Action: DataCommunication.GetHttpWebRequestResponse(""http://api.themoviedb.org/2.1/Movie.search/en/xml/MovieInfo.apiKey/" + searchSubstring + @""")
                            httpResponseDataString: " + httpResponseDataString + @"
                            
                            " + exception.Message
                            , EventLogEntryType.Error, 123);
                    }

                    // Extract from the result how many movies the search query returned.
                    if (!String.IsNullOrEmpty(httpResponseDataString))
                    {
                        string searchResultsPattern = @"<opensearch:totalResults>(?<searchResults>.+)</opensearch:totalResults>";
                        Match  searchResultsMatch   = Regex.Match(httpResponseDataString, searchResultsPattern);
                        searchResults               = Regex.Replace(searchResultsMatch.ToString(), searchResultsPattern, "${searchResults}");
                    }

                    // Let the iterative loop know that we have found exactly one match so we can break out of the loop.
                    if (!String.IsNullOrEmpty(searchResults) && (searchResults != @"0"))
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

            // Return the Movie details if a movie name was found using the search string.
            // Otherwise return a blank string.
            return httpResponseDataString;
        }

        /// <summary>
        /// Queries TheMovieDB.org and its API for the specified movie.
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns>Returns the movie details.</returns>
        public static Dictionary<string, string> GetMovieDetails(string searchString)
        {
            // Main variables.
            string httpResponseDataString = @"";
            
            // Reserve a dictionary to contain all the movie details to return.
            Dictionary<string, string> movieDetails = new Dictionary<string, string>() { };
            
            // Replace dots and underscores with spaces.
            searchString = MediaInfo.RemoveSpecialCharacters(searchString);

            try 
            {
                // Search for the movie details.
                if (!String.IsNullOrEmpty(searchString))
                {
                    httpResponseDataString = MovieInfo.GetMovieRawData(searchString);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  MovieInfo
                    Method: GetMovieDetails(""" + searchString + @""")
                    Action: MovieInfo.GetMovieRawData(""" + searchString + @""")
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            
            // Return movie details if they were found.
            if (!String.IsNullOrEmpty(httpResponseDataString))
            {
                try
                {
                    if (!String.IsNullOrEmpty(httpResponseDataString))
                    {
                        movieDetails = MovieInfo.GetMovieDetailsInternal(httpResponseDataString);
                    }
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                        Class:  MovieInfo
                        Method: GetMovieDetails(""" + searchString + @""")
                        Action: MovieInfo.GetMovieDetailsInternal(""<httpResponseDataString>"")
                        
                        " + exception.Message
                        , EventLogEntryType.Error, 123);
                }
            }

            // Return the movie details.
            return movieDetails;
        }

    }
}
