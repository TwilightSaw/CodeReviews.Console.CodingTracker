namespace CodingTracker.TwilightSaw;

public class Config
{
    static string dbString = "coding-tracker.db";
    string databasePath = $@"C:\Users\Alex\source\repos\projects\CodeReviews.Console.HabitTracker\bin\Debug\net8.0\{dbString}"; 
    public static string connection = @$"Data Source={dbString}"; 
}