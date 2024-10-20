using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Object = System.Object;

namespace CodingTracker.TwilightSaw
{
    internal class TrackerController
    {
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

            connection.Execute(insertTableQuery, new { Date = session.StartTime.Date.ToShortDateString(), 
                                                             StartTime = session.StartTime.ToLongTimeString(), 
                                                             EndTime = session.EndTime.ToLongTimeString(), 
                                                             Duration = session.CalculateDuration() });
        }
        
        public void Read(SqliteConnection connection)
        {
            var selectTableQuery = @$"SELECT Id, Date, StartTime, EndTime, Duration from [{Name}]";
            var data = connection.Query(selectTableQuery).ToList();
            foreach (var x in data)
            {
                
                Console.WriteLine(@$"Date: {x.Date} Start of Session: {x.StartTime} End of Session: {x.EndTime} Total Session Duration: {x.Duration}");
            }

        }

        public void Update(SqliteConnection connection, CodingSession session, string previousTime)
        {
            //FIX
            var selectTableDateQuery = @$"SELECT Date from [{Name}]
                                   WHERE Date = @Date AND StartTime = @PreviousTime";

            var selectTableEndTimeQuery = @$"SELECT EndTime from [{Name}]
                                   WHERE Date = @Date AND StartTime = @PreviousTime";

            connection.Execute(selectTableDateQuery, new
            {
                Date = session.StartTime.Date.ToShortDateString(),
                PreviousTime = previousTime
            });
            session.EndTime = DateTime.Parse(selectTableDateQuery + " " + selectTableEndTimeQuery);
            //FIX
            var insertTableQuery = $@"UPDATE [{Name}] 
        SET StartTime = @StartTime, Duration = @Duration
        Where Date = @Date AND StartTime = @PreviousTime";
        
            connection.Execute(insertTableQuery, new {Date = session.StartTime.Date.ToShortDateString(), 
                                                           StartTime = session.StartTime.ToLongTimeString(),
                                                           PreviousTime = previousTime,
                                                           Duration = session.CalculateDuration()
            });
        }

        public void Delete(SqliteConnection connection, string date)
        {
            var deleteTableQuery = $@"DELETE FROM [{Name}]
                WHERE Date = @Date";
            connection.Execute(deleteTableQuery, new { Date = date});
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
            var currentTop1 = Console.CursorTop;
            var currentLeft2 = Console.CursorLeft;

            elapsedSeconds++;

            Console.Write($"\rTimer: {elapsedSeconds / 60:D2}:{elapsedSeconds % 60:D2}");
        }
    }
}
