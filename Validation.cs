using Microsoft.Data.Sqlite;

namespace CodingTracker.TwilightSaw;

public class Validation
{
    public Exception? CheckExecute(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return e;
        }

        return null;
    }

    public Exception? CheckRead(Action action, string message)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Console.WriteLine(message);
            return e;
        }

        return null;
    }
}