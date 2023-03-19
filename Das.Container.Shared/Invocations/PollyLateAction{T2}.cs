using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public readonly struct PollyLateAction<TInput1, TInput2> : IPollyAction<TInput1>
    {
        private readonly TInput2 _input2;
        private readonly Action<TInput1, TInput2> _action;

        public PollyLateAction(TInput2 input2,
                               Action<TInput1, TInput2> action)
        {
            _input2 = input2;
            _action = action;
        }

        public void Execute(TInput1 input1)
        {
            _action(input1, _input2);
        }
    }

    public readonly struct PollyLateAction<TInput> : IPollyAction<TInput>
    {
        private readonly Action<TInput> _action;

        public PollyLateAction(Action<TInput> action)
        {
            _action = action;
        }

        public void Execute(TInput input1)
        {
            _action(input1);
        }
    }
}
