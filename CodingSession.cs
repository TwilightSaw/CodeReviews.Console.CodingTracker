using Dapper;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Globalization;
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
            throw new Exception("You are trying to add session that finishes in another day or entered bad Start Time or End Time");
        return StartTime;
    }

    public string CalculateDuration()
    {
        Duration = (TimeSpan.Parse(EndTime.ToLongTimeString()) - TimeSpan.Parse(StartTime.ToLongTimeString())).ToString();
        return Duration;
    }

    public override string ToString()
    {
        return $"{Date} {StartTime:dd.MM.yyyy} {EndTime:dd.MM.yyyy} {Duration}" ;
    }
}