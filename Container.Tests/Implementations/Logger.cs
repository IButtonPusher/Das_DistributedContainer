using System;
using System.Threading.Tasks;
using Container.Tests.Interfaces;

namespace Container.Tests.Implementations
{
    public class Logger : ILog
    {
        public Logger()
        {
            TimeInstantiated++;
        }

        public void Log(String text)
        {
        }

        public static Int32 TimeInstantiated;
    }
}
