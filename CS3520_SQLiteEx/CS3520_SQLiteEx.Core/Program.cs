using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace CS3520_SQLiteEx.Core
{
  internal class Program
  {
    /// <summary>
    /// Private static Random Generator - used to help randomize some aspects of this program, to see differences between runs.
    /// </summary>
    private static Random generator;

    private static void Main(string[] args)
    {
      generator = new Random();
      string dbNameToUse = "TestStudentDB"; // DatabaseName of TestStudentDB (*.SQLITE is automatically added to the end)
      string directoryToEmplaceDBWithin = string.Empty; // Executing directory. Let's hope we have write access to exectuing directory!

      // 1. Initialize DB
      InitializeDB(dbNameToUse, directoryToEmplaceDBWithin);
      Console.WriteLine("Done. Press any key to run next step.");
      Console.ReadKey();
      Console.WriteLine();
      Console.WriteLine();

      // 2. Run SQL Queries against DB

      // A: Run sample query that simply retrieves all data from db, and print it.
      Console.WriteLine("Running Query 1 - Select all data, no filter, no sort.");
      RunQueryAndPrintArbitraryResults(
        ReadResourceFileByName("001SelectStarFromStudentsSimple.sql"),
        dbNameToUse,
        directoryToEmplaceDBWithin);
      Console.WriteLine("Done. Press any key to run next step.");
      Console.ReadKey();
      Console.WriteLine();
      Console.WriteLine();

      // B: Run Sample query with some data selected, where gpa >= 3.0, ordered by GPA DESC.
      Console.WriteLine("Running Query 2 - Select name, gpa, where GPA >= 3.0, sorted by GPA DESC.");
      RunQueryAndPrintArbitraryResults(
        ReadResourceFileByName("002SelectNameGPAFromStudentsWhereGPASortByGPA.sql"),
        dbNameToUse,
        directoryToEmplaceDBWithin);
      Console.WriteLine("Done. Press any key to run next step.");
      Console.ReadKey();
      Console.WriteLine();
      Console.WriteLine();

      // C: Run sample query with some data selected, top 10, ordered by birthdate asc.
      Console.WriteLine("Running Query 3 - Select name, BirthDate, LIMIT 10, sorted by BirthDate ASC.");
      RunQueryAndPrintArbitraryResults(
        ReadResourceFileByName("003SelectNameBirthdayFromStudentsWhereLimit10GPAASC.sql"),
        dbNameToUse,
        directoryToEmplaceDBWithin);
      Console.WriteLine("Done. Press any key to run next step.");
      Console.ReadKey();
      Console.WriteLine();
      Console.WriteLine();

      // 3. Delete DB.
      DeleteDB(dbNameToUse, directoryToEmplaceDBWithin);

      // Await user to press a key to terminate the program.
      Console.WriteLine();
      Console.WriteLine("Press Any Key to Terminate.");
      Console.ReadKey();
    }

    /// <summary>
    /// Initializes a SQLITE Database by the name / directory provided.
    /// Creates it, generates a table, and fills the table with 100 dummy rows of data.
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="dir">The directory of the database (optional - assumes executing directory)</param>
    private static void InitializeDB(string dbName, string dir = "")
    {
      Console.WriteLine("InitializeDB");

      string combinedPath = Path.Combine(dir, dbName);
      combinedPath = Path.ChangeExtension(combinedPath, ".sqlite");
      Console.WriteLine("DB File Name: {0}", combinedPath);

      if (File.Exists(combinedPath))
      {
        // If we are told to initialize the DB, let's delete it first.
        Console.WriteLine("Detected DB already exists - deleting");
        DeleteDB(dbName, dir);
      }

      Console.WriteLine("Creating SQLite Database");

      // 1. Create the Database, with the file name specified.
      SQLiteConnection.CreateFile(combinedPath);

      Console.WriteLine("Instantiating Student Table in database");

      // 2. Run a SQL Query to instantiate a table
      // Note: This is wrapped in a using statement, to ensure the IDbConnection is disposed of - ALWAYS.
      // Also, note that we stuff a SQLiteConnection into an IDbConnection. This allows us to more easily swap
      // underlying DBMS's within C#, as any well formulated Database-implementation should adhere to the
      // IDbConnection, IDbCommand, IDataReader semantics / interfaces, allowing DBMS-agnostic C#-layer code.
      // (However, this will still need to have different SQL queries depending upon the variant of SQL used).
      using (IDbConnection con = new SQLiteConnection(string.Format("Data Source={0};Version=3;", combinedPath))) // 2.A: Create a Connection to the database.
      {
        // Open the connection to the database.
        con.Open();

        using (IDbCommand cmd = con.CreateCommand()) // 2.B: Create a Command object, to be able to interact with the database.
        {
          // Set the command timeout to 60 seconds. The default Value for MS SQL is 30s, I believe...
          // We're using SQLIte, which I'm unsure if command timeouts even have a meaningful place, since
          // the query is executed against the local disk, rather than potentially a remote server...
          // I've never tested SQLite with command timeouts.
          // If CommandTimeout == 0, there is no timeout, and an infinite amount of time is allowed to lapse.
          cmd.CommandTimeout = 60;

          cmd.CommandText = @"
CREATE TABLE Students
(
  ID INTEGER PRIMARY KEY,
  Name VARCHAR(50),
  Address VARCHAR(300),
  PhoneNumber VARCHAR(12),
  GPA DECIMAL(15,4),
  BirthDate DATETIME
)";

          // Execute the SQL Statement in Non-Query mode.
          // This doesn't return an IDataReader, and is meant for modifying
          // data, without retrieving any tuples from SQL.
          // Note: This will return total # of rows affected, in the event that rows are affected.
          cmd.ExecuteNonQuery();
        } // End scope for cmd; it gets disposed.
      } // End scope for con; it gets disposed.

      Console.WriteLine("Inserting Data into Database");

      // 3. Insert 100 items into the table.
      using (IDbConnection con = new SQLiteConnection(string.Format("Data Source={0};Version=3;", combinedPath)))
      {
        con.Open();

        using (IDbCommand cmd = con.CreateCommand())
        {
          cmd.CommandTimeout = 60;

          cmd.CommandText = @"
INSERT INTO Students
(Name, Address, PhoneNumber, GPA, BirthDate)
VALUES
(@NameParameter, @AddressParameter, @PhoneNumberParameter, @GPAParameter, @BirthDateParameter)";

          for (int i = 0; i < 100; i++)
          {
            // Note: Rather than using literals, or string-formatted literals,
            // we will use SQL Command Parameterization, which "sanitizes" the SQL input, to (hypothetically) avoid SQL Injection.
            // That's what the @-stuff in the VALUES section of the query represents.

            cmd.Parameters.Clear(); // Clear pre-existing parameters, since we are reusing an existing IDbCommand.

            // Use Static Helper Extension Method to add command parameter, to elliminate some boiler plate code.
            cmd.AddCommandParameter(DbType.String, "@NameParameter", "Student" + i);

            // Note below that we ommitted the "@" character at the front of the paramter name;
            // this is fine in MS SQL and SQLITE.
            cmd.AddCommandParameter(DbType.String, "AddressParameter", string.Format("6{0} Some Road Some City Some State Some Zip Some Country", i.ToString().PadLeft(3, '0')));
            cmd.AddCommandParameter(DbType.String, "PhoneNumberParameter", "+1-801-800-9" + i.ToString().PadLeft(3, '0'));
            cmd.AddCommandParameter(DbType.Decimal, "GPAParameter", (decimal)generator.Next(1000, 4000) / 1000.0m); // Randomly generate a GPA between 1.0 and 4.0 inclusive.
            cmd.AddCommandParameter(DbType.DateTime, "BirthDateParameter", new DateTime(
              generator.Next(1980, 2007), // Allow datetimes between 1980, and 2006 inclusive. (Random is exclusive on upper bound, in C#).
              generator.Next(1, 13), // Allow datetime between months 1, and 12 inclusive.
              generator.Next(1, 29))); // Allow day between 1 and 28 inclusive (that way we don't worry about months with differing max # of days)

            // Execute nonquery again, as we don't need to read results, we're just storing data in the database.
            cmd.ExecuteNonQuery();
          }
        }
      }
    }

    /// <summary>
    /// Deletes the SQLITE Database by the specified name (and optional directory).
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="dir">The directory of the database (optional - assumes executing directory)</param>
    private static void DeleteDB(string dbName, string dir = "")
    {
      Console.WriteLine("DeleteDB");

      string combinedPath = Path.Combine(dir, dbName);
      combinedPath = Path.ChangeExtension(combinedPath, ".sqlite");

      Console.WriteLine("DB File Name: {0}", combinedPath);

      if (!File.Exists(combinedPath))
      {
        throw new Exception(string.Format("No database file existed by the name of {0}; Failed to delete it.", combinedPath));
      }

      File.Delete(combinedPath);
    }

    /// <summary>
    /// Runs a provided Query and prints arbitrary results to console window.
    /// Note: Will not support command parameterization, but instead, will run any arbitrary query thrown at it.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="dir">The directory of the database (optional - assumes executing directory)</param>
    private static void RunQueryAndPrintArbitraryResults(string query, string dbName, string dir = "")
    {
      ConsoleColor archivedColor = Console.ForegroundColor;

      Console.WriteLine("RunQueryAndPrintArbitraryResults");

      string combinedPath = Path.Combine(dir, dbName);
      combinedPath = Path.ChangeExtension(combinedPath, ".sqlite");
      Console.WriteLine("DB File Name: {0}", combinedPath);

      if (!File.Exists(combinedPath))
      {
        throw new Exception(string.Format("Database already instantiated at {0}", combinedPath));
      }

      Console.WriteLine("Executing Query:");
      Console.WriteLine();
      Console.ForegroundColor = ConsoleColor.Blue;
      Console.WriteLine(query);
      Console.ForegroundColor = archivedColor;
      Console.WriteLine("Press any key to print results:");
      Console.ReadKey();
      Console.WriteLine();

      using (IDbConnection con = new SQLiteConnection(string.Format("Data Source={0};Version=3;", combinedPath)))
      {
        con.Open();

        using (IDbCommand cmd = con.CreateCommand())
        {
          cmd.CommandTimeout = 60;
          cmd.CommandText = query;

          using (IDataReader rdr = cmd.ExecuteReader()) // Execute the command with a Returned IDataReader, so we can enumerate over the results.
          {
            Console.WriteLine("Returned # of columns: {0:N0}", rdr.FieldCount);
            
            // Investigate the reader for a listing of every column
            for (int i = 0; i < rdr.FieldCount; i++)
            {
              if (i != 0)
              {
                Console.Write(","); // add commas if we're not on the 0th iteration.
              }

              Console.Write("{");
              Console.ForegroundColor = ConsoleColor.Green;
              Console.Write(rdr.GetName(i));
              Console.ForegroundColor = archivedColor;
              Console.Write("_");
              Console.ForegroundColor = ConsoleColor.Green;
              Console.Write(rdr.GetFieldType(i).Name);
              Console.ForegroundColor = archivedColor;
              Console.Write("}");
            }

            Console.WriteLine();

            while (rdr.Read()) // Pump the IDataReader.Read() method to set the next row. (Must be invoked at least once before we begin our current read, at least, as per IDataReader standards).
            {
              // Investigate the reader for values. Read as objects, regardless of type, and print their ToString() to the window.
              for (int i = 0; i < rdr.FieldCount; i++)
              {
                if (i != 0)
                {
                  Console.Write(","); // add commas if we're not on the 0th iteration.
                }

                Console.Write(rdr.GetValue(i).ToString());
              }

              Console.WriteLine();
            }
          }
        }
      }

      Console.ForegroundColor = archivedColor;
    }

    /// <summary>
    /// Private Helper-function to read a resource file by name, from the project's embedded resources folder for SQL Queries.
    /// </summary>
    /// <param name="resourceFileName">The name of the resource file to read.</param>
    /// <returns>A string representing the embedded resource file to read.</returns>
    private static string ReadResourceFileByName(string resourceFileName)
    {
      Console.WriteLine("Reading Resource: {0}", resourceFileName);

      // Use Reflection to retrieve the entry assembly.
      Assembly assembly = Assembly.GetEntryAssembly();

      // Instantiate a stream that (hopefully) points to the resource being retrieved, by file name.
      Stream resourceStream = assembly.GetManifestResourceStream(string.Format("CS3520_SQLiteEx.Core.SQLQueries.{0}", resourceFileName));

      // Read the resource.
      using (StreamReader rdr = new StreamReader(resourceStream))
      {
        return rdr.ReadToEnd();
      }
    }
  }
}