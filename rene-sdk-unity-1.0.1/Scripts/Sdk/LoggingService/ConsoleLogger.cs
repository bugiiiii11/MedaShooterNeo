using System;
using System.Collections.Generic;
using System.Text;

namespace ReneSdk.Rene.Sdk.LoggingService
{
    internal class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
