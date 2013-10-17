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

namespace Network
{
    /// <summary>
    /// Handles data transfer over network protocols.
    /// </summary>
    public static class DataCommunication
    {
        // Private class member properties.
        private static ResourceManager resourceManager = new ResourceManager("Network.Properties.Resources", Assembly.GetExecutingAssembly());

        /// <summary>
        /// Escapes the special characters created when passing strings through the HTTP POST Protocol.
        /// </summary>
        /// <param name="unescapedPath">A string representing the original directory path.</param>
        /// <returns>An escaped version of the original directory path.</returns>
        public static string EscapeDirectoryPathFromHttpPost(string unescapedPath)
        {
            // Main variables.
            string escapedPath = @"";

            // Since the HTTP Protocol replaces spaces with "+" instead of "%2B",
            // it will create problems for directory names with "+" in them.
            // Therefore we will first replace "%2B" with a replacement holder called "__PLUS__".
            escapedPath = Regex.Replace(unescapedPath, @"%2B", @"__PLUS__");

            // Remove strange escape characters created by the HTTP Protocol and double-escape backslash characters ("\").
            escapedPath = Regex.Replace(Uri.UnescapeDataString(escapedPath), @"(\\)+", @"\\");

            // Replace the problematic plus characters ("+") with real spaces (" ").
            escapedPath = Regex.Replace(escapedPath, @"\+", @" ");

            // Change our "__PLUS__" replacement holders back to real plus characters ("+").
            escapedPath = Regex.Replace(escapedPath, @"__PLUS__", @"+");

            return escapedPath;
        }

        /// <summary>
        /// Checks if there are any stored HTTP POST data in the windows system environment variables, 
        /// and returns the POST data if it's found.
        /// </summary>
        /// <returns>A string containing the HTTP POST data.</returns>
        public static string GetHttpPostData()
        {
            // Main variables.
            string postData = "";

            // Check if this application was accessed using the POST HTTP Request Method.
            if (System.Environment.GetEnvironmentVariable(@"REQUEST_METHOD") == @"POST")
            {
                // Get the length/size of the POST Data.
                int PostDataLength = Convert.ToInt32(System.Environment.GetEnvironmentVariable(@"CONTENT_LENGTH"));

                // Set the Max length/size for POST Data to 2 KB.
                if (PostDataLength > 2048)
                {
                    PostDataLength = 2048;
                }

                // Read the POST Data one character at a time into the postData string.
                for (int dataIterator = 0; dataIterator < PostDataLength; dataIterator++)
                {
                    postData += Convert.ToChar(Console.Read()).ToString();
                }
            }

            // Return the HTTP POST data.
            return postData;
        }

        /// <summary>
        /// Queries a Web Service using the specified URL and returns the HTML or XML result from the response.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetHttpWebRequestResponse(string url)
        {
            // Main variables.
            string          httpResponseDataString = @"";
            HttpWebRequest  httpRequest            = null;
            HttpWebResponse httpResponse           = null;
            StreamReader    httpResponseData       = null;
            
            // Create a Windows Event Log Source.
            if (!EventLog.SourceExists(resourceManager.GetString("application_title")))
            {
                EventLog.CreateEventSource(resourceManager.GetString("application_title"), "Application");
            }

            // Get the HTTP Web Response of the request.
            if (!String.IsNullOrEmpty(url))
            {
                try
                {
                    // Query the specified URL by sending an HTTP GET Request.
                    httpRequest             = HttpWebRequest.Create(url) as HttpWebRequest;
                    httpRequest.ContentType = "text/xml; charset=utf-8";
                    httpRequest.Method      = "GET";
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                            Class:  DataCommunication
                            Method: GetHttpWebRequestResponse(""" + url + @""")
                            Action: HttpWebRequest.Create(""" + url + @""")
                            
                            " + exception.Message
                        , EventLogEntryType.Error, 123);
                }
                
                try
                {
                    httpResponse = httpRequest.GetResponse() as HttpWebResponse;
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                            Class:  DataCommunication
                            Method: GetHttpWebRequestResponse(""" + url + @""")
                            Action: httpRequest.GetResponse()
                            
                            " + exception.Message
                        , EventLogEntryType.Error, 123);
                }
            }

            try
            {
                // Read the HTTP web response into a stream reader.
                if (httpResponse != null)
                {
                    httpResponseData = new StreamReader(httpResponse.GetResponseStream(), true);
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                            Class:  DataCommunication
                            Method: GetHttpWebRequestResponse(""" + url + @""")
                            Action: new StreamReader(httpResponse.GetResponseStream(), true)
                            
                            " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            try
            {
                // Read the stream reader stream into a string.
                if (httpResponseData != null)
                {
                    httpResponseDataString = httpResponseData.ReadToEnd();
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                            Class:  DataCommunication
                            Method: GetHttpWebRequestResponse(""" + url + @""")
                            Action: httpResponseData.ReadToEnd()
                            
                            " + exception.Message
                    , EventLogEntryType.Error, 123);
            }

            // Close and Dispose of open resources.
            httpResponseData.Close();
            httpResponse.Close();

            // Return the web request response as a string.
            return httpResponseDataString;
        }

    }
}
