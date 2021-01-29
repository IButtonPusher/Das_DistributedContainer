using System;
using System.Threading.Tasks;

namespace Das.Container.Invocations
{
    public class InstanceCompletionSource : TaskCompletionSource<Object>
    {
        private readonly Type _contractType;

        public InstanceCompletionSource(IPollyFunc<Task<Object>> builder,
                                        Type contractType)
            #if !NET40
            : base(TaskCreationOptions.RunContinuationsAsynchronously)
        #endif
        {
            _contractType = contractType;
            var building = builder.Execute();
            building.ContinueWith(OnBuilt);
        }

        private void OnBuilt(Task<Object> promise)
        {
            if (promise.IsFaulted)
            {
                SetException(promise.Exception ?? new Exception());
                return;
                //throw promise.Exception ?? new Exception();
            }

            if (promise.IsCanceled)
            {
                SetException(new Exception("Unable to resolve contract: " + _contractType));
                //SetCanceled();
            }

            else SetResult(promise.Result);
        }
    }
}
