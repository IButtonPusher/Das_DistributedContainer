using System;
using System.Threading.Tasks;

namespace Das.Container
{
    public class ParamValueWaiter : TaskCompletionSource<Object?>
    {
        private readonly Object?[] _resultHolder;
        private readonly Int32 _valueIndex;
        private readonly Object _resultLock;

        public ParamValueWaiter(//IEnumerable<Task<Object>> possibleSources,
                Task<Object> promise,
                                Object?[] resultHolder,
                                Int32 valueIndex,
                                Object  resultLock)
            #if NET40
            #else
            : base(TaskCreationOptions.RunContinuationsAsynchronously)
            #endif
        {
            _resultHolder = resultHolder;
            _valueIndex = valueIndex;
            _resultLock = resultLock;
            promise.ContinueWith(ValueIsReady);
            //var someone = System.Threading.Tasks.Task.WhenAny<Object>(possibleSources);
            //someone.ContinueWith(ValueIsAlmostReady);
        }

        //private void ValueIsAlmostReady(Task<Task<Object>> promiseOfAPromise)
        //{
        //    var promise = promiseOfAPromise.Result;
        //    promise.ContinueWith(ValueIsReady);
        //}

        private void ValueIsReady(Task<Object> promise)
        {
            var value = promise.Result;

            lock (_resultLock)
            {
                _resultHolder[_valueIndex] = value;
            }

            TrySetResult(value);
        }
    }
}
