using System;
using System.IO;
using System.Threading.Tasks;
using Container.Tests.Interfaces;
// ReSharper disable All

namespace Container.Tests.Implementations
{
    public class TestLogFileProvider : ILogFileProvider
    {
        private readonly ILog _logger;

        public TestLogFileProvider(ILog logger)
        {
            _logger = logger;
        }

        public FileInfo GetLogFile()
        {
            return new FileInfo(
                Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "log_" +
                                                       DateTime.Now.Ticks + ".txt"));
        }
    }
}
