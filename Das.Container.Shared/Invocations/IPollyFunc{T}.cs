using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public interface IPollyFunc<out TResult>
    {
        TResult Execute();
    }
}
