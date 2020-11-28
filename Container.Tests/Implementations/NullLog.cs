using System;
using System.Threading.Tasks;
using Container.Tests.Interfaces;

namespace Container.Tests.Implementations
{
    public class NullLog : IMegaLog
    {
        public void Log(String text)
        {
            
        }

        public void LogEx(String text)
        {
            
        }
    }
}
