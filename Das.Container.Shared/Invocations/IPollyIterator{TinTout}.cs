using System;
using System.Collections.Generic;

namespace Das.Container.Invocations
{
    public interface IPollyIterator<in TIn, out TOut>
    {
        IEnumerable<TOut> Execute(TIn arg);
    }
}
