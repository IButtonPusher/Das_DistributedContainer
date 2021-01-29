using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    /// <summary>
    ///     Idea from: https://github.com/App-vNext/Polly/issues/271
    /// </summary>
    public interface IPollyAction<in TInput>
    {
        void Execute(TInput input);
    }
}
