using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using Dapper;
using System.IO;
using Microsoft.Data.Sqlite;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Object = System.Object;
using System.Security.AccessControl;
using Spectre.Console;

// Main Controller class
namespace CodingTracker.TwilightSaw
{
    internal class TrackerController
    {
        private Validation validation = new();
        private const string Name = "Tracker";
        private System.Timers.Timer aTimer;
        private int elapsedSeconds;

        public TrackerController(SqliteConnection connection)
        {
            // Constructor to create a table
            var createTableQuery = @$"CREATE TABLE IF NOT EXISTS '{Name}' (
                            Id INTEGER PRIMARY KEY,
                            Date TEXT,
                            StartTime TEXT,
                            EndTime TEXT,
                            Duration TEXT
                            )";

            connection.Execute(createTableQuery);
        }
        public void Create(SqliteConnection connection, CodingSession session)
        {
            // Method from CRUD specification that creates new session based on values
            var insertTableQuery = $@"INSERT INTO [{Name}] (
            Id,
            Date,
            StartTime,
            EndTime,
            Duration
        )
        VALUES (
            (SELECT MAX(Id) + 1 FROM [{Name}]),  
            @Date,                         
            @StartTime,
            @EndTime,
            @Duration
        )";
            
            validation.CheckExecute(() => connection.Execute(insertTableQuery, new
            {
                Date = session.StartTime.Date.ToShortDateString(),
                StartTime = session.GetStartTime().ToLongTimeString(),
                EndTime = session.EndTime.ToLongTimeString(),
                Duration = session.CalculateDuration()
            }));
        }



        public void CreateWithTimer(SqliteConnection connection, CodingSession session)
        {
            // Deviated Create method to use with Timers
            var dateInputC = DateTime.Now;
            session.StartTime = DateTime.Parse(dateInputC.ToShortDateString() + " " + dateInputC.ToLongTimeString());
            SetTimer();

            Console.WriteLine("Press any key to stop the timer.");
            Console.ReadKey();

            StopTimer();
            elapsedSeconds = 0;
            dateInputC = DateTime.Now;
            session.EndTime = DateTime.Parse(dateInputC.ToShortDateString() + " " + dateInputC.ToLongTimeString());

            var savedDuration = session.CalculateDuration();

            // Checking if session lasts to another day, if so then split a session on two different
            if (TimeSpan.Parse(session.CalculateDuration()).Ticks < 0)
            {
                session.EndTime = DateTime.MaxValue;
                Create(connection, session);
                session.StartTime = DateTime.Today;
                var t = TimeSpan.Parse(DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59).ToLongTimeString()) +
                        TimeSpan.Parse(savedDuration);
                session.EndTime = DateTime.Parse(session.StartTime.ToShortDateString() + " " + (t));
                Create(connection, session);
            }
            else
            {
                Create(connection, session);
            }
        }

        public List<CodingSession> Read(SqliteConnection connection)
        {
            // Method from CRUD specification that reads from database all sessions
            var selectTableQuery = @$"SELECT Id, Date, StartTime, EndTime, Duration from [{Name}]";
            List<CodingSession> data = connection.Query<CodingSession>(selectTableQuery).ToList();
            return data;
        }

        public List<CodingSession> Read(SqliteConnection connection, string date)
        {
            // Overrided Read method to read a specific session by date
            var iterator = 0;
            var selectTableQuery = @$"SELECT Id, Date, StartTime, EndTime, Duration from [{Name}] 
                                    WHERE Date = @Date";
            List<CodingSession> data = connection.Query<CodingSession>(selectTableQuery, new {Date = date}).ToList();
            validation.CheckRead(() => Console.WriteLine($"Date: {data[0].Date} "), "Date does not exist.");
            return data;
        }

        public void Update(SqliteConnection connection, CodingSession session, string previousTime, string time)
        {
            // Method from CRUD specification that updates an existed session with a new Start or End time
            if (time is "Change Start Time")
            {

                    var selectTableDateQuery = @$"SELECT Date, EndTime from [{Name}]
                                   WHERE Date = @Date AND StartTime = @PreviousTime";

                CodingSession r = connection.QuerySingleOrDefault<CodingSession>(selectTableDateQuery, new
                {
                    Date = session.StartTime.Date.ToShortDateString(),
                    PreviousTime = previousTime
                });
                session.EndTime = DateTime.Parse(r.Date + " " + r.EndTime.ToLongTimeString());
                session.Date = session.StartTime.Date.ToShortDateString();
                if (CheckCrossingUpdate(connection, session, previousTime))
                {
                    var insertTableQuery = $@"UPDATE [{Name}] 
        SET StartTime = @StartTime, Duration = @Duration
        Where Date = @Date AND StartTime = @PreviousTime";
                    validation.CheckExecute(() => connection.Execute(insertTableQuery, new
                    {
                        Date = session.GetStartTime().Date.ToShortDateString(),
                        StartTime = session.StartTime.ToLongTimeString(),
                        PreviousTime = previousTime,
                        Duration = session.CalculateDuration()
                    }));
                }
                else
                {
                    AnsiConsole.Write(new Rows(
                        new Text("Your session is crossing another sessions.", new Style(Color.Red))));
                }
               
            }
            else
            {
                var selectTableDateQuery = @$"SELECT Date, StartTime, EndTime from [{Name}]
                                   WHERE Date = @Date AND EndTime = @PreviousTime";

                CodingSession r = connection.QuerySingleOrDefault<CodingSession>(selectTableDateQuery, new
                {
                    Date = session.EndTime.Date.ToShortDateString(),
                    PreviousTime = previousTime
                });
                validation.CheckExecute(() => session.StartTime = DateTime.Parse(r.Date + " " + r.GetStartTime().ToLongTimeString()));
                session.Date = session.EndTime.Date.ToShortDateString();
                if (CheckCrossingUpdate(connection, session, previousTime))
                {
                    var insertTableQuery = $@"UPDATE [{Name}] 
        SET EndTime = @EndTime, Duration = @Duration
        Where Date = @Date AND EndTime = @PreviousTime";
                connection.Execute(insertTableQuery, new
                {
                    Date = session.EndTime.Date.ToShortDateString(),
                    EndTime = session.EndTime.ToLongTimeString(),
                    PreviousTime = previousTime,
                    Duration = session.CalculateDuration()
                });
                }
                else
                {
                    AnsiConsole.Write(new Rows(
                        new Text("Your session is crossing another sessions.", new Style(Color.Red))));
                }
            }
            
        }

        public void Delete(SqliteConnection connection, string date, string previousTime)
        {
            // Method from CRUD specification that delete a session based on date and time
            var deleteTableQuery = $@"DELETE FROM [{Name}]
                WHERE Date = @Date AND StartTime = @PreviousTime";
            connection.Execute(deleteTableQuery, new { Date = date, PreviousTime = previousTime});
        }

        public void SetTimer()
        {
            // Method from Timer specification that starts timer
            aTimer = new System.Timers.Timer(1000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        public void StopTimer()
        {
            // Method from Timer specification that stops timer
            aTimer.Stop();
            aTimer.Dispose();
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // Event from Timer specification
            elapsedSeconds++;
            Console.Write($"\rTimer: {elapsedSeconds / 60:D2}:{elapsedSeconds % 60:D2}");
        }

        public (int, TimeSpan, TimeSpan) CreateReport(SqliteConnection connection)
        {
            // Method that creates report based on main properties of CodingSession and data from database
            var sessionList = Read(connection);
            TimeSpan totalTime = new(0,0,0);
            foreach (var session in sessionList)
            {
                totalTime += TimeSpan.Parse(session.Duration);
            }
            var avgTimeSeconds = totalTime.TotalSeconds/sessionList.Count;
            TimeSpan avgTime = new(0, 0, Convert.ToInt32(avgTimeSeconds));
            return (sessionList.Count, totalTime, avgTime);
        }

        public void Start(SqliteConnection connection)
        {
            // Method that creates a table
            var createTableQuery = @"CREATE TABLE IF NOT EXISTS 'Tracker' (
    Id INTEGER PRIMARY KEY,
    Date TEXT,
    StartTime TEXT,
    EndTime TEXT
    )";

            connection.Execute(createTableQuery);
        }

        public bool CheckCrossingUpdate(SqliteConnection connection, CodingSession session, string previousTime)
        {
            var sessionStartTime = TimeSpan.Parse(session.StartTime.ToLongTimeString());
            var sessionEndTime = TimeSpan.Parse(session.EndTime.ToLongTimeString());
            
            var list = Read(connection, session.Date);
            list.RemoveAll(t => t.StartTime.ToLongTimeString() == previousTime);
            var sortedList = list.OrderBy((t) => TimeSpan.Parse(t.StartTime.ToLongTimeString())).ToList() ;

            if (sessionStartTime < TimeSpan.Parse(sortedList[0].StartTime.ToLongTimeString()) &&
                sessionEndTime < TimeSpan.Parse(sortedList[0].StartTime.ToLongTimeString()))
                return true;

            for (int i = 1; i < sortedList.Count-1; i++)
            {
                var currentListSession = sortedList[i-1];
                var nextListSession = sortedList[i];
                if (sessionStartTime > TimeSpan.Parse(currentListSession.EndTime.ToLongTimeString()) &&
                    sessionEndTime < TimeSpan.Parse(nextListSession.StartTime.ToLongTimeString()))
                    return true;
            }
            if(sessionStartTime > TimeSpan.Parse(sortedList[0].EndTime.ToLongTimeString()) &&
              sessionEndTime > TimeSpan.Parse(sortedList[0].EndTime.ToLongTimeString()))
            return true;
            return false;
        }
    }
}
