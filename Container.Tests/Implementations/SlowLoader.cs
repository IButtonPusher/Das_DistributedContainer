using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Container.Tests.Interfaces;
using Das.Container;

namespace Container.Tests.Implementations
{
    public class SlowLoader : ILoadSlowly, IInitializeAsync
    {
        public SlowLoader()
        {
            if (Interlocked.Add(ref _instances, 1) > 1)
                throw new DuplicateNameException();   
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref _instances, 0);
        }

        public async Task InitializeAsync()
        {
            await Task.Delay(5000);
        }

        private static Int32 _instances;
    }
}
