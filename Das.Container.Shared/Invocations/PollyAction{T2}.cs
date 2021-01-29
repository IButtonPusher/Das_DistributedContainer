using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public readonly struct PollyAction<TInput1, TInput2> : IPollyAction
    {
        private readonly TInput1 _input1;
        private readonly TInput2 _input2;
        private readonly Action<TInput1, TInput2> _action;

        public PollyAction(TInput1 input1,
                           TInput2 input2,
                           Action<TInput1, TInput2> action)
        {
            _input1 = input1;
            _input2 = input2;
            _action = action;
        }

        public void Execute()
        {
            _action(_input1, _input2);
        }
    }
}
