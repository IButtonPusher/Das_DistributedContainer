using System;
using System.Threading.Tasks;
using Container.Tests.Interfaces;

// ReSharper disable All

namespace Container.Tests.Implementations
{
    public class TestLogSizeProvider : ILogSizeProvider
    {
        public TestLogSizeProvider(ILog logger,
                                   ILogFileProvider logFileProvider)
        {
            _logger = logger;
            _logFileProvider = logFileProvider;
        }

        public Int64 GetLogSize()
        {
            var file = _logFileProvider.GetLogFile();
            if (!file.Exists)
                return 0;

            return file.Length;
        }

        private readonly ILogFileProvider _logFileProvider;
        private readonly ILog _logger;
    }
}
