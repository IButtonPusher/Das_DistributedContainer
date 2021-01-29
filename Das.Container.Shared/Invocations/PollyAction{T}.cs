using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public readonly struct PollyAction<TInput1> : IPollyAction
    {
        private readonly TInput1 _input1;
        private readonly Action<TInput1> _action;

        public PollyAction(TInput1 input1,
                           Action<TInput1> action)
        {
            _input1 = input1;
            _action = action;
        }

        public void Execute()
        {
            _action(_input1);
        }
    }
}
