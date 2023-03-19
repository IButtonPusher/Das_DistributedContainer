using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public readonly struct PollyLateFunc<TIn, TResult> : IPollyLateFunc<TIn, TResult>
    {
        private readonly Func<TIn, TResult> _func;

        public PollyLateFunc(Func<TIn, TResult> func)
        {
            _func = func;
        }

        public TResult Execute(TIn p)
        {
            return _func(p);
        }
    }
}
    

