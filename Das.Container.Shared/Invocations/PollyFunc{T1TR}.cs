using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public readonly struct PollyFunc<T1, TResult> : IPollyFunc<TResult>
    {
        private readonly Func<T1, TResult> _action;
        private readonly T1 _arg1;

        public PollyFunc(Func<T1, TResult> action,
                         T1 arg1)
        {
            _action = action;
            _arg1 = arg1;
        }

        public TResult Execute()
        {
            return _action(_arg1);
        }
    }
}
