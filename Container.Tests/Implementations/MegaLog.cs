using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Container.Tests.Interfaces;
using Das.Container;

namespace Container.Tests.Implementations
{
    public class MegaLog : IMegaLog,
                           IInitializeAsync
    {
        public MegaLog()
        {
            TimeInstantiated++;
        }

        public async Task InitializeAsync()
        {
            Debug.WriteLine("BEGIN INIT MegaLog");


            await Task.Yield();

            Debug.WriteLine("END INIT MegaLog");
        }

        public void Log(String text)
        {
        }

        public void LogEx(String text)
        {
        }

        public static Int32 TimeInstantiated;
    }
}
