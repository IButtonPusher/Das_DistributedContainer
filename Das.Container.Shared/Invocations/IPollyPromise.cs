using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public interface IPollyPromise<TResult>
    {
        Task<TResult> ExecuteAsync();

        Boolean TryCancel();
    }
}
