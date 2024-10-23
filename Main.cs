using CodingTracker.TwilightSaw;
using Dapper;
using Microsoft.Data.Sqlite;
using Spectre.Console;
using SQLitePCL;

var userInput = new UserInput();

var end = true;

raw.SetProvider(new SQLite3Provider_e_sqlite3());
using var connection = new SqliteConnection(Config.connection);
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
    AnsiConsole.Markup(@"[blue]Welcome to the Coding Tracker![/]
                    
Please, choose an option from the list below:
1. Start a Coding Session.
2. Add a Coding Session.
3. Change an existed Coding Session.
4. Show your Coding Sessions.
5. Delete a Coding Session.
6. Exit

");
    // ADD VALIDATION

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
            Console.Write("Type date of your Coding Session or type T for today's date: ");
            var dateAddInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$",
                "Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            dateAddInput = UserInput.CheckT(dateAddInput);

            Console.Write("Type your Coding Session start time: ");
            var startAddTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$",
                "Wrong data format, try again. Example: 10:10:10: ");

            Console.Write("Add end time of your Coding Session or type N for the time at this moment: ");
            var endAddTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$|^N|n$",
                "Wrong data format, try again. Example: 12:12:12 or N for the time at this moment: ");

            endAddTimeInput = UserInput.CheckN(endAddTimeInput);
            session.StartTime = DateTime.Parse(dateAddInput + " " + startAddTimeInput);
            session.EndTime = DateTime.Parse(dateAddInput + " " + endAddTimeInput);
            controller.Create(connection, session);
            break;
        case 3:
            Console.Write("Type date of your Coding Session or type T for today's date: ");
            var dateChangeInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$",
                "Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            dateChangeInput = UserInput.CheckT(dateChangeInput);

            Console.Write("Type S to change Start Time of Session or type E to change End Time: ");
            var sessionTimeInput = userInput.CreateRegex(@"^S|s$|^E|e$", "Please, type only listed symbols: ");

            var chooseChangeSession = userInput.ChooseSession(controller.Read(connection, dateChangeInput));
            Console.Write("Type your new Coding time: ");
            var ChangeTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$|^N|n$",
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
        case 4:
            controller.Read(connection);
            break;
        case 5:
            Console.Write("Type data of your Coding Session you chose to delete or type T for today's date: ");
            var deleteDateInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$",
                "Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            deleteDateInput = UserInput.CheckT(deleteDateInput);

            var chooseDeleteSession = userInput.ChooseSession(controller.Read(connection, deleteDateInput));
            controller.Delete(connection, deleteDateInput, chooseDeleteSession.StartTime.ToLongTimeString());
            break;
        case 6:
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