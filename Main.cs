using CodingTracker.TwilightSaw;
using Dapper;
using Microsoft.Data.Sqlite;
using Spectre.Console;
using SQLitePCL;
using System.IO;

using System.Configuration;
using System.Collections.Specialized;
using static System.Runtime.InteropServices.JavaScript.JSType;

var userInput = new UserInput();

var end = true;

raw.SetProvider(new SQLite3Provider_e_sqlite3());
using var connection = new SqliteConnection(ConfigurationManager.AppSettings["connection"]);
connection.Open();

var session = new CodingSession();
var controller = new TrackerController(connection);

var createTableQuery = @"CREATE TABLE IF NOT EXISTS 'Tracker' (
    Id INTEGER PRIMARY KEY,
    Date TEXT,
    StartTime TEXT,
    EndTime TEXT
    )";

connection.Execute(createTableQuery);

while (end)
{
    var panelHeader = new Panel("[blue]Welcome to the Coding Tracker![/]").Padding(38,3,3,3).Expand();
    
    AnsiConsole.Write(panelHeader);

    string fruit = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[blue]Please, choose an option from the list below:[/]")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
            .AddChoices(new[] {
                "Start a Coding Session.", "Add a Coding Session.", "Change an existed Coding Session.",
                "Show your Coding Sessions.", "Delete a Coding Session.", "Exit",
                }));
    // ADD VALIDATION
    switch (fruit)
    {
        case "Start a Coding Session.":
            controller.CreateWithTimer(connection, session);
            break;
        case "Add a Coding Session.":
            var dateAddInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$", "Type date of your Coding Session or type T for today's date: "
                ,"Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            dateAddInput = UserInput.CheckT(dateAddInput);

            var startAddTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$", "Type your Coding Session start time: "
                , "Wrong data format, try again. Example: 10:10:10: ");

            var endAddTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$|^N|n$", "Add end time of your Coding Session or type N for the time at this moment: "
                , "Wrong data format, try again. Example: 12:12:12 or N for the time at this moment: ");

            endAddTimeInput = UserInput.CheckN(endAddTimeInput);
            session.StartTime = DateTime.Parse(dateAddInput + " " + startAddTimeInput);
            session.EndTime = DateTime.Parse(dateAddInput + " " + endAddTimeInput);
            controller.Create(connection, session);
            break;
        case "Change an existed Coding Session.":
            var dateChangeInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$", "Type date of your Coding Session or type T for today's date: ",
                "Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            dateChangeInput = UserInput.CheckT(dateChangeInput);

            var sessionTimeInput = userInput.CreateRegex(@"^S|s$|^E|e$", "Type S to change Start Time of Session or type E to change End Time: ","Please, type only listed symbols: ");

            var addSessionList = controller.Read(connection, dateChangeInput);
            if (addSessionList.Count == 0) break;
            var chooseChangeSession = userInput.ChooseSession(addSessionList);
            var ChangeTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$|^N|n$", "Type your new Coding time: ",
                "Wrong data format, try again. Example: 10:10:10: ");

            if (sessionTimeInput is "S" or "s")
            {
                session.StartTime = DateTime.Parse(dateChangeInput + " " + ChangeTimeInput);
                controller.Update(connection, session, chooseChangeSession.StartTime.ToLongTimeString(), sessionTimeInput);
            }
            else
            {
                session.EndTime = DateTime.Parse(dateChangeInput + " " + ChangeTimeInput);
                controller.Update(connection, session, chooseChangeSession.EndTime.ToLongTimeString(), sessionTimeInput);
            }

            break;
        case "Show your Coding Sessions.":
            controller.Read(connection);
            break;
        case "Delete a Coding Session.":
            var deleteDateInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$", "Type data of your Coding Session you chose to delete or type T for today's date: ",
                "Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            deleteDateInput = UserInput.CheckT(deleteDateInput);

            var deleteSessionList = controller.Read(connection, deleteDateInput);
            if (deleteSessionList.Count == 0) break;
            var chooseDeleteSession = userInput.ChooseSession(deleteSessionList);
            controller.Delete(connection, deleteDateInput, chooseDeleteSession.StartTime.ToLongTimeString());
            break;
        case "Exit":
            end = false;
            break;
        default:
            Console.WriteLine("Wrong choice selection, try again! \n");
            break;
    }

    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey();
    Console.Clear();
}