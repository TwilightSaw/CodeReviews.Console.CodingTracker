using Microsoft.Data.Sqlite;

namespace CodingTracker.TwilightSaw;

public class Validation
{
    public bool IsExecutable(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
        return true;
    }

    public Exception? CheckWithMessage(Action action, string message)
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