using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.Core;

namespace Test
{
    public static class LoggerAssertions
    {
        public static ICall FirstCall(this ILogger logger)
        {
            return logger.ReceivedCalls().ElementAt(0);
        }

        public static ICall SecondCall(this ILogger logger)
        {
            return logger.ReceivedCalls().ElementAt(1);
        }

        public static FormattedLogValues FormattedLogValues(this ICall call)
        {
            return (FormattedLogValues)call.GetArguments()[2];
        }

        public static void AssertCallCount(this ILogger logger, int numCalls)
        {
            Assert.AreEqual(numCalls, logger.ReceivedCalls().Count());
        }

        public static void AssertLogLevel(this ICall call, LogLevel logLevel)
        {
            Assert.AreEqual(logLevel, call.GetArguments()[0]);
        }

        public static void AssertLogValues(this ICall call, params KeyValuePair<string, string>[] logValues)
        {
            foreach (var logValue in logValues)
            {
                var actualValue = call.FormattedLogValues().First(x => x.Key == logValue.Key).Value;
                Assert.AreEqual(logValue.Value, actualValue.ToString());
            }
        }

        public static void AssertLogMessage(this ICall call, string message)
        {
            var actualMessage = call.FormattedLogValues().First(x => x.Key == "{OriginalFormat}").Value;
            Assert.AreEqual(message, actualMessage);
        }
    }
}
