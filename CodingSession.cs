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
    

    

    public string CalculateDuration()
    {
        Duration = (TimeSpan.Parse(EndTime.ToLongTimeString()) - TimeSpan.Parse(StartTime.ToLongTimeString())).ToString();
        if (TimeSpan.Parse(Duration).Ticks < 0)
        {
            throw new Exception("Bad start or end time.");
        }
        return Duration;
    }

    public override string ToString()
    {
        return $"{Date} {StartTime.ToLongTimeString()} {EndTime.ToLongTimeString()} {Duration}" ;
    }
}