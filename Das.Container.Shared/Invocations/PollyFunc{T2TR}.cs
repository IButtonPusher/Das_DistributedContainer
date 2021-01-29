using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public readonly struct PollyFunc<T1, T2, TResult> : IPollyFunc<TResult>
    {
        private readonly Func<T1, T2, TResult> _action;
        private readonly T1 _arg1;
        private readonly T2 _arg2;

        public PollyFunc(Func<T1, T2, TResult> action,
                         T1 arg1,
                         T2 arg2)
        {
            _action = action;
            _arg1 = arg1;
            _arg2 = arg2;
        }

        public TResult Execute()
        {
            return _action(_arg1, _arg2);
        }
    }
}
