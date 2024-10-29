﻿using System.Configuration;
using CodingTracker.TwilightSaw;
using Dapper;
using Microsoft.Data.Sqlite;
using Spectre.Console;
using SQLitePCL;

// Initialize start components
var userInput = new UserInput();
var end = true;

raw.SetProvider(new SQLite3Provider_e_sqlite3());
using var connection = new SqliteConnection(ConfigurationManager.AppSettings["connection"]);
connection.Open();

var session = new CodingSession();
var controller = new TrackerController(connection);

// Create a new table if current is not existed
controller.Start(connection);

// Start a main menu loop
while (end)
{
    var panelHeader = new Panel("[blue]Welcome to the Coding Tracker![/]").Padding(42, 3, 3, 3).Expand();

    AnsiConsole.Write(panelHeader);

    // Initialize menu choice
    var input = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[blue]Please, choose an option from the list below:[/]")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more categories[/]")
            .AddChoices("Start a Coding Session.", "Add a Coding Session.", "Change an existed Coding Session.",
                "Show your Coding Sessions.", "Delete a Coding Session.", "Reports", "Goals", "Exit"));
    switch (input)
    {
        case "Start a Coding Session.":
            // Create a live stopwatch session
            controller.CreateWithTimer(connection, session);
            break;
        case "Add a Coding Session.":
            // Add a new coding session from scratch
            var dateAddInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$",
                "Type date of your Coding Session or type T for today's date: "
                , "Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            dateAddInput = UserInput.CheckT(dateAddInput);

            var startAddTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$",
                "Type your Coding Session start time: "
                , "Wrong data format, try again. Example: 10:10:10: ");

            var endAddTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$|^N|n$",
                "Add end time of your Coding Session or type N for the time at this moment: "
                , "Wrong data format, try again. Example: 12:12:12 or N for the time at this moment: ");

            endAddTimeInput = UserInput.CheckN(endAddTimeInput);
            session.StartTime = DateTime.Parse(dateAddInput + " " + startAddTimeInput);
            session.EndTime = DateTime.Parse(dateAddInput + " " + endAddTimeInput);
            controller.Create(connection, session);
            break;
        case "Change an existed Coding Session.":
            // Change a Start or End time of already existed session
            var dateChangeInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$",
                "Type date of your Coding Session or type T for today's date: ",
                "Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            dateChangeInput = UserInput.CheckT(dateChangeInput);

            // Choice of Start or End time
            var sessionTimeInput = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[blue]Please, choose an option from the list below:[/]")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more categories)[/]")
                    .AddChoices("Change Start Time", "Change End Time"));

            // Get a list of chosen sessions by date
            var addSessionList = controller.Read(connection, dateChangeInput);
            if (addSessionList.Count == 0) break;
            var chooseChangeSession = UserInput.ChooseSession(addSessionList);
            var ChangeTimeInput = userInput.CreateRegex(@"^([0-1][0-9]|2[0-3])\:([0-5][0-9])\:([0-5][0-9])$|^N|n$",
                "Type your new Coding time: ",
                "Wrong data format, try again. Example: 10:10:10: ");

            // Redirect action based on previous choice of time
            if (sessionTimeInput is "Change Start Time")
            {
                session.StartTime = DateTime.Parse(dateChangeInput + " " + ChangeTimeInput);
                controller.Update(connection, session, chooseChangeSession.StartTime.ToLongTimeString(),
                    sessionTimeInput);
            }
            else
            {
                session.EndTime = DateTime.Parse(dateChangeInput + " " + ChangeTimeInput);
                controller.Update(connection, session, chooseChangeSession.EndTime.ToLongTimeString(),
                    sessionTimeInput);
            }
            break;
        case "Show your Coding Sessions.":
            // Show a table of all sessions
            var data = controller.Read(connection);
            var table = new Table();
            table.AddColumn("Date")
                .AddColumn("Start of Session")
                .AddColumn("End of Session")
                .AddColumn("Total Session Duration").Centered();
            foreach (var x in data)
                table.AddRow(@$"{x.Date}", $"{x.StartTime.ToLongTimeString()}", $"{x.EndTime.ToLongTimeString()}",
                    $"{x.Duration}");
            AnsiConsole.Write(table);
            break;
        case "Delete a Coding Session.":
            // Delete a chosen session selected by date and then by time
            var deleteDateInput = userInput.CreateRegex(@"^([0-2][0-9]|3[01])\.(0[1-9]|1[0-2])\.(\d{4})$|^T|t$",
                "Type data of your Coding Session you chose to delete or type T for today's date: ",
                "Wrong data format, try again. Example: 01.01.2001 or T for today's date: ");
            deleteDateInput = UserInput.CheckT(deleteDateInput);

            var deleteSessionList = controller.Read(connection, deleteDateInput);
            if (deleteSessionList.Count == 0) break;
            var chooseDeleteSession = UserInput.ChooseSession(deleteSessionList);
            controller.Delete(connection, deleteDateInput, chooseDeleteSession.StartTime.ToLongTimeString());
            break;
        case "Reports":
            // Show a simple report of main data
            var reportData = controller.CreateReport(connection);
            AnsiConsole.Write(new Columns(
                new Text($"You have achieved {reportData.Item1} sessions.", new Style(Color.Orange1)),
                new Text($"Total time you spent coding is {reportData.Item2}", new Style(Color.Orange1)),
                new Text($"Your average session lasts {reportData.Item3}", new Style(Color.Orange1))));
            break;
        case "Goals":
            // Read a user goal of desired amount of hours per day and show his completion
            var reportGoalData = controller.CreateReport(connection);
            Console.Write("What is your desired amount of coding hours in a day: ");
            var goalInput = userInput.CreateSpecifiedInt(12, "Only reachable amount of time.");
            var goalHours = reportGoalData.Item3.TotalHours;
            AnsiConsole.Write(new BarChart()
                .Width(60)
                .Label("[green bold underline]Average time per day[/]")
                .CenterLabel()
                .AddItem("Your Time", Math.Round(goalHours, 1), Color.Red)
                .AddItem("Planned Time", Convert.ToDouble(goalInput), Color.Green));

            if (goalHours > goalInput)
                Console.WriteLine($"You are ahead of plan for {goalHours - goalInput:F1} hours! Congrats!");
            else
                Console.WriteLine($"You need {goalInput - goalHours:F1} hours more to achieve your goal! Keep going!");
            break;
        case "Exit":
            // Exit from application
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