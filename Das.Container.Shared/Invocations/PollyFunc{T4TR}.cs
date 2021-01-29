using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public readonly struct PollyFunc<T1, T2, T3, T4, TResult> : IPollyFunc<TResult>
    {
        private readonly Func<T1, T2, T3, T4, TResult> _action;
        private readonly T1 _arg1;
        private readonly T2 _arg2;
        private readonly T3 _arg3;
        private readonly T4 _arg4;

        public PollyFunc(Func<T1, T2, T3, T4, TResult> action,
                         T1 arg1,
                         T2 arg2,
                         T3 arg3,
                         T4 arg4)
        {
            _action = action;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
        }

        public TResult Execute()
        {
            return _action(_arg1, _arg2, _arg3, _arg4);
        }
    }
}
