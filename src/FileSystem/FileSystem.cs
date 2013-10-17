using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace FileSystem
{
    /// <summary>
    /// Custom methods for File Operations.
    /// </summary>
    public class FileManagement
    {
        // Define private member properties.
        private static List<string> filesInDirectory = new List<string>();

        /// <summary>
        /// Replaces the default System.IO.Directory.GetFiles() method with Recursive option since it
        /// throws System.IO.UnAuthorizedAccessException exceptions and "Access Denied" errors
        /// when parsing directories like "My Documents" because they contain Junction Points.
        /// This static method takes care of checking each file's attributes before returning it.
        /// </summary>
        /// <param name="directoryPath">A string that defines the path of the directory to retrieve files from.</param>
        /// <param name="maxFiles">An integer that defines the maximum amount of files to retrieve.</param>
        /// <returns>Returns a string List of all files found in the directory and sub directories.</returns>
        public static List<string> GetAllFilesInDirectory(string directoryPath, int maxFiles = 1000000)
        {
            // Clear/Empty the list before starting.
            FileManagement.filesInDirectory.Clear();

            // Use the internal helper method to get all files in the directory.
            FileManagement.GetAllFilesInDirectoryInternal(directoryPath, maxFiles);

            // After adding files from the directory and all sub directories, 
            // return the list of files.
            return FileManagement.filesInDirectory;
        }

        /// <summary>
        /// Replaces the default System.IO.Directory.GetFiles() method with Recursive option since it
        /// throws System.IO.UnAuthorizedAccessException exceptions and "Access Denied" errors
        /// when parsing directories like "My Documents" because they contain Junction Points.
        /// This static method takes care of checking each file's attributes before returning it.
        /// </summary>
        /// <param name="directoryPath">A string that defines the path of the directory to retrieve files from.</param>
        /// <param name="maxFiles">An integer that defines the maximum amount of files to retrieve.</param>
        private static void GetAllFilesInDirectoryInternal(string directoryPath, int maxFiles)
        {
            // Get the filesystem and security properties of the root directory.
            DirectoryInfo     directoryInfo        = new DirectoryInfo(directoryPath);
            DirectorySecurity directoryPermissions = new DirectorySecurity();

            try
            {
                // 1. This first part will get a list of all the files in the root directory.

                // Check if the root directory is accessible.
                directoryPermissions = Directory.GetAccessControl(directoryPath);

                // Check if the root directory is a real folder or if it is a Junction/Reparse point.
                if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    // Get a list of all files in the root directory.
                    string[] rootFiles = Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly);

                    // Check each file.
                    foreach (string file in rootFiles)
                    {
                        // Get the filesystem and security properties of the root files.
                        FileInfo     fileInfo        = new FileInfo(file);
                        FileSecurity filePermissions = new FileSecurity();

                        if (FileManagement.filesInDirectory.Count < maxFiles)
                        {
                            try
                            {
                                // Check if the root file is accessible.
                                filePermissions = File.GetAccessControl(file);

                                // Check if the file is a real file or if it is a Junction/Reparse point.
                                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                                {
                                    // Add the file to the final list of files we will return.
                                    FileManagement.filesInDirectory.Add(fileInfo.FullName);
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // Avoid application crashing because of unhandled exceptions.
                            }
                            catch (InvalidOperationException)
                            {
                                // Avoid application crashing because of unhandled exceptions.
                            }
                            catch (PathTooLongException)
                            {
                                // Avoid application crashing because of unhandled exceptions.
                            }
                            catch (Exception)
                            {
                                // Avoid application crashing because of unhandled exceptions.
                            }
                        }
                    }
                }

                // 2. This second part will get a list of all sub directories in the root directory.

                try
                {
                    // Check if the root directory is accessible.
                    directoryPermissions = Directory.GetAccessControl(directoryPath);

                    // Check if the folder is a real folder or if it is a Junction/Reparse point.
                    if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                    {
                        // Get a list of all sub directories in the root directory.
                        string[] subDirectories = Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly);

                        // Check each sub directory.
                        foreach (string subDirectory in subDirectories)
                        {
                            // Get the filesystem and security properties of the sub directories.
                            DirectoryInfo     subDirectoryInfo        = new DirectoryInfo(subDirectory);
                            DirectorySecurity subDirectoryPermissions = new DirectorySecurity();

                            try
                            {
                                // Check if the sub directories are accessible.
                                subDirectoryPermissions = Directory.GetAccessControl(subDirectory);

                                // Check if the folder is a real folder or if it is a Junction/Reparse point.
                                if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                                {
                                    // Recursively check files in each sub directory.
                                    FileManagement.GetAllFilesInDirectoryInternal(subDirectory, maxFiles);
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // Avoid application crashing because of unhandled exceptions.
                            }
                            catch (InvalidOperationException)
                            {
                                // Avoid application crashing because of unhandled exceptions.
                            }
                            catch (PathTooLongException)
                            {
                                // Avoid application crashing because of unhandled exceptions.
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Avoid application crashing because of unhandled exceptions.
                }
                catch (InvalidOperationException)
                {
                    // Avoid application crashing because of unhandled exceptions.
                }
                catch (PathTooLongException)
                {
                    // Avoid application crashing because of unhandled exceptions.
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Avoid application crashing because of unhandled exceptions.
            }
            catch (InvalidOperationException)
            {
                // Avoid application crashing because of unhandled exceptions.
            }
            catch (PathTooLongException)
            {
                // Avoid application crashing because of unhandled exceptions.
            }
            catch (IOException)
            {
                // Avoid application crashing because of unhandled exceptions.
            }
            catch (Exception)
            {
                // Avoid application crashing because of unhandled exceptions.
            }
        }
    }

    /// <summary>
    /// Custom methods for Directory Operations.
    /// </summary>
    public class DirectoryManagement
    {
        // Define private member properties.
        private static List<string> subDirectoriesInDirectory = new List<string>();

        /// <summary>
        /// Replaces the default System.IO.Directory.GetDirectories() method with Recursive option since it
        /// throws System.IO.UnAuthorizedAccessException exceptions and "Access Denied" errors
        /// when parsing directories like "My Documents" because they contain Junction Points.
        /// This static method takes care of checking each file's attributes before returning it.
        /// </summary>
        /// <param name="directoryPath">A string that defines the path of the directory to retrieve sub-directories from.</param>
        /// <param name="maxSubDirectories">An integer that defines the maximum amount of sub-directories to retrieve.</param>
        /// <returns>Returns a string List of all sub-directories found in the directory and sub directories.</returns>
        public static List<string> GetAllSubDirectoriesInDirectory(string directoryPath, int maxSubDirectories = 1000000)
        {
            // Clear/Empty the list before starting.
            DirectoryManagement.subDirectoriesInDirectory.Clear();

            // Use the internal helper method to get all sub-directories in the directory.
            DirectoryManagement.GetAllSubDirectoriesInDirectoryInternal(directoryPath, maxSubDirectories);

            // After adding sub-directories from the directory and all sub-directories, 
            // return the list of sub-directories.
            return DirectoryManagement.subDirectoriesInDirectory;
        }

        /// <summary>
        /// Replaces the default System.IO.Directory.GetDirectories() method with Recursive option since it
        /// throws System.IO.UnAuthorizedAccessException exceptions and "Access Denied" errors
        /// when parsing directories like "My Documents" because they contain Junction Points.
        /// This static method takes care of checking each file's attributes before returning it.
        /// </summary>
        /// <param name="directoryPath">A string that defines the path of the directory to retrieve sub-directories from.</param>
        /// <param name="maxSubDirectories">An integer that defines the maximum amount of sub-directories to retrieve.</param>
        private static void GetAllSubDirectoriesInDirectoryInternal(string directoryPath, int maxSubDirectories)
        {
            // Get the filesystem and security properties of the root directory.
            DirectoryInfo     directoryInfo        = new DirectoryInfo(directoryPath);
            DirectorySecurity directoryPermissions = new DirectorySecurity();

            try
            {
                // Check if the root directory is accessible.
                directoryPermissions = Directory.GetAccessControl(directoryPath);

                // Check if the root directory is a real folder or if it is a Junction/Reparse point.
                if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    if (DirectoryManagement.subDirectoriesInDirectory.Count < maxSubDirectories)
                    {
                        try
                        {
                            // Check if the directory is a real directory or if it is a Junction/Reparse point.
                            if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                            {
                                // Add the directory to the final list of directory we will return.
                                DirectoryManagement.subDirectoriesInDirectory.Add(directoryInfo.FullName);
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Avoid application crashing because of unhandled exceptions.
                        }
                        catch (InvalidOperationException)
                        {
                            // Avoid application crashing because of unhandled exceptions.
                        }
                        catch (PathTooLongException)
                        {
                            // Avoid application crashing because of unhandled exceptions.
                        }
                        catch (Exception)
                        {
                            // Avoid application crashing because of unhandled exceptions.
                        }
                    }
                }

                // Check if the folder is a real folder or if it is a Junction/Reparse point.
                if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    // Get a list of all sub directories in the root directory.
                    string[] subDirectories = Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly);

                    // Check each sub directory.
                    foreach (string subDirectory in subDirectories)
                    {
                        // Get the filesystem and security properties of the sub directories.
                        DirectoryInfo     subDirectoryInfo        = new DirectoryInfo(subDirectory);
                        DirectorySecurity subDirectoryPermissions = new DirectorySecurity();

                        try
                        {
                            // Check if the sub directories are accessible.
                            subDirectoryPermissions = Directory.GetAccessControl(subDirectory);

                            // Check if the folder is a real folder or if it is a Junction/Reparse point.
                            if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                            {
                                // Recursively check directories in each sub directory.
                                DirectoryManagement.GetAllSubDirectoriesInDirectoryInternal(subDirectory, maxSubDirectories);
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Avoid application crashing because of unhandled exceptions.
                        }
                        catch (InvalidOperationException)
                        {
                            // Avoid application crashing because of unhandled exceptions.
                        }
                        catch (PathTooLongException)
                        {
                            // Avoid application crashing because of unhandled exceptions.
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Avoid application crashing because of unhandled exceptions.
            }
            catch (InvalidOperationException)
            {
                // Avoid application crashing because of unhandled exceptions.
            }
            catch (PathTooLongException)
            {
                // Avoid application crashing because of unhandled exceptions.
            }
            catch (IOException)
            {
                // Avoid application crashing because of unhandled exceptions.
            }
            catch (Exception)
            {
                // Avoid application crashing because of unhandled exceptions.
            }
        }

        /// <summary>
        ///  Returns a list of possibly valid Windows Drive Letters the local filesystem can have mapped.
        /// </summary>
        /// <returns>A string List of possibly valid Windows Drive Letters.</returns>
        private static List<string> GetPossibleDriveLetters()
        {
            // Make a list of possibly valid Windows Drive Letters.
            List<string> possibleDriveLetters = new List<string>() 
            { 
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
            };

            // Return a string List of possibly valid Windows Drive Letters.
            return possibleDriveLetters;
        }

        /// <summary>
        /// Returns a string list of network shares in current computer's network/workgroup.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetWindowsNetworkShares()
        {
            // Main variables.
            List<string> networkShares     = new List<string>() { };
            string       netDomainsOutFile = @"data\NetDomains.txt";
            string       netDomainsResult  = @"";
            string       netServersOutFile = @"data\NetServers.txt";
            string       netServersResult  = @"";
            string       netSharesOutFile  = @"data\NetShares.txt";
            string[]     netSharesResult;

            // Run NET VIEW and write the result of all network domains to a text file.
            ProcessManagement.StartHiddenShellProcess(@"NET.exe", @"VIEW /DOMAIN", true, netDomainsOutFile);

            // Wait before continuing.
            //Thread.Sleep(500);

            if (File.Exists(netDomainsOutFile))
            {
                // Read the result into a text string.
                netDomainsResult = File.ReadAllText(netDomainsOutFile);
            
                // Find all network domains in the text string using regular expression.
                string          networkDomainPattern = @"(?<networkDomain>\S+)\s+";
                MatchCollection networkDomainMatches = Regex.Matches(netDomainsResult, networkDomainPattern);
            
                // Iterate each network domain in the list.
                foreach (Match networkDomainMatch in networkDomainMatches)
                {
                    // Extract the exact network domain name from each match.
                    // Example: "DOM123 " => "DOM123"
                    string networkDomain = Regex.Replace(networkDomainMatch.ToString(), networkDomainPattern, "${networkDomain}").Trim();
                
                    // Ignore default shares and descriptive text that resulted from the NET VIEW command.
                    if (
                        !String.IsNullOrEmpty(networkDomain) &&
                        (networkDomain != @"Domain") &&
                        (networkDomain != @"The") &&
                        (networkDomain != @"command") &&
                        (networkDomain != @"completed") &&
                        (networkDomain != @"successfully.") &&
                        (!networkDomain.Contains(@"----------"))
                    ) {
                        // Run NET VIEW and write the result of all network servers to a text file.
                        ProcessManagement.StartHiddenShellProcess(@"NET.exe", String.Format(@"VIEW /DOMAIN:{0}", networkDomain), true, netServersOutFile);

                        // Wait before continuing.
                        //Thread.Sleep(500);

                        if (File.Exists(netServersOutFile))
                        {
                            // Read the result into a text string.
                            netServersResult = File.ReadAllText(netServersOutFile);

                            // Find all network servers in the text string using regular expression.
                            string          networkServerPattern = @"(?<networkServer>\\\\\S+)\s+";
                            MatchCollection networkServerMatches = Regex.Matches(netServersResult, networkServerPattern);
                    
                            // Iterate each network server in the list.
                            foreach (Match networkServerMatch in networkServerMatches)
                            {
                                // Extract the exact network server name from each match.
                                // Example: "\\SERVER " => "\\SERVER"
                                string networkServer = Regex.Replace(networkServerMatch.ToString(), networkServerPattern, "${networkServer}").Trim();

                                // Run NET VIEW and write the result of all network shares at the current server to a text file.
                                ProcessManagement.StartHiddenShellProcess(@"NET.exe", @"VIEW " + networkServer + @"", true, netSharesOutFile);

                                // Wait before continuing.
                                //Thread.Sleep(500);

                                if (File.Exists(netSharesOutFile))
                                {
                                    // Read the result into a text string.
                                    netSharesResult = File.ReadAllLines(netSharesOutFile);

                                    foreach (string netShareResultLine in netSharesResult)
                                    {
                                        // Find all network shares at the current server in the text string using regular expression.
                                        string networkSharesPattern = @"(?<networkShares>\S+)\s+Disk";
                                        Match  networkSharesMatch   = Regex.Match(netShareResultLine, networkSharesPattern);

                                        // Extract the exact network server name from each match.
                                        // Example: "Share " => "Share"
                                        string networkShareMatch = Regex.Replace(networkSharesMatch.ToString(), networkSharesPattern, "${networkShares}").Trim();

                                        // Ignore default shares and descriptive text that resulted from the NET VIEW command.
                                        if (
                                            !String.IsNullOrEmpty(networkShareMatch) &&
                                            (networkShareMatch != @"NETLOGON") &&
                                            (networkShareMatch != @"Share") &&
                                            (networkShareMatch != @"Shared") &&
                                            (networkShareMatch != @"System") &&
                                            (networkShareMatch != @"SYSVOL") &&
                                            (networkShareMatch != @"The") &&
                                            (networkShareMatch != @"There")
                                        ) {
                                            string networkShare = @"" + networkServer + @"\" + networkShareMatch + @"";

                                            // Add the network share to our list if it's valid.
                                            if (!networkShares.Contains(networkShare))
                                            {
                                                networkShares.Add(networkShare);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // Delete temporary data files.
            if (File.Exists(netDomainsOutFile))
            {
                File.Delete(netDomainsOutFile);
            }
            
            if (File.Exists(netDomainsOutFile))
            {
                File.Delete(netServersOutFile);
            }
            
            if (File.Exists(netDomainsOutFile))
            {
                File.Delete(netSharesOutFile);
            }

            // Return the list of network shares.
            return networkShares;
        }

        /// <summary>
        /// Returns a list of system directories in the local windows file system that can throw I/O errors.
        /// </summary>
        /// <returns>A string List of Windows system directories.</returns>
        private static List<string> GetWindowsSystemDirectories()
        {
            // Make a list of system directories.
            List<string> systemDirectories = new List<string>() { 
                @"$Recycle.Bin", 
                @"Config.Msi", 
                @"Documents and Settings", 
                @"MSOCache", 
                @"PerfLogs", 
                @"ProgramData", 
                @"Recovery", 
                @"System Volume Information",
                @"Application Data",
                @"Cookies", 
                @"Local Settings", 
                @"My Documents", 
                @"My Music", 
                @"My Pictures", 
                @"My Videos", 
                @"NetHood", 
                @"PrintHood", 
                @"SendTo", 
                @"Start Menu", 
                @"Templates"
            };

            // Return the list of system directories.
            return systemDirectories;
        }

        /// <summary>
        /// Checks if the directory is a Windows system directory.
        /// </summary>
        /// <param name="directoryPath">The directory path to validate.</param>
        /// <returns>A Boolean: True or False.</returns>
        public static bool IsWindowsSystemDirectory(string directoryPath)
        {
            // Main variables.
            bool directoryIsSystemDirectory = false;
            List<string> systemDirectories = new List<string>() { };

            // Get a string List of possibly valid Windows Drive Letters.
            List<string> PossibleDriveLetters = DirectoryManagement.GetPossibleDriveLetters();

            // Make a list of system directories in the local windows file system that can throw I/O errors.
            systemDirectories = DirectoryManagement.GetWindowsSystemDirectories();

            // Check if the directory is a Windows system directory.
            foreach (string systemDirectory in systemDirectories)
            {
                foreach (string driveLetter in PossibleDriveLetters)
                {
                    // Check the directory against all possible windows drive letters.
                    if ((driveLetter + @":\\" + systemDirectory).ToLower() == directoryPath.ToLower())
                    {
                        directoryIsSystemDirectory = true;
                    }
                    else if ((driveLetter + @":\" + systemDirectory).ToLower() == directoryPath.ToLower())
                    {
                        directoryIsSystemDirectory = true;
                    }
                    else if (Regex.IsMatch(directoryPath, @"\w:\W+Users\W+(\S|\s)+\W+" + systemDirectory))
                    {
                        directoryIsSystemDirectory = true;
                    }
                    else if (Regex.IsMatch(directoryPath, @"\w:\W+Users\W+(\S|\s)+\W+Documents\W+" + systemDirectory))
                    {
                        directoryIsSystemDirectory = true;
                    }
                }
            }

            // Return the Boolean.
            return directoryIsSystemDirectory;
        }

        /// <summary>
        /// Returns the path from where current code is called from after it has been compiled and deployed.
        /// So if the code that calls this method is compiled into "C:\deploy\execute.exe",
        /// this method will return "C:\deploy", if it's later moved to "C:\new_dir\execute.exe",
        /// it will return "C:\new_dir".
        /// </summary>
        /// <returns>Returns a string representing the Runtime Executing Path.</returns>
        public static string GetRuntimeExecutingPath()
        {
            // Main variables.
            string runtimeExecutingPath = @"";

            // Get the current runtime executing path.
            runtimeExecutingPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

            // Return the Runtime Executing Path.
            if (runtimeExecutingPath.IndexOf(@"\htdocs\cgi-bin") >= 0)
            {
                return runtimeExecutingPath.Substring(6, runtimeExecutingPath.IndexOf(@"\htdocs\cgi-bin") - 6);
            }
            else
            {
                return runtimeExecutingPath.Substring(6);
            }
        }

    }

    /// <summary>
    /// Custom methods for Process Operations.
    /// </summary>
    public class ProcessManagement
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="processExecutable"></param>
        public static void StartHiddenShellProcess(string processExecutable, string argumentsString = @"", bool outputToFile = false, string fileNameAndPath = @"")
        {
            // Prepare to run a hidden shell process in the background.
            ProcessStartInfo process = new ProcessStartInfo(processExecutable);
            process.Arguments        = argumentsString;
            process.UseShellExecute  = true;
            process.CreateNoWindow   = true;
            process.WindowStyle      = ProcessWindowStyle.Hidden;

            if (outputToFile && !String.IsNullOrEmpty(fileNameAndPath))
            {
                // Make Output Redirection possible.
                process.UseShellExecute        = false;
                process.RedirectStandardOutput = true;

                // Run the process and write the output to the inputted file.
                File.WriteAllText(fileNameAndPath, Process.Start(process).StandardOutput.ReadToEnd());
            }
            else
            {
                // Run the process.
                Process.Start(process);
            }
        }

    }

}
