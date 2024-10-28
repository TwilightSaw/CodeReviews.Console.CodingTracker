using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Spectre.Console;

namespace CodingTracker.TwilightSaw
{
    // Validation type class where user input his data and its automatic validated
    internal class UserInput
    {
        private string input;
        private int inputInt;

        public string Create()
        {
            // Simple user input
            input = Console.ReadLine();
            return input;
        }

        public int CreateInt(string message)
        {
            // User input précised to int 
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
            // User input précised to a range of int
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
            // User input précised to a certain combinations of symbols
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
            // Specialized method to process list of session and choose one
            var chosenSession = AnsiConsole.Prompt(
                new SelectionPrompt<CodingSession>()
                    .Title("[blue]Please, choose an option from the list below:[/]")
                    .PageSize(10)
                    .AddChoices(
                        data));
            return chosenSession;
        }

        // Specialized method to check if certain symbols are chosen
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
