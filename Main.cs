using System.Globalization;
using CodingTracker.TwilightSaw;
using Microsoft.Data.Sqlite;
using Spectre.Console;
using SQLitePCL;
using System.Xml.Linq;
using Dapper;
using System;

UserInput userInput = new UserInput();

var end = true;

raw.SetProvider(new SQLite3Provider_e_sqlite3());
using var connection = new SqliteConnection(Config.connection);
connection.Open();

CodingSession session = new CodingSession();
TrackerController controller = new TrackerController(connection);

var createTableQuery = @$"CREATE TABLE IF NOT EXISTS 'Tracker' (
    Id INTEGER PRIMARY KEY,
    Date TEXT,
    StartTime TEXT,
    EndTime TEXT
    )";

connection.Execute(createTableQuery);

while (end)
{
    AnsiConsole.Markup(@"[blue]Welcome to the Coding Tracker![/]
                    
Please, choose an option from the list below:
1. Start a Coding Session.
2. Change an existed Coding Session.
3. Show your Coding Sessions.
4. Delete a Coding Session.
5. Exit

");

    Console.Write("Type your choice: ");
    switch (userInput.CreateInt())
    {
        case 1:
            var dateInputC = DateTime.Now;
            session.StartTime = DateTime.Parse(dateInputC.ToShortDateString() + " " + dateInputC.ToLongTimeString());
            controller.SetTimer();

            Console.WriteLine("Press any key to stop the timer.");
            Console.ReadKey();

            controller.StopTimer();


            dateInputC = DateTime.Now;
            session.EndTime = DateTime.Parse(dateInputC.ToShortDateString() + " " + dateInputC.ToLongTimeString());
            controller.Create(connection, session);
            break;
        case 2:
            Console.Write("Type data of your Coding Session: ");
            var dateInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$",
                                "Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            var x = userInput.ChooseSession(controller.Read(connection, dateInput));
            /*Console.Write("Type your previous Session start time: ");
            var previousTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$|^N|n$",
                                "Wrong data format, try again. Example: 10:10:10 or N for today's date: ");*/
            Console.Write("Type your new Start Session start time: ");
            var startTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$|^N|n$",
                                "Wrong data format, try again. Example: 10:10:10 or N for today's date: ");
           // Console.Write("Add end time of your Coding Session: ");
            //var endTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$",
             //                   "Wrong data format, try again. Example: 12:12:12: ");
            session.StartTime = DateTime.Parse(dateInput + " " + startTimeInput);
            //session.EndTime = DateTime.Parse(dateInput + " " + endTimeInput);
            controller.Update(connection, session, x.StartTime.ToLongTimeString());
            break;
        case 3:
            controller.Read(connection);
            break;
        case 4:
            Console.Write("Type data of your Coding Session you chose to delete: ");
            var deleteDateInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$",
                "Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            controller.Delete(connection, deleteDateInput);
            break;
        case 5:
            end = false;
            break;
        default:
            Console.WriteLine("Wrong choice selection, try again! \n");
            break;
    }
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
    Console.Clear();
}