using System;
using System.Threading.Tasks;
using Container.Tests.Interfaces;

// ReSharper disable All

namespace Container.Tests.Implementations
{
    public class NeedSlowly1 : INeedLoadSlowly1
    {
        public NeedSlowly1(ILoadSlowly lodr,
                           INeedLoadSlowly2 needLoadSlowly2)
        {
            _lodr = lodr;
            _needLoadSlowly2 = needLoadSlowly2;
        }

        private readonly ILoadSlowly _lodr;
        private readonly INeedLoadSlowly2 _needLoadSlowly2;
    }
}
