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
    }
}
