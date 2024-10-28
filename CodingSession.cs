using Dapper;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Runtime.InteropServices.JavaScript;

namespace CodingTracker.TwilightSaw;

// Domain type structure of Coding Session 
public struct CodingSession
{
    // Main fields
    private int Id;
    public string Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Duration { get; set; }

    // Specialized Getter to handle specific exception
    public DateTime GetStartTime()
    {
        if (StartTime > EndTime)
            throw new Exception("Bad StartTime or EndTime");
        return StartTime;
    }

    // Calculate duration of session based on values
    public string CalculateDuration()
    {
        Duration = (TimeSpan.Parse(EndTime.ToLongTimeString()) - TimeSpan.Parse(StartTime.ToLongTimeString())).ToString();
        
        return Duration;
    }

    // Overrided ToString method to simplify reading and parsing
    public override string ToString()
    {
        return $"{Date} {StartTime.ToLongTimeString()} {EndTime.ToLongTimeString()} {Duration}" ;
    }
}