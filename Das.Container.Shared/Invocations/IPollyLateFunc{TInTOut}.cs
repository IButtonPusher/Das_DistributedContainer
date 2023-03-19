using System;

namespace Das.Container.Invocations
{
    public interface IPollyLateFunc<in TIn, out TResult>
    {
        TResult Execute(TIn p);
    }
}
