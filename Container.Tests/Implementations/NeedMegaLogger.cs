using System;
using System.Threading.Tasks;
using Container.Tests.Interfaces;

// ReSharper disable All

namespace Container.Tests.Implementations
{
    public class NeedMegaLogger : INeedMegaLogger
    {
        public NeedMegaLogger(IMegaLog megaLog)
        {
            _megaLog = megaLog;
        }

        private readonly IMegaLog _megaLog;
    }
}
