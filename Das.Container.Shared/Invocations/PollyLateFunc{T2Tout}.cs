using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public readonly struct PollyLateFunc<TIn, TMid, TResult> 
       : IPollyLateFunc<TIn, TResult>
    {
        private readonly Func<TMid, TResult> _func1;
        private readonly Func<TIn, Func<TMid, TResult>, TResult> _func2;

        public PollyLateFunc(Func<TMid, TResult> func1,
                             Func<TIn, Func<TMid, TResult>, TResult> func2)
        {
           _func1 = func1;
           _func2 = func2;
        }

        public TResult Execute(TIn p) => _func2(p, _func1);
    }
}
    

