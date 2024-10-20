using Dapper;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Runtime.InteropServices.JavaScript;

namespace CodingTracker.TwilightSaw;

public struct CodingSession
{
    private int id;
    private DateTime startTime;
    private DateTime endTime;
    private TimeSpan duration;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string CalculateDuration()
    {
        duration = TimeSpan.Parse(EndTime.ToLongTimeString()) - TimeSpan.Parse(StartTime.ToLongTimeString());
        return duration.ToString();
    }

}