using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        public int CreateInt()
        {
            int inputInt;
            input = Console.ReadLine();
            while (!Int32.TryParse(input, out inputInt))
            {
                Console.Write("Write number, please: ");
                input = Console.ReadLine();
            }

            return inputInt;
        }

        public string CreateRegex(string regexString, string message)
        {
            input = Console.ReadLine();
            Regex regex = new Regex(regexString);
            while (!regex.IsMatch(input))
            {
                Console.Write(message);
                input = Console.ReadLine();
            }

            return input;
        }

        public CodingSession ChooseSession(List<CodingSession> data)
        {
            List<string> x = new List<string>();
            for (int i = 0; i < data.Count ; i++)
            {
                x.Add((i+1).ToString());
            }
            Console.Write("Please, choose desired Coding Session: ");
            var r = CreateRegex(CreateDynamicRegex(x), "Insert only the number that is allocated for your Coding Session: ");
            return data[int.Parse(r)-1];
        }

        static string CreateDynamicRegex(List<string> elements)
        {
            // Protecting Regex from special characters
            List<string> escapedElements = new List<string>();
            foreach (string element in elements)
            {
                escapedElements.Add(Regex.Escape(element));
            }

            // Gather elements in one string
            string pattern = string.Join("|", escapedElements);

            return $@"(?<!-)\b({pattern})\b";
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
