using System;
using System.Threading.Tasks;
using Container.Tests.Interfaces;

// ReSharper disable All

namespace Container.Tests.Implementations
{
    public class NeedLogger1 : INeedLogger1
    {
        public NeedLogger1(ILog iUseLog)
        {
            _iUseLog = iUseLog;
        }

        private readonly ILog _iUseLog;
    }
}
