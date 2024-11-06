using System.Timers;
using Dapper;
using Microsoft.Data.Sqlite;
using Spectre.Console;
using Object = object;
using Timer = System.Timers.Timer;

namespace CodingTracker.TwilightSaw;

internal class TrackerController
{
    private readonly Validation _validation = new();
    private const string TableName = "Tracker";
    private Timer _aTimer;
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
        var insertTableQuery = $@"INSERT INTO '{TableName}' (
            Id,
            Date,
            StartTime,
            EndTime,
            Duration
        )
        VALUES (
            (SELECT MAX(Id) + 1 FROM '{TableName}'),  
            @Date,                         
            @StartTime,
            @EndTime,
            @Duration
        )";

        session.Date = session.StartTime.Date.ToShortDateString();

        var savedDuration = session.CalculateDuration();


        if (TimeSpan.Parse(session.CalculateDuration()).Ticks < 0)
        {
            bool checkValidation = false;
            session.EndTime = DateTime.MaxValue;
            session.CalculateDuration();
            if (IsAvailable(connection, session, null))
            {
                checkValidation = _validation.IsExecutable(() => connection.Execute(insertTableQuery, new
                {
                    session.Date,
                    StartTime = session.GetStartTime().ToLongTimeString(),
                    EndTime = session.EndTime.ToLongTimeString(),
                    Duration = session.CalculateDuration()
                }));
            }
            session.StartTime = DateTime.Today;
            session.Date = DateTime.Today.AddDays(1).ToShortDateString();
            var splitSessionTime =
                TimeSpan.Parse(DateTime.Today.AddHours(23).ToLongTimeString()) +
                TimeSpan.Parse(savedDuration);
            session.EndTime = DateTime.Parse(session.StartTime.ToShortDateString() + " " + splitSessionTime);
            session.CalculateDuration();
            if (IsAvailable(connection, session, null))
            {
                checkValidation = _validation.IsExecutable(() => connection.Execute(insertTableQuery, new
                {
                    session.Date,
                    StartTime = session.GetStartTime().ToLongTimeString(),
                    EndTime = session.EndTime.ToLongTimeString(),
                    Duration = session.CalculateDuration()
                }));
            }
            AnsiConsole.Write(checkValidation
                ? new Rows(new Text("\nAdded successfully", new Style(Color.LightGreen)))
                : new Rows(new Text("\nFailed to add", new Style(Color.Red))));
        }
        else
        {
            if (IsAvailable(connection, session, null))
            {
                var checkValidation = _validation.IsExecutable(() => connection.Execute(insertTableQuery, new
                {
                    session.Date,
                    StartTime = session.GetStartTime().ToLongTimeString(),
                    EndTime = session.EndTime.ToLongTimeString(),
                    Duration = session.CalculateDuration()
                }));
                AnsiConsole.Write(checkValidation
                    ? new Rows(new Text("\nAdded successfully", new Style(Color.LightGreen)))
                    : new Rows(new Text("\nFailed to add", new Style(Color.Red))));
            }
            else
            {
                AnsiConsole.Write(new Rows(
                    new Text("Your session is crossing another sessions.", new Style(Color.Red))));
            }
        }
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
            var splitSessionTime =
                TimeSpan.Parse(DateTime.Today.AddHours(23).AddMinutes(59).AddSeconds(59).ToLongTimeString()) +
                TimeSpan.Parse(savedDuration);
            session.EndTime = DateTime.Parse(session.StartTime.ToShortDateString() + " " + splitSessionTime);
            Create(connection, session);
        }
        else
        {
            Create(connection, session);
        }
    }

    public List<CodingSession> Read(SqliteConnection connection)
    {
        var selectTableQuery = $"SELECT Id, Date, StartTime, EndTime, Duration from '{TableName}'";
        var data = connection.Query<CodingSession>(selectTableQuery).ToList();
        return data;
    }

    public List<CodingSession> ReadBy(SqliteConnection connection, string date, string message)
    {
        var selectTableQuery = @$"SELECT Id, Date, StartTime, EndTime, Duration from '{TableName}' 
                                    WHERE Date LIKE '%{date}%'";
        var data = connection.Query<CodingSession>(selectTableQuery, new { Date = date }).ToList();
        _validation.CheckWithMessage(() => DateTime.Parse(data[0].Date), message);
        return data;
    }

    public void Update(SqliteConnection connection, CodingSession session, string previousTime, string time)
    {
        if (time is "Change Start Time")
        {
            var selectTableDateQuery = @$"SELECT Date, EndTime from '{TableName}'
                                   WHERE Date = @Date AND StartTime = @PreviousTime";
            var selectedSession = connection.QuerySingleOrDefault<CodingSession>(selectTableDateQuery, new
            {
                Date = session.StartTime.Date.ToShortDateString(),
                PreviousTime = previousTime
            });
            session.EndTime = DateTime.Parse(selectedSession.Date + " " + selectedSession.EndTime.ToLongTimeString());
            session.Date = session.StartTime.Date.ToShortDateString();

            if (IsAvailable(connection, session, previousTime))
            {
                var insertTableQuery = $@"UPDATE '{TableName}' 
                                SET StartTime = @StartTime, Duration = @Duration
                                Where Date = @Date AND StartTime = @PreviousTime";

                var checkValidation = _validation.IsExecutable(() => connection.Execute(insertTableQuery, new
                {
                    Date = session.GetStartTime().Date.ToShortDateString(),
                    StartTime = session.StartTime.ToLongTimeString(),
                    PreviousTime = previousTime,
                    Duration = session.CalculateDuration()
                }));
                AnsiConsole.Write(checkValidation
                    ? new Rows(new Text("Changed successfully", new Style(Color.LightGreen)))
                    : new Rows(new Text("Failed to add", new Style(Color.Red))));
            }
            else
            {
                AnsiConsole.Write(new Rows(
                    new Text("Your session is crossing another sessions.", new Style(Color.Red))));
            }
        }
        else
        {
            var selectTableDateQuery = @$"SELECT Date, StartTime, EndTime from '{TableName}'
                                   WHERE Date = @Date AND EndTime = @PreviousTime";
            var selectedSession = connection.QuerySingleOrDefault<CodingSession>(selectTableDateQuery, new
            {
                Date = session.EndTime.Date.ToShortDateString(),
                PreviousTime = previousTime
            });
            session.StartTime =
                DateTime.Parse(selectedSession.Date + " " + selectedSession.GetStartTime().ToLongTimeString());
            session.Date = session.EndTime.Date.ToShortDateString();

            if (!_validation.IsExecutable(() => session.GetStartTime())) return;
            if (IsAvailable(connection, session, session.StartTime.ToLongTimeString()))
            {
                var insertTableQuery = $@"UPDATE '{TableName}' 
                            SET EndTime = @EndTime, Duration = @Duration
                            Where Date = @Date AND EndTime = @PreviousTime";
                var checkValidation = _validation.IsExecutable(() => connection.Execute(insertTableQuery, new
                {
                    session.Date,
                    EndTime = session.EndTime.ToLongTimeString(),
                    PreviousTime = previousTime,
                    Duration = session.CalculateDuration()
                }));
                AnsiConsole.Write(checkValidation
                    ? new Rows(new Text("Changed successfully", new Style(Color.LightGreen)))
                    : new Rows(new Text("Failed to add", new Style(Color.Red))));
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
        var deleteTableQuery = $@"DELETE FROM '{TableName}'
                WHERE Date = @Date AND StartTime = @PreviousTime";
        connection.Execute(deleteTableQuery, new { Date = date, PreviousTime = previousTime });
        AnsiConsole.Write(new Rows(new Text("\nDeleted successfully", new Style(Color.LightGreen))));
    }

    public void SetTimer()
    {
        _aTimer = new Timer(1000);
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
        TimeSpan totalTime = new(0, 0, 0);

        foreach (var session in sessionList)
            totalTime += TimeSpan.Parse(session.Duration);

        var avgTimeSeconds = totalTime.TotalSeconds / sessionList.Count;
        if (double.IsNaN(avgTimeSeconds))
            return (sessionList.Count, totalTime, new TimeSpan(0, 0, 0));
        TimeSpan avgTime = new(0, 0, Convert.ToInt32(avgTimeSeconds));
        return (sessionList.Count, totalTime, avgTime);
    }

    public bool IsAvailable(SqliteConnection connection, CodingSession session, string? previousTime)
    {
        var sessionStartTime = TimeSpan.Parse(session.StartTime.ToLongTimeString());
        var sessionEndTime = TimeSpan.Parse(session.EndTime.ToLongTimeString());

        var list = ReadBy(connection, session.Date, "");
        if (list.Count != 0)
        {
            list.RemoveAll(t => t.StartTime.ToLongTimeString() == previousTime);
            var sortedList = list.OrderBy(t => TimeSpan.Parse(t.StartTime.ToLongTimeString())).ToList();
            try
            {
                sortedList[0].ToString();
            }
            catch (ArgumentOutOfRangeException)
            {
                return true;
            }

            if (sessionStartTime < TimeSpan.Parse(sortedList[0].StartTime.ToLongTimeString()) &&
                sessionEndTime < TimeSpan.Parse(sortedList[0].StartTime.ToLongTimeString()))
                return true;

            for (var i = 1; i <= sortedList.Count - 1; i++)
            {
                var currentListSession = sortedList[i - 1];
                var nextListSession = sortedList[i];
                if (sessionStartTime > TimeSpan.Parse(currentListSession.EndTime.ToLongTimeString()) &&
                    sessionEndTime < TimeSpan.Parse(nextListSession.StartTime.ToLongTimeString()))
                    return true;
            }

            if (sessionStartTime > TimeSpan.Parse(sortedList[^1].EndTime.ToLongTimeString()) &&
                sessionEndTime > TimeSpan.Parse(sortedList[^1].EndTime.ToLongTimeString()))
                return true;
        }
        else
        {
            return true;
        }

        return false;
    }

    public List<CodingSession> Order(SqliteConnection connection, List<CodingSession> sessions, bool isOrderAscending)
    {
        var sortedSessions = sessions.OrderBy(t => DateTime.Parse(t.Date))
            .ThenBy(t => TimeSpan.Parse(t.StartTime.ToLongTimeString())).ToList();
        var sortedSessionsReversed = sessions.OrderByDescending(t => DateTime.Parse(t.Date))
            .ThenBy(t => TimeSpan.Parse(t.StartTime.ToLongTimeString())).ToList();
        return isOrderAscending ? sortedSessionsReversed : sortedSessions;
    }
}