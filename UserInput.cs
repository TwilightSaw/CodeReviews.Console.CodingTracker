using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Spectre.Console;

namespace CodingTracker.TwilightSaw
{
    internal class UserInput
    {
        private string input;

        public string Create()
        {
            input = Console.ReadLine();
            return input;
        }

        public int CreateInt(string message)
        {
            int inputInt;
            input = Console.ReadLine();
            while (!Int32.TryParse(input, out inputInt))
            {
                Console.Write(message);
                input = Console.ReadLine();
            }

            return inputInt;
        }

        public int CreateSpecifiedInt(int bound, string message)
        {
            int inputInt;
            input = Console.ReadLine();
            while (!Int32.TryParse(input, out inputInt))
            {
                Console.Write(message);
                input = Console.ReadLine();
            }
            while (int.Parse(input) > bound || int.Parse(input) < 1)
            {
                Console.Write(message);
                input = Console.ReadLine();
            }
            return inputInt;
        }

        public string CreateRegex(string regexString, string messageStart, string messageError)
        {
            Regex regex = new Regex(regexString);
            var input = AnsiConsole.Prompt(
                new TextPrompt<string>($"[green]{messageStart}[/]")
                    .Validate(value =>
                    {
                        return regex.IsMatch(value)
                            ? ValidationResult.Success()
                            : ValidationResult.Error(messageError);
                    }));
            
           return input;
        }

        public CodingSession ChooseSession(List<CodingSession> data)
        {
            var chosenSession = AnsiConsole.Prompt(
                new SelectionPrompt<CodingSession>()
                    .Title("[blue]Please, choose an option from the list below:[/]")
                    .PageSize(10)
                    .AddChoices(
                        data));
            /*List<string> x = new List<string>();
            for (int i = 0; i < data.Count ; i++)
            {
                x.Add((i+1).ToString());
            }
            Console.Write("Please, choose desired Coding Session: ");
            var r = CreateSpecifiedInt(data.Count, "Insert only the number that is allocated for your Coding Session: ");*/
            return chosenSession;
        }

        
        public static string CheckT(string dateInput)
        {
            return dateInput is "T" or "t" ? DateTime.Now.ToShortDateString() : dateInput;
        }

        public static string CheckN(string timeInput)
        {
            return timeInput is "N" or "n" ? DateTime.Now.ToLongTimeString() : timeInput;
        }
    }
}
