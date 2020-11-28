using System;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Container
{
    public class MappingCompletionSource<TValue> : TaskCompletionSource<TValue>
    {
        public MappingCompletionSource(CancellationToken cancellationToken)
            #if NET40
            : base()
        #else
        : base(TaskCreationOptions.RunContinuationsAsynchronously)
        #endif
        {
            cancellationToken.Register(OnTokenCancelled);
        }


        private void OnTokenCancelled()
        {
            TrySetCanceled();
        }
    }
}