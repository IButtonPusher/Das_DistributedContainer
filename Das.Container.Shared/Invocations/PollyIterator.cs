using System;
using System.Collections.Generic;

namespace Das.Container.Invocations
{
    public readonly struct PollyIterator<TIn, TOut> : IPollyIterator<TIn, TOut>
    {
        private readonly Func<TIn, IEnumerable<TOut>> _builder;

        public PollyIterator(Func<TIn, IEnumerable<TOut>> builder)
        {
            _builder = builder;
        }

        public IEnumerable<TOut> Execute(TIn arg)
        {
            foreach (var r in _builder(arg))
                yield return r;
        }
    }
}
