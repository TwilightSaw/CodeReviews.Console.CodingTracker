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
                StartTime = session.StartTime.ToLongTimeString(),
                EndTime = session.EndTime.ToLongTimeString(),
                Duration = session.CalculateDuration()
            }));
        }



        public void CreateWithTimer(SqliteConnection connection, CodingSession session)
        {

            //VALIDATION OF TIMER AFTER 00:00?
            var dateInputC = DateTime.Now;
            session.StartTime = DateTime.Parse(dateInputC.ToShortDateString() + " " + dateInputC.ToLongTimeString());
            SetTimer();

            Console.WriteLine("Press any key to stop the timer.");
            Console.ReadKey();

            StopTimer();
            elapsedSeconds = 0;
            dateInputC = DateTime.Now;
            session.EndTime = DateTime.Parse(dateInputC.ToShortDateString() + " " + dateInputC.ToLongTimeString());

            if (TimeSpan.Parse(session.CalculateDuration()).Ticks < 0)
            {

            }
            Create(connection, session);
            
        }

        public List<CodingSession> Read(SqliteConnection connection)
        {
            var selectTableQuery = @$"SELECT Id, Date, StartTime, EndTime, Duration from [{Name}]";
            List<CodingSession> data = connection.Query<CodingSession>(selectTableQuery).ToList();
            return data;
        }

        public List<CodingSession> Read(SqliteConnection connection, string date)
        {
            var iterator = 0;
            var selectTableQuery = @$"SELECT Id, Date, StartTime, EndTime, Duration from [{Name}] 
                                    WHERE Date = @Date";
            List<CodingSession> data = connection.Query<CodingSession>(selectTableQuery, new {Date = date}).ToList();
            validation.CheckRead(() => Console.WriteLine($"Date: {data[0].Date} "), "Date is not existed.");
            return data;
        }

        public void Update(SqliteConnection connection, CodingSession session, string previousTime, string time)
        {
            if (time is "Change Start Time")
            {
                var selectTableDateQuery = @$"SELECT Date, EndTime from [{Name}]
                                   WHERE Date = @Date AND StartTime = @PreviousTime";

                var r = connection.QuerySingleOrDefault(selectTableDateQuery, new
                {
                    Date = session.StartTime.Date.ToShortDateString(),
                    PreviousTime = previousTime
                });
                session.EndTime = DateTime.Parse(r.Date + " " + r.EndTime);
                var insertTableQuery = $@"UPDATE [{Name}] 
        SET StartTime = @StartTime, Duration = @Duration
        Where Date = @Date AND StartTime = @PreviousTime";

                validation.CheckExecute(() => connection.Execute(insertTableQuery, new
                {
                    Date = session.StartTime.Date.ToShortDateString(),
                    StartTime = session.StartTime.ToLongTimeString(),
                    PreviousTime = previousTime,
                    Duration = session.CalculateDuration()
                }));
            }
            else
            {
                var selectTableDateQuery = @$"SELECT Date, StartTime from [{Name}]
                                   WHERE Date = @Date AND EndTime = @PreviousTime";

                var r = connection.QuerySingleOrDefault(selectTableDateQuery, new
                {
                    Date = session.EndTime.Date.ToShortDateString(),
                    PreviousTime = previousTime
                });
                session.StartTime = DateTime.Parse(r.Date + " " + r.StartTime);
                var insertTableQuery = $@"UPDATE [{Name}] 
        SET EndTime = @EndTime, Duration = @Duration
        Where Date = @Date AND EndTime = @PreviousTime";

                validation.CheckExecute(() => connection.Execute(insertTableQuery, new
                {
                    Date = session.EndTime.Date.ToShortDateString(),
                    EndTime = session.EndTime.ToLongTimeString(),
                    PreviousTime = previousTime,
                    Duration = session.CalculateDuration()
                }));
            }
            
        }

        public void Delete(SqliteConnection connection, string date, string previousTime)
        {
            var deleteTableQuery = $@"DELETE FROM [{Name}]
                WHERE Date = @Date AND StartTime = @PreviousTime";
            connection.Execute(deleteTableQuery, new { Date = date, PreviousTime = previousTime});
        }

        public void SetTimer()
        {
            aTimer = new System.Timers.Timer(1000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        public void StopTimer()
        {
            aTimer.Stop();
            aTimer.Dispose();
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            elapsedSeconds++;
            Console.Write($"\rTimer: {elapsedSeconds / 60:D2}:{elapsedSeconds % 60:D2}");
        }

        public (int, TimeSpan, TimeSpan) CreateReport(SqliteConnection connection)
        {
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

        public void CreateGoal()
        {
            
        }
    }
}
