using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace Database
{
    /// <summary>
    /// Performs SQL queries with the Database.
    /// Supports inserting, updating, deleting and retrieving data.
    /// Takes care of connection handling (opening/closing).
    /// </summary>
    public class SQL
    {
        // Define private member properties.
        private ResourceManager             resourceManager = new ResourceManager("Database.Properties.Resources", Assembly.GetExecutingAssembly());
        private SqlCeConnection             sqlConnection;
        private string                      databaseName;
        private string                      sqlConnectionString;
        private string                      sqlQuery;
        private Dictionary<string, string>  sqlCommandParameters;
        private Dictionary<string, string>  sqlWhereParameters;
        private string tableName;
        
        // Define public member properties.
        public string SqlConnectionString
        {
            get { return sqlConnectionString; }
            set { sqlConnectionString = value; }
        }

        public string SqlQuery
        {
            get { return sqlQuery; }
            set { sqlQuery = value; }
        }

        public Dictionary<string, string> SqlCommandParameters
        {
            get { return sqlCommandParameters; }
            set { sqlCommandParameters = value; }
        }

        public Dictionary<string, string> SqlWhereParameters
        {
            get { return sqlWhereParameters; }
            set { sqlWhereParameters = value; }
        }

        public string TableName
        {
            get { return tableName; }
            set { tableName = value; }
        }

        // DatabaseType structure.
        public struct DatabaseType
        {
            public const string MediaLibrary   = @"\data\MediaLibrary.sdf";
            public const string MediaOrganizer = @"\data\MediaOrganizer.sdf";
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="databaseType">An optional constant string of the Database.SQL.DatabaseType structure.</param>
        public SQL(string databaseType = DatabaseType.MediaLibrary)
        {
            // Set the name and path of the database.
            this.databaseName = this.GetRuntimeExecutingPath() + databaseType;

            // Set the name of the new database.
            this.SqlConnectionString = @"Data Source=" + databaseName + ";Persist Security Info=False;Max Database Size=4000";

            // Initialize member properties.
            this.SqlCommandParameters = new Dictionary<string, string>() { };
            this.SqlWhereParameters   = new Dictionary<string, string>() { };

            // Create a Windows Event Log Source.
            if (!EventLog.SourceExists(resourceManager.GetString("application_title")))
            {
                EventLog.CreateEventSource(resourceManager.GetString("application_title"), "Application");
            }

            // Create the necessary database structure if not already done.
            this.InitializeDatabases(databaseType);
        }

        /// <summary>
        /// Returns the max length of the database table column.
        /// </summary>
        /// <param name="tableName">Name of the Database Table.</param>
        /// <param name="tableName">Name of the Database Column.</param>
        /// <returns>An Int: Max length of the column, or -1 if the column was not found or does not have the max char length property.</returns>
        public int GetDatabaseColumnMaxLength(string tableName, string columnName)
        {
            // Main variables.
            DataTable databaseTableDataMediaLibrary   = new DataTable();
            DataTable databaseTableDataMediaOrganizer = new DataTable();

            // Save the original connection string.
            string originalConnectionString = this.SqlConnectionString;

            // Set the SQL query.
            string sql = @"
                SELECT CHARACTER_MAXIMUM_LENGTH 
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME='" + tableName + @"' AND COLUMN_NAME='" + columnName + @"'
            ";

            // Set the table name.
            this.tableName = tableName;

            // Set the connection string for MediaLibrary.
            this.SqlConnectionString = @"Data Source=" + this.GetRuntimeExecutingPath() + DatabaseType.MediaLibrary + ";Persist Security Info=False;Max Database Size=4000";

            // Build the SQL Query string for MediaLibrary.
            this.SqlQuery = sql;

            // Try to retrieve the data for MediaLibrary.
            databaseTableDataMediaLibrary = this.GetData();

            // Set the connection string for MediaOrganizer.
            this.SqlConnectionString = @"Data Source=" + this.GetRuntimeExecutingPath() + DatabaseType.MediaOrganizer + ";Persist Security Info=False;Max Database Size=4000";

            // Build the SQL Query string for MediaOrganizer.
            this.SqlQuery = sql;

            // Try to retrieve the data for MediaOrganizer.
            databaseTableDataMediaOrganizer = this.GetData();

            // Restore the original connection string.
            this.SqlConnectionString = originalConnectionString;

            // Return the max length of the column.
            if (databaseTableDataMediaLibrary.Rows.Count > 0)
            {
                try
                {
                    return Convert.ToInt32(databaseTableDataMediaLibrary.Rows[0][0].ToString());
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(
                        resourceManager.GetString("application_title"),
                        @"
                        Class:  SQL
                        Method: GetDatabaseColumnMaxLength(" + tableName + @", " + columnName + @")
                        Action: Convert.ToInt32(" + databaseTableDataMediaLibrary.Rows[0][0].ToString() + @");
                    
                        " + exception.Message,
                        EventLogEntryType.Error, 
                        123
                    );

                    return -1;
                }
            }
            if (databaseTableDataMediaOrganizer.Rows.Count > 0)
            {
                try
                {
                    return Convert.ToInt32(databaseTableDataMediaOrganizer.Rows[0][0].ToString());
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(
                        resourceManager.GetString("application_title"),
                        @"
                        Class:  SQL
                        Method: GetDatabaseColumnMaxLength(" + tableName + @", " + columnName + @")
                        Action: Convert.ToInt32(" + databaseTableDataMediaOrganizer.Rows[0][0].ToString() + @");
                    
                        " + exception.Message,
                        EventLogEntryType.Error, 
                        123
                    );

                    return -1;
                }
            }
            // Otherwise return -1.
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Checks if the database table Column exists in the current database scheme.
        /// </summary>
        /// <param name="tableName">Name of the Database Table.</param>
        /// <param name="tableName">Name of the Database Column.</param>
        /// <returns>A Boolean: True or False.</returns>
        public bool DatabaseColumnExists(string tableName, string columnName)
        {
            // Main variables.
            DataTable databaseTableDataMediaLibrary   = new DataTable();
            DataTable databaseTableDataMediaOrganizer = new DataTable();

            // Save the original connection string.
            string originalConnectionString = this.SqlConnectionString;

            // Set the SQL query.
            string sql = @"
                SELECT * 
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME='" + tableName + @"' AND COLUMN_NAME='" + columnName + @"'
            ";

            // Set the table name.
            this.tableName = tableName;
            
            // Set the connection string for MediaLibrary.
            this.SqlConnectionString = @"Data Source=" + this.GetRuntimeExecutingPath() + DatabaseType.MediaLibrary + ";Persist Security Info=False;Max Database Size=4000";
            
            // Build the SQL Query string for MediaLibrary.
            this.SqlQuery = sql;

            // Try to retrieve the data for MediaLibrary.
            databaseTableDataMediaLibrary = this.GetData();

            // Set the connection string for MediaOrganizer.
            this.SqlConnectionString = @"Data Source=" + this.GetRuntimeExecutingPath() + DatabaseType.MediaOrganizer + ";Persist Security Info=False;Max Database Size=4000";

            // Build the SQL Query string for MediaOrganizer.
            this.SqlQuery = sql;

            // Try to retrieve the data for MediaOrganizer.
            databaseTableDataMediaOrganizer = this.GetData();

            // Restore the original connection string.
            this.SqlConnectionString = originalConnectionString;

            // Return True if the column exists.
            if ((databaseTableDataMediaLibrary.Rows.Count > 0) || (databaseTableDataMediaOrganizer.Rows.Count > 0))
            {
                return true;
            }
            // Otherwise return False.
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the database Table exists in the current database scheme.
        /// </summary>
        /// <param name="tableName">Name of the Database Table.</param>
        /// <returns>A Boolean: True or False.</returns>
        public bool DatabaseTableExists(string tableName)
        {
            // Main variables.
            DataTable databaseTableDataMediaLibrary   = new DataTable();
            DataTable databaseTableDataMediaOrganizer = new DataTable();

            // Save the original connection string.
            string originalConnectionString = this.SqlConnectionString;

            // Set the SQL query.
            string sql = @"
                SELECT * 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME='" + tableName + @"'
            ";

            // Set the table name.
            this.tableName = tableName;

            // Set the connection string for MediaLibrary.
            this.SqlConnectionString = @"Data Source=" + this.GetRuntimeExecutingPath() + DatabaseType.MediaLibrary + ";Persist Security Info=False;Max Database Size=4000";

            // Build the SQL Query string for MediaLibrary.
            this.SqlQuery = sql;

            // Try to retrieve the data for MediaLibrary.
            databaseTableDataMediaLibrary = this.GetData();

            // Set the connection string for MediaOrganizer.
            this.SqlConnectionString = @"Data Source=" + this.GetRuntimeExecutingPath() + DatabaseType.MediaOrganizer + ";Persist Security Info=False;Max Database Size=4000";

            // Build the SQL Query string for MediaOrganizer.
            this.SqlQuery = sql;

            // Try to retrieve the data for MediaOrganizer.
            databaseTableDataMediaOrganizer = this.GetData();

            // Restore the original connection string.
            this.SqlConnectionString = originalConnectionString;

            // Return True if the table exists.
            if ((databaseTableDataMediaLibrary.Rows.Count > 0) || (databaseTableDataMediaOrganizer.Rows.Count > 0))
            {
                return true;
            }
            // Otherwise return False.
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Drops/Deletes the Database Table.
        /// </summary>
        /// <param name="tableName">Name of the Database Table.</param>
        public void DropDatabaseTable(string tableName)
        {
            // Build the SQL Query string.
            this.SqlQuery = @"DROP TABLE " + tableName + @"";

            // Drop/Delete the table.
            this.ExecuteSql();
        }

        /// <summary>
        /// Executes the SQL Command.
        /// 
        /// Requires the following member properties to be set:
        /// SqlQuery               String      - Defines the Database SQL Query.
        /// </summary>
        public void ExecuteSql()
        {
            // Define main variables.
            SqlCeCommand sqlCommand;

            // Connect to the database.
            this.InitConnection();

            // Open a new database connection.
            this.sqlConnection.Open();

            // Try to insert data into a database table, and catch possible errors the method can throw.
            try
            {
                // Make sure the required Sql Query string is set.
                if (!string.IsNullOrEmpty(this.SqlQuery))
                {
                    // Create an SQL Ce Command object which we will use to execute an SQL query.
                    sqlCommand = new SqlCeCommand(this.SqlQuery, this.sqlConnection);
                }
                // Otherwise fail with a descriptive error.
                else
                {
                    sqlCommand = null;
                }

                // Execute the SQL Command object using the above assigned query.
                if (sqlCommand != null)
                {
                    try
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                            Class:  SQL
                            Method: ExecuteSql()
                            Action: sqlCommand.ExecuteNonQuery()
                            SQL:    " + sqlCommand.CommandText + @"
                    
                            " + exception.Message
                            , EventLogEntryType.Error, 123);
                    }
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                            Class:  SQL
                            Method: ExecuteSql()
                            Action: sqlCommand = new SqlCeCommand(""" + this.SqlQuery + @""", """ + this.sqlConnection.ConnectionString + @""")
                    
                            " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            // Make sure we close the connection after completing the operation.
            finally
            {
                sqlConnection.Close();
            }
        }

        /// <summary>
        /// Returns the path from where current code is called from after it has been compiled and deployed.
        /// So if the code that calls this method is compiled into "C:\deploy\execute.exe",
        /// this method will return "C:\deploy", if it's later moved to "C:\new_dir\execute.exe",
        /// it will return "C:\new_dir".
        /// </summary>
        /// <returns>Returns a string representing the Runtime Executing Path.</returns>
        public string GetRuntimeExecutingPath()
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetCurrentDate()
        {
            string year   = "";
            string month  = "";
            string day    = "";
            string hour   = "";
            string minute = "";
            string second = "";
            string offset = "";

            if (DateTimeOffset.Now.Year < 10)
            {
                year = "0" + DateTimeOffset.Now.Year.ToString();
            }
            else
            {
                year = DateTimeOffset.Now.Year.ToString();
            }

            if (DateTimeOffset.Now.Month < 10)
            {
                month = "0" + DateTimeOffset.Now.Month.ToString();
            }
            else
            {
                month = DateTimeOffset.Now.Month.ToString();
            }

            if (DateTimeOffset.Now.Day < 10)
            {
                day = "0" + DateTimeOffset.Now.Day.ToString();
            }
            else
            {
                day = DateTimeOffset.Now.Day.ToString();
            }

            if (DateTimeOffset.Now.Hour < 10)
            {
                hour = "0" + DateTimeOffset.Now.Hour.ToString();
            }
            else
            {
                hour = DateTimeOffset.Now.Hour.ToString();
            }

            if (DateTimeOffset.Now.Minute < 10)
            {
                minute = "0" + DateTimeOffset.Now.Minute.ToString();
            }
            else
            {
                minute = DateTimeOffset.Now.Minute.ToString();
            }

            if (DateTimeOffset.Now.Second < 10)
            {
                second = "0" + DateTimeOffset.Now.Second.ToString();
            }
            else
            {
                second = DateTimeOffset.Now.Second.ToString();
            }

            if (DateTimeOffset.Now.Offset.Hours > 0)
            {
                offset = "UTC+" + DateTimeOffset.Now.Offset.Hours.ToString();
            }
            else
            {
                offset = "UTC" + DateTimeOffset.Now.Offset.Hours.ToString();
            }

            return year + "-" + month + "-" + day + " " + hour + ":" + minute + ":" + second + " " + offset;
        }

        /// <summary>
        /// Retrieves data from a database table.
        /// 
        /// Requires the following member properties to be set:
        /// SqlConnectionString    String      - Defines the SQL Database Connection String.
        /// SqlQuery               String      - Defines the Database SQL Query.
        /// TableName              String      - Defines the Database Table Name.
        /// </summary>
        /// <returns>The DataSet and all the retrieved data records as a DataTable object.</returns>
        public DataTable GetData()
        {
            // Connect to the database.
            this.InitConnection();

            // Define main variables.
            SqlCeDataAdapter sqlTableAdapter = null;
            DataSet sqlDataSet = null;

            // Open a new database connection.
            if (sqlConnection.State == ConnectionState.Closed)
            {
                this.sqlConnection.Open();
            }

            // Try to retrieve data from a database table, 
            // and catch possible errors the method can throw.
            try
            {
                // Make sure the required SQL Query is not blank.
                if ((this.SqlQuery != "") && (this.SqlQuery != null))
                {
                    // Create an SQL Ce Adapter object which we will use to execute an SQL query for retrieval.
                    sqlTableAdapter = new SqlCeDataAdapter(this.SqlQuery, this.sqlConnection);
                }

                // Create an SQL DataSet object which will hold the retrieved data records.
                sqlDataSet = new DataSet();

                // Fill the DataSet with the retrieved data records,
                // and assign the DataSet to the SQL Adapter.
                sqlTableAdapter.Fill(sqlDataSet, this.TableName);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                            Class:  SQL
                            Method: GetData()
                            Action: sqlTableAdapter.Fill(""<sqlDataSet>"", """ + this.TableName + @""")
                    
                            " + this.SqlQuery + @"
                            " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            // Make sure we close the connection after completing the operation.
            finally
            {
                sqlConnection.Close();
            }

            // Return the DataSet and all the retrieved data records.
            return sqlDataSet.Tables[this.TableName];
        }

        /// <summary>
        /// Initializes and builds a connection to the Database.
        /// A private method used internally by the SQL Class methods.
        ///
        /// Requires the following member properties to be set:
        /// SqlConnectionString     String  - Defines the SQL Database Connection String.
        /// </summary>
        private void InitConnection()
        {
            // Build the connection as long as the connection string has been set.
            if ((this.SqlConnectionString != "") && (this.SqlConnectionString != null))
            {
                this.sqlConnection = new SqlCeConnection(this.SqlConnectionString);
            }
        }

        /// <summary>
        /// Initializes and creates all needed databases for this application.
        /// This method is run once when the application is opened/started.
        /// </summary>
        /// <param name="databaseType">An optional constant string of the Database.SQL.DatabaseType structure.</param>
        public void InitializeDatabases(string databaseType = DatabaseType.MediaLibrary)
        {
            // Define main variables.
            SqlCeEngine  sqlEngine;

            // Try to create the new database, and catch possible errors the method can throw.
            if (!File.Exists(this.databaseName))
            {
                // Build an SQL Ce Engine using the above data source string.
                // The Engine object will be used later to create the new database.
                sqlEngine = new SqlCeEngine(this.SqlConnectionString);

                try
                {
                    // Create the database.
                    sqlEngine.CreateDatabase();
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                            Class:  SQL
                            Method: InitializeDatabases()
                            Action: sqlEngine.CreateDatabase()
                    
                            " + exception.Message
                        , EventLogEntryType.Error, 123);
                }
            }

            // Try to create a new table in the new database, and catch possible errors the method can throw.
            if (File.Exists(this.databaseName))
            {
                try
                {
                    // Create a Test Table to use for Unit Testing.
                    if (!this.DatabaseTableExists(@"test"))
                    {
                        this.sqlQuery = @"
                        CREATE TABLE test (
                            id int NOT NULL IDENTITY (1, 1), 
                            unique_id uniqueidentifier, 
                            title nvarchar(256) NOT NULL, 
                            media_type nvarchar(256), 
                            PRIMARY KEY (id), 
                            UNIQUE (id)
                        )
                        ";

                        this.ExecuteSql();
                    }

                    if (databaseType == DatabaseType.MediaOrganizer)
                    {
                        // Create the Synchronizer Status Table.
                        if (!this.DatabaseTableExists(@"sync_status"))
                        {
                            this.sqlQuery = @"
                            CREATE TABLE sync_status (
                                pkid uniqueidentifier NOT NULL DEFAULT NEWID(), 
                                id int NOT NULL IDENTITY (1, 1), 
                                job_id uniqueidentifier NOT NULL, 
                                date nvarchar(50) NOT NULL, 
                                directory nvarchar(256) NOT NULL, 
                                action nvarchar(4000) NOT NULL,
                                PRIMARY KEY (pkid), 
                                UNIQUE (pkid)
                            )
                            ";

                            this.ExecuteSql();
                        }

                        // Create the Disabled Movie Details Table.
                        if (!this.DatabaseTableExists(@"disabled_movie_details"))
                        {
                            this.sqlQuery = @"
                            CREATE TABLE disabled_movie_details (
                                pkid uniqueidentifier NOT NULL DEFAULT NEWID(), 
                                id int NOT NULL IDENTITY (1, 1), 
                                path nvarchar(256) NOT NULL, 
                                filename nvarchar(256), 
                                PRIMARY KEY (pkid), 
                                UNIQUE (pkid)
                            )
                            ";

                            this.ExecuteSql();
                        }
                    }
                    else if (databaseType == DatabaseType.MediaLibrary)
                    {
                        // Create the Audio Table.
                        if (!this.DatabaseTableExists(@"audio"))
                        {
                            this.sqlQuery = @"
                            CREATE TABLE audio (
                                pkid uniqueidentifier NOT NULL DEFAULT NEWID(), 
                                id int NOT NULL IDENTITY (1, 1), 
                                unique_id uniqueidentifier NOT NULL, 
                                filename nvarchar(256) NOT NULL, 
                                path nvarchar(256) NOT NULL, 
                                file_type nvarchar(4) NOT NULL, 
                                file_size nvarchar(25), 
                                duration nvarchar(25),
                                audio nvarchar (256), 
                                cover image,
                                artist nvarchar(256), 
                                track nvarchar(256), 
                                album nvarchar(256), 
                                genre nvarchar(256), 
                                release_year nvarchar(25), 
                                tag nvarchar(256),
                                completed int DEFAULT 0,
                                last_played_date nvarchar(50),
                                last_played_duration int DEFAULT 0,
                                nr_of_times_played int DEFAULT 0,
                                PRIMARY KEY (pkid), 
                                UNIQUE (pkid)
                            )
                            ";

                            this.ExecuteSql();

                            // Create an ascending sorted index for the filename column.
                            if (this.DatabaseColumnExists(@"audio", @"filename"))
                            {
                                this.sqlQuery = @"CREATE INDEX idx_audio_filename_SortAscending ON audio (filename ASC)";
                                this.ExecuteSql();
                            }

                            // Create an ascending sorted index for the path column.
                            if (this.DatabaseColumnExists(@"audio", @"path"))
                            {
                                this.sqlQuery = @"CREATE INDEX idx_audio_path_SortAscending ON audio (path ASC)";
                                this.ExecuteSql();
                            }
                        }

                        // Create the Pictures Table.
                        if (!this.DatabaseTableExists(@"pictures"))
                        {
                            this.sqlQuery = @"
                            CREATE TABLE pictures (
                                pkid uniqueidentifier NOT NULL DEFAULT NEWID(), 
                                id int NOT NULL IDENTITY (1, 1), 
                                unique_id uniqueidentifier NOT NULL, 
                                filename nvarchar(256) NOT NULL, 
                                path nvarchar(256) NOT NULL, 
                                file_type nvarchar(4) NOT NULL, 
                                file_size nvarchar(25), 
                                dimensions nvarchar(25),
                                comment nvarchar(4000),
                                tag nvarchar(256),
                                last_played_date nvarchar(50),
                                nr_of_times_played int DEFAULT 0,
                                PRIMARY KEY (pkid), 
                                UNIQUE (pkid)
                            )
                            ";

                            this.ExecuteSql();

                            // Create an ascending sorted index for the filename column.
                            if (this.DatabaseColumnExists(@"pictures", @"filename"))
                            {
                                this.sqlQuery = @"CREATE INDEX idx_pictures_filename_SortAscending ON pictures (filename ASC)";
                                this.ExecuteSql();
                            }

                            // Create an ascending sorted index for the path column.
                            if (this.DatabaseColumnExists(@"pictures", @"path"))
                            {
                                this.sqlQuery = @"CREATE INDEX idx_pictures_path_SortAscending ON pictures (path ASC)";
                                this.ExecuteSql();
                            }
                        }

                        // Create the Videos Table.
                        if (!this.DatabaseTableExists(@"videos"))
                        {
                            this.sqlQuery = @"
                            CREATE TABLE videos (
                                pkid uniqueidentifier NOT NULL DEFAULT NEWID(), 
                                id int NOT NULL IDENTITY (1, 1), 
                                unique_id uniqueidentifier NOT NULL, 
                                filename nvarchar(256) NOT NULL, 
                                path nvarchar(256) NOT NULL, 
                                file_type nvarchar(4) NOT NULL, 
                                file_size nvarchar(25), 
                                duration nvarchar(25), 
                                audio nvarchar(256),
                                video nvarchar(256),
                                tag nvarchar(256),
                                completed int DEFAULT 0,
                                last_played_date nvarchar(50),
                                last_played_duration int DEFAULT 0,
                                nr_of_times_played int DEFAULT 0,
                                movie_details_id nvarchar(25),
                                movie_details_backdrop nvarchar(256),
                                movie_details_cast nvarchar(1024),
                                movie_details_cover nvarchar(256),
                                movie_details_director nvarchar(256),
                                movie_details_genre nvarchar(256),
                                movie_details_name nvarchar(256),
                                movie_details_released nvarchar(25),
                                movie_details_summary nvarchar(4000),
                                movie_details_trailer nvarchar(1024),
                                movie_details_writers nvarchar(1024),
                                episode_details_airdate nvarchar(25),
                                episode_details_cast nvarchar(1024),
                                episode_details_cover nvarchar(256),
                                episode_details_director nvarchar(256),
                                episode_details_genre nvarchar(256),
                                episode_details_name nvarchar(256),
                                episode_details_next_airdate nvarchar(25),
                                episode_details_next_number nvarchar(25),
                                episode_details_next_title nvarchar(256),
                                episode_details_number nvarchar(25),
                                episode_details_summary nvarchar(4000),
                                episode_details_title nvarchar(256),
                                episode_details_writers nvarchar(1024),
                                PRIMARY KEY (pkid), 
                                UNIQUE (pkid)
                            )
                            ";

                            this.ExecuteSql();

                            // Create an ascending sorted index for the filename column.
                            if (this.DatabaseColumnExists(@"videos", @"filename"))
                            {
                                this.sqlQuery = @"CREATE INDEX idx_videos_filename_SortAscending ON videos (filename ASC)";
                                this.ExecuteSql();
                            }

                            // Create an ascending sorted index for the path column.
                            if (this.DatabaseColumnExists(@"videos", @"path"))
                            {
                                this.sqlQuery = @"CREATE INDEX idx_videos_path_SortAscending ON videos (path ASC)";
                                this.ExecuteSql();
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                            Class:  SQL
                            Method: InitializeDatabases()
                            Action: Creating default tables and indexes.
                    
                            " + exception.Message
                        , EventLogEntryType.Error, 123);
                }
                // Make sure we close the connection after completing the operation.
                finally
                {
                    sqlConnection.Close();
                }

                try
                {
                    // Alter the table structures.
                    if (databaseType == DatabaseType.MediaOrganizer)
                    {
                        // Change the action column from varbinary to nvarchar.
                        if (this.GetDatabaseColumnMaxLength(@"sync_status", @"action") > 4000)
                        {
                            this.sqlQuery = @"ALTER TABLE sync_status ALTER COLUMN action nvarchar(4000)";
                            this.ExecuteSql();
                        }
                    }

                    // Alter the table structures.
                    if (databaseType == DatabaseType.MediaLibrary)
                    {
                        // Extend the max characters of the file_type column.
                        if (this.GetDatabaseColumnMaxLength(@"audio", @"file_type") < 4)
                        {
                            this.sqlQuery = @"ALTER TABLE audio ALTER COLUMN file_type nvarchar(4) NOT NULL";
                            this.ExecuteSql();
                        }

                        // Extend the max characters of the file_type column.
                        if (this.GetDatabaseColumnMaxLength(@"pictures", @"file_type") < 4)
                        {
                            this.sqlQuery = @"ALTER TABLE pictures ALTER COLUMN file_type nvarchar(4) NOT NULL";
                            this.ExecuteSql();
                        }

                        // Change the comment column from varbinary to nvarchar.
                        if (this.GetDatabaseColumnMaxLength(@"pictures", @"comment") > 4000)
                        {
                            this.sqlQuery = @"ALTER TABLE pictures ALTER COLUMN comment nvarchar(4000)";
                            this.ExecuteSql();
                        }

                        // Extend the max characters of the file_type column.
                        if (this.GetDatabaseColumnMaxLength(@"videos", @"file_type") < 4)
                        {
                            this.sqlQuery = @"ALTER TABLE videos ALTER COLUMN file_type nvarchar(4) NOT NULL";
                            this.ExecuteSql();
                        }
                        
                        // Extend the max characters of the movie_details_writers column.
                        if (this.GetDatabaseColumnMaxLength(@"videos", @"movie_details_writers") < 1024)
                        {
                            this.sqlQuery = @"ALTER TABLE videos ALTER COLUMN movie_details_writers nvarchar(1024)";
                            this.ExecuteSql();
                        }

                        // Extend the max characters of the episode_details_writers column.
                        if (this.GetDatabaseColumnMaxLength(@"videos", @"episode_details_writers") < 1024)
                        {
                            this.sqlQuery = @"ALTER TABLE videos ALTER COLUMN episode_details_writers nvarchar(1024)";
                            this.ExecuteSql();
                        }
                        
                        // Change the movie_details_summary column from varbinary to nvarchar.
                        if (this.GetDatabaseColumnMaxLength(@"videos", @"movie_details_summary") > 4000)
                        {
                            this.sqlQuery = @"ALTER TABLE videos ALTER COLUMN movie_details_summary nvarchar(4000)";
                            this.ExecuteSql();
                        }

                        // Change the episode_details_summary column from varbinary to nvarchar.
                        if (this.GetDatabaseColumnMaxLength(@"videos", @"episode_details_summary") > 4000)
                        {
                            this.sqlQuery = @"ALTER TABLE videos ALTER COLUMN episode_details_summary nvarchar(4000)";
                            this.ExecuteSql();
                        }
                        
                        // Add a movie_details_cast column.
                        if (!this.DatabaseColumnExists(@"videos", @"movie_details_cast"))
                        {
                            this.sqlQuery = @"ALTER TABLE videos ADD movie_details_cast nvarchar(1024)";
                            this.ExecuteSql();
                        }

                        // Add an episode_details_cast column.
                        if (!this.DatabaseColumnExists(@"videos", @"episode_details_cast"))
                        {
                            this.sqlQuery = @"ALTER TABLE videos ADD episode_details_cast nvarchar(1024)";
                            this.ExecuteSql();
                        }

                        // Add a movie_details_trailer column.
                        if (!this.DatabaseColumnExists(@"videos", @"movie_details_trailer"))
                        {
                            this.sqlQuery = @"ALTER TABLE videos ADD movie_details_trailer nvarchar(1024)";
                            this.ExecuteSql();
                        }
                    }
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry(resourceManager.GetString("application_title"),
                        @"
                            Class:  SQL
                            Method: InitializeDatabases()
                            Action: Altering the table structure.
                    
                            " + exception.Message
                        , EventLogEntryType.Error, 123);
                }
                // Make sure we close the connection after completing the operation.
                finally
                {
                    sqlConnection.Close();
                }
            }
        }

        /// <summary>
        /// Inserts data into a database table.
        /// 
        /// Requires the following member properties to be set:
        /// SqlCommandParameters   Dictionary  - Optional String,String Dictionary containing Query Parameters.
        /// TableName              String      - Defines the Database Table Name.
        /// </summary>
        public void InsertData()
        {
            // Connect to the database.
            this.InitConnection();

            // Define main variables.
            SqlCeCommand sqlCommand;

            // Open a new database connection.
            if (sqlConnection.State == ConnectionState.Closed)
            {
                this.sqlConnection.Open();
            }

            // Try to insert data into a database table, 
            // and catch possible errors the method can throw.
            try
            {
                // Make sure the required Sql Command Parameters and Table Name are set.
                if (
                    (this.SqlCommandParameters != null) && 
                    (this.SqlCommandParameters.Count > 0) && 
                    (!string.IsNullOrEmpty(this.TableName))
                )
                {
                    // Build the SQL Query.
                    // Final SQL query will some something like: "INSERT INTO table_name (col1, col2) VALUES (@col1, @col2)".
                    // And at the end the parameters @col1 and @col2 will be replaced with 'val1' and 'val2'.
                    this.SqlQuery = @"INSERT INTO " + this.TableName + @" (";

                    // Initialize the loop counter.
                    int loopCounter = 0;

                    // Build the table column key part of the SQL query.
                    foreach (string key in this.SqlCommandParameters.Keys)
                    {
                        if (loopCounter != SqlCommandParameters.Keys.Count - 1)
                        {
                            this.SqlQuery += key + @", ";
                        }
                        else
                        {
                            this.SqlQuery += key;
                        }

                        loopCounter++;
                    }

                    this.SqlQuery += @") VALUES (";

                    loopCounter = 0;

                    // Build the table column value part of the SQL query.
                    foreach (string key in this.SqlCommandParameters.Keys)
                    {
                        if (loopCounter != SqlCommandParameters.Keys.Count - 1)
                        {
                            this.SqlQuery += @"@" + key + @", ";
                        }
                        else
                        {
                            this.SqlQuery += @"@" + key;
                        }

                        loopCounter++;
                    }

                    this.SqlQuery += @")";

                    // Create an SQL Ce Command object which we will use to execute an SQL query.
                    sqlCommand = new SqlCeCommand(this.SqlQuery, this.sqlConnection);

                    // Replace @-parameters in the query with actual values.
                    foreach (KeyValuePair<string, string> parameter in this.SqlCommandParameters)
                    {
                        try
                        {
                            /*
                            if (
                                (parameter.Key == @"cover") ||
                                (parameter.Key == @"movie_details_summary") || 
                                (parameter.Key == @"episode_details_summary") || 
                                (parameter.Key == @"comment") || 
                                (parameter.Key == @"action")
                            )
                            */
                            if (parameter.Key == @"cover")
                            {
                                sqlCommand.Parameters.AddWithValue(@"@" + parameter.Key, System.Text.UnicodeEncoding.Unicode.GetBytes(parameter.Value));
                            }
                            else
                            {
                                sqlCommand.Parameters.AddWithValue(@"@" + parameter.Key, parameter.Value);
                            }
                        }
                        catch (Exception exception)
                        {
                            EventLog.WriteEntry(resourceManager.GetString("application_title"),
                                @"
                                Class:  SQL
                                Method: InsertData()
                                Action: sqlCommand.Parameters.AddWithValue(""" + parameter.Key + @""", """ + parameter.Value + @""")
                    
                                " + exception.Message
                                , EventLogEntryType.Error, 123);
                        }
                    }
                }
                // Otherwise fail with a descriptive error.
                else
                {
                    sqlCommand = null;
                }

                // Execute the SQL Command object using the above assigned query.
                if (sqlCommand != null)
                {
                    try
                    {
                        //Console.WriteLine(sqlCommand.CommandText);
                        sqlCommand.ExecuteNonQuery();
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                            Class:  SQL
                            Method: InsertData()
                            Action: sqlCommand.ExecuteNonQuery()
                            SQL: " + sqlCommand.CommandText + @"
                            
                            " + exception.Message
                            , EventLogEntryType.Error, 123);
                    }
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SQL
                    Method: InsertData()
                    Action: Insert record into database table.
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            // Make sure we close the connection after completing the operation.
            finally
            {
                sqlConnection.Close();
            }
        }

        /// <summary>
        /// Updates existing data in a database table.
        /// 
        /// Requires the following member properties to be set:
        /// TableName              String      - Defines the Database Table Name.
        /// SqlCommandParameters   Dictionary  - Optional Dictionary<String,String> containing Query Parameters.
        /// SqlWhereParameters     Dictionary  - Optional Dictionary<String,String> containing WHERE clause Parameters.
        /// </summary>
        /// <param name="whereSearch">If true, will make a "key LIKE '%string%'" match instead of a "key='val'" match.</param>
        public void UpdateData(bool whereSearch = false)
        {
            // Connect to the database.
            this.InitConnection();

            // Define main variables.
            SqlCeCommand sqlCommand;

            // Open a new database connection.
            if (sqlConnection.State == ConnectionState.Closed)
            {
                this.sqlConnection.Open();
            }

            // Try to insert data into a database table, 
            // and catch possible errors the method can throw.
            try
            {
                // Make sure the required Sql Command Parameters and Table Name are set.
                if (
                    (this.SqlCommandParameters != null) &&
                    (this.SqlCommandParameters.Count > 0) &&
                    (!string.IsNullOrEmpty(this.TableName))
                )
                {
                    // Build the SQL Query.
                    // Final SQL query will be some something like: "UPDATE table_name col1=@col1, col2=@col2 WHERE col0=@col0".
                    // And at the end the parameters @col1 and @col2 will be replaced with 'val1' and 'val2' etc.
                    this.SqlQuery = @"UPDATE " + this.TableName + @" SET ";

                    // Initialize the loop counter.
                    int loopCounter = 0;

                    // Build the table column key part of the SQL query.
                    foreach (string key in this.SqlCommandParameters.Keys)
                    {
                        if (loopCounter != SqlCommandParameters.Keys.Count - 1)
                        {
                            this.SqlQuery += key + @"=@" + key + @", ";
                        }
                        else
                        {
                            this.SqlQuery += key + @"=@" + key;
                        }

                        loopCounter++;
                    }

                    this.SqlQuery += @" WHERE ";

                    // Initialize the loop counter.
                    loopCounter = 0;

                    // Filter which keys and values will be updated using a WHERE clause.
                    foreach (string key in this.SqlWhereParameters.Keys)
                    {
                        if (loopCounter != SqlWhereParameters.Keys.Count - 1)
                        {
                            if (whereSearch)
                            {
                                this.SqlQuery += key.Replace(@"where_", "") + @" LIKE @" + key + @" AND ";
                            }
                            else
                            {
                                this.SqlQuery += key.Replace(@"where_", "") + @"=@" + key + @" AND ";
                            }
                        }
                        else
                        {
                            if (whereSearch)
                            {
                                this.SqlQuery += key.Replace(@"where_", "") + @" LIKE @" + key + @"";
                            }
                            else
                            {
                                this.SqlQuery += key.Replace(@"where_", "") + @"=@" + key + @"";
                            }
                        }

                        loopCounter++;
                    }

                    // Create an SQL Ce Command object which we will use to execute an SQL query.
                    sqlCommand = new SqlCeCommand(this.SqlQuery, this.sqlConnection);

                    // Replace @-parameters in the query with actual values.
                    foreach (KeyValuePair<string, string> parameter in this.SqlCommandParameters)
                    {
                        try
                        {
                            /*
                            if (
                                (parameter.Key == @"cover") ||
                                (parameter.Key == @"movie_details_summary") ||
                                (parameter.Key == @"episode_details_summary") ||
                                (parameter.Key == @"comment") ||
                                (parameter.Key == @"action")
                            )
                            */
                            if (parameter.Key == @"cover")
                            {
                                sqlCommand.Parameters.AddWithValue(@"@" + parameter.Key, System.Text.UnicodeEncoding.Unicode.GetBytes(parameter.Value));
                            }
                            else
                            {
                                sqlCommand.Parameters.AddWithValue(@"@" + parameter.Key, parameter.Value);
                            }
                        }
                        catch (Exception exception)
                        {
                            EventLog.WriteEntry(resourceManager.GetString("application_title"),
                                @"
                                Class:  SQL
                                Method: UpdateData()
                                Action: sqlCommand.Parameters.AddWithValue(""" + parameter.Key + @""", """ + parameter.Value + @""")
                    
                                " + exception.Message
                                , EventLogEntryType.Error, 123);
                        }
                    }
                    
                    // Replace @-parameters in the WHERE clause with actual values.
                    foreach (KeyValuePair<string, string> parameter in this.SqlWhereParameters)
                    {
                        try
                        {
                            if (whereSearch)
                            {
                                sqlCommand.Parameters.AddWithValue(@"@" + parameter.Key, @"%" + parameter.Value + @"%");
                            }
                            else
                            {
                                sqlCommand.Parameters.AddWithValue(@"@" + parameter.Key, parameter.Value);
                            }
                        }
                        catch (Exception exception)
                        {
                            EventLog.WriteEntry(resourceManager.GetString("application_title"),
                                @"
                                Class:  SQL
                                Method: UpdateData()
                                Action: sqlCommand.Parameters.AddWithValue(""" + parameter.Key + @""", """ + parameter.Value + @""")
                    
                                " + exception.Message
                                , EventLogEntryType.Error, 123);
                        }
                    }
                }
                else
                {
                    sqlCommand = null;
                }

                // Execute the SQL Command object using the above assigned query.
                if (sqlCommand != null)
                {
                    try
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                    catch (Exception exception)
                    {
                        EventLog.WriteEntry(resourceManager.GetString("application_title"),
                            @"
                            Class:  SQL
                            Method: UpdateData()
                            Action: sqlCommand.ExecuteNonQuery()
                            SQL: " + sqlCommand.CommandText + @"
                    
                            " + exception.Message
                            , EventLogEntryType.Error, 123);
                    }
                }
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry(resourceManager.GetString("application_title"),
                    @"
                    Class:  SQL
                    Method: UpdateData()
                    Action: Updating existing record in the database table.
                    
                    " + exception.Message
                    , EventLogEntryType.Error, 123);
            }
            // Make sure we close the connection after completing the operation.
            finally
            {
                sqlConnection.Close();
            }
        }

        /// <summary>
        /// Deletes the specified data entry in a database table.
        /// 
        /// Requires the following member properties to be set:
        /// TableName              String      - Defines the Database Table Name.
        /// </summary>
        /// <param name="primaryKeyId">The Primary Key ID of the table record.</param>
        public void DeleteData(string primaryKeyId)
        {
            // Make sure the table name and the primary key ID is set.
            if (!string.IsNullOrEmpty(this.TableName) && !string.IsNullOrEmpty(primaryKeyId))
            {
                // Build the SQL Query.
                this.SqlQuery = @"DELETE FROM " + this.TableName + @" WHERE pkid = '" + primaryKeyId + @"'";

                // Execute the SQL Query.
                this.ExecuteSql();
            }
        }

    }
}
