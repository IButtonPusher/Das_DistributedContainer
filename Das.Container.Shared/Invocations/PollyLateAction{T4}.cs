using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public readonly struct PollyLateAction<TInput1, TInput2, TInput3, TInput4> : IPollyAction<TInput1>
    {
        private readonly TInput2 _input2;
        private readonly TInput3 _input3;
        private readonly TInput4 _input4;
        private readonly Action<TInput1, TInput2, TInput3, TInput4> _action;

        public PollyLateAction(
            TInput2 input2,
            TInput3 input3,
            TInput4 input4,
            Action<TInput1, TInput2, TInput3, TInput4> action)
        {
            _input2 = input2;
            _input3 = input3;
            _input4 = input4;
            _action = action;
        }

        public void Execute(TInput1 input1)
        {
            _action(input1, _input2, _input3, _input4);
        }
    }
}
