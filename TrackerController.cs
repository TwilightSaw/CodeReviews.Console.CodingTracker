using System.Timers;
using Dapper;
using Microsoft.Data.Sqlite;
using Object = System.Object;
using Spectre.Console;
using System.Xml.Linq;

namespace CodingTracker.TwilightSaw
{
    internal class TrackerController
    {
        private Validation _validation = new();
        private const string TableName = "Tracker";
        private System.Timers.Timer _aTimer;
        private int _elapsedSeconds;

        public TrackerController(SqliteConnection connection)
        {
            var createTableQuery = @$"CREATE TABLE IF NOT EXISTS '{TableName}' (
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
            var insertTableQuery = $@"INSERT INTO [{TableName}] (
            Id,
            Date,
            StartTime,
            EndTime,
            Duration
        )
        VALUES (
            (SELECT MAX(Id) + 1 FROM [{TableName}]),  
            @Date,                         
            @StartTime,
            @EndTime,
            @Duration
        )";

            session.Date = session.StartTime.Date.ToShortDateString();
            if (IsAvailable(connection, session, null))
            {
                _validation.CheckExecute(() => connection.Execute(insertTableQuery, new
                {
                    session.Date,
                    StartTime = session.GetStartTime().ToLongTimeString(),
                    EndTime = session.EndTime.ToLongTimeString(),
                    Duration = session.CalculateDuration()
                }));
            }
            else
                AnsiConsole.Write(new Rows(
                    new Text("Your session is crossing another sessions.", new Style(Color.Red))));
        }

        public void CreateWithTimer(SqliteConnection connection, CodingSession session)
        {
            var dateTime = DateTime.Now;
            session.StartTime = DateTime.Parse(dateTime.ToShortDateString() + " " + dateTime.ToLongTimeString());
            SetTimer();

            AnsiConsole.Write(new Rows(
                new Text("Press any key to stop the timer.", new Style(Color.LightGreen))));
            Console.ReadKey();

            StopTimer();
            _elapsedSeconds = 0;
            dateTime = DateTime.Now;
            session.EndTime = DateTime.Parse(dateTime.ToShortDateString() + " " + dateTime.ToLongTimeString());

            var savedDuration = session.CalculateDuration();
            if (TimeSpan.Parse(session.CalculateDuration()).Ticks < 0)
            {
                session.EndTime = DateTime.MaxValue;
                Create(connection, session);
                session.StartTime = DateTime.Today;
                var splitSessionTime = TimeSpan.Parse(DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59).ToLongTimeString()) +
                        TimeSpan.Parse(savedDuration);
                session.EndTime = DateTime.Parse(session.StartTime.ToShortDateString() + " " + (splitSessionTime));
                Create(connection, session);
            }
            else
                Create(connection, session);
        }

        public List<CodingSession> Read(SqliteConnection connection)
        {
            var selectTableQuery = @$"SELECT Id, Date, StartTime, EndTime, Duration from [{TableName}]";
            List<CodingSession> data = connection.Query<CodingSession>(selectTableQuery).ToList();
            return data;
        }
        //DELETE?
        public List<CodingSession> ReadOneDate(SqliteConnection connection, string date)
        {
            var selectTableQuery = @$"SELECT Id, Date, StartTime, EndTime, Duration from [{TableName}] 
                                    WHERE Date = @Date";
            List<CodingSession> data = connection.Query<CodingSession>(selectTableQuery, new {Date = date}).ToList();
            _validation.CheckWithMessage(() => Console.WriteLine($"Date: {data[0].Date} "), "Date does not exist.");
            return data;
        }

        public void Update(SqliteConnection connection, CodingSession session, string previousTime, string time)
        {
            if (time is "Change Start Time")
            {
                    var selectTableDateQuery = @$"SELECT Date, EndTime from [{TableName}]
                                   WHERE Date = @Date AND StartTime = @PreviousTime";
                CodingSession selectedSession = connection.QuerySingleOrDefault<CodingSession>(selectTableDateQuery, new
                {
                    Date = session.StartTime.Date.ToShortDateString(),
                    PreviousTime = previousTime
                });
                session.EndTime = DateTime.Parse(selectedSession.Date + " " + selectedSession.EndTime.ToLongTimeString());
                session.Date = session.StartTime.Date.ToShortDateString();

                if (IsAvailable(connection, session, previousTime))
                {
                    var insertTableQuery = $@"UPDATE [{TableName}] 
                                SET StartTime = @StartTime, Duration = @Duration
                                Where Date = @Date AND StartTime = @PreviousTime";
                    _validation.CheckExecute(() => connection.Execute(insertTableQuery, new
                    {
                        Date = session.GetStartTime().Date.ToShortDateString(),
                        StartTime = session.StartTime.ToLongTimeString(),
                        PreviousTime = previousTime,
                        Duration = session.CalculateDuration()
                    }));
                }
                else
                    AnsiConsole.Write(new Rows(
                        new Text("Your session is crossing another sessions.", new Style(Color.Red))));
            }
            else
            {
                var selectTableDateQuery = @$"SELECT Date, StartTime, EndTime from [{TableName}]
                                   WHERE Date = @Date AND EndTime = @PreviousTime";
                CodingSession selectedSession = connection.QuerySingleOrDefault<CodingSession>(selectTableDateQuery, new
                {
                    Date = session.EndTime.Date.ToShortDateString(),
                    PreviousTime = previousTime
                });
                _validation.CheckExecute(() => session.StartTime = DateTime.Parse(selectedSession.Date + " " + selectedSession.GetStartTime().ToLongTimeString()));

                session.Date = session.EndTime.Date.ToShortDateString();
                if (IsAvailable(connection, session, session.StartTime.ToLongTimeString()))
                {
                    var insertTableQuery = $@"UPDATE [{TableName}] 
                            SET EndTime = @EndTime, Duration = @Duration
                            Where Date = @Date AND EndTime = @PreviousTime";
                connection.Execute(insertTableQuery, new
                {
                    session.Date,
                    EndTime = session.EndTime.ToLongTimeString(),
                    PreviousTime = previousTime,
                    Duration = session.CalculateDuration()
                });
                }
                else
                    AnsiConsole.Write(new Rows(
                        new Text("Your session is crossing another sessions.", new Style(Color.Red))));
            }
        }

        public void Delete(SqliteConnection connection, string date, string previousTime)
        {
            var deleteTableQuery = $@"DELETE FROM [{TableName}]
                WHERE Date = @Date AND StartTime = @PreviousTime";
            connection.Execute(deleteTableQuery, new { Date = date, PreviousTime = previousTime});
        }

        public void SetTimer()
        {
            _aTimer = new System.Timers.Timer(1000);
            _aTimer.Elapsed += OnTimedEvent;
            _aTimer.AutoReset = true;
            _aTimer.Enabled = true;
        }

        public void StopTimer()
        {
            _aTimer.Stop();
            _aTimer.Dispose();
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            _elapsedSeconds++;
            Console.Write($"\rTimer: {_elapsedSeconds / 60:D2}:{_elapsedSeconds % 60:D2}");
        }

        public (int, TimeSpan, TimeSpan) CreateReport(SqliteConnection connection)
        {
            var sessionList = Read(connection);
            TimeSpan totalTime = new(0,0,0);

            foreach (var session in sessionList)
                totalTime += TimeSpan.Parse(session.Duration);

            var avgTimeSeconds = totalTime.TotalSeconds/sessionList.Count;
            TimeSpan avgTime = new(0, 0, Convert.ToInt32(avgTimeSeconds));
            return (sessionList.Count, totalTime, avgTime);
        }

        public bool IsAvailable(SqliteConnection connection, CodingSession session, string? previousTime)
        {
            var sessionStartTime = TimeSpan.Parse(session.StartTime.ToLongTimeString());
            var sessionEndTime = TimeSpan.Parse(session.EndTime.ToLongTimeString());
            
            var list = ReadOneDate(connection, session.Date);
            //FIX
            if (list.Count == 0) return false;
            list.RemoveAll(t => t.StartTime.ToLongTimeString() == previousTime);
            var sortedList = list.OrderBy((t) => TimeSpan.Parse(t.StartTime.ToLongTimeString())).ToList() ;

            if (sessionStartTime < TimeSpan.Parse(sortedList[0].StartTime.ToLongTimeString()) &&
                sessionEndTime < TimeSpan.Parse(sortedList[0].StartTime.ToLongTimeString()))
                return true;

            for (int i = 1; i <= sortedList.Count-1; i++)
            {
                var currentListSession = sortedList[i-1];
                var nextListSession = sortedList[i];
                if (sessionStartTime > TimeSpan.Parse(currentListSession.EndTime.ToLongTimeString()) &&
                    sessionEndTime < TimeSpan.Parse(nextListSession.StartTime.ToLongTimeString()))
                    return true;
            }

            if(sessionStartTime > TimeSpan.Parse(sortedList[^1].EndTime.ToLongTimeString()) &&
              sessionEndTime > TimeSpan.Parse(sortedList[^1].EndTime.ToLongTimeString()))
                return true;
            return false;
        }

        public List<CodingSession> Order(SqliteConnection connection, List<CodingSession> sessions, bool isOrderAscending)
        {
            var sortedSessions = sessions.OrderBy((t) => t.Date).ThenBy(t => TimeSpan.Parse(t.StartTime.ToLongTimeString())).ToList();
            var sortedSessionsReversed = sessions.OrderByDescending((t) => t.Date)
                .ThenBy(t => TimeSpan.Parse(t.StartTime.ToLongTimeString())).ToList();
           return isOrderAscending ? sortedSessionsReversed : sortedSessions;
        }

        public List<CodingSession> ReadBy(SqliteConnection connection, string date)
        {
            var selectTableQuery = @$"SELECT Id, Date, StartTime, EndTime, Duration from [{TableName}] 
                                    WHERE Date LIKE '%{date}%'";
            List<CodingSession> data = connection.Query<CodingSession>(selectTableQuery, new { Date = date }).ToList();
            _validation.CheckWithMessage(() => Console.WriteLine($"Date: {data[0].Date} "), "Date does not exist.");
            return data;
        }
    }
}
