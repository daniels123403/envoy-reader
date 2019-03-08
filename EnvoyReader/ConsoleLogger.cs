using System;
using System.Collections.Generic;
using System.Text;

namespace EnvoyReader
{
    class ConsoleLogger : ILogger
    {
        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }
    }
}
