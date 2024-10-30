using Dapper;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Runtime.InteropServices.JavaScript;

namespace CodingTracker.TwilightSaw;

public struct CodingSession
{
    private int Id;
    public string Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Duration { get; set; }

    public DateTime GetStartTime()
    {
        if (StartTime > EndTime)
            throw new Exception("Bad StartTime or EndTime");
        return StartTime;
    }

    public string CalculateDuration()
    {
        Duration = (TimeSpan.Parse(EndTime.ToLongTimeString()) - TimeSpan.Parse(StartTime.ToLongTimeString())).ToString();
        return Duration;
    }

    public override string ToString()
    {
        return $"{Date} {StartTime.ToLongTimeString()} {EndTime.ToLongTimeString()} {Duration}" ;
    }
}