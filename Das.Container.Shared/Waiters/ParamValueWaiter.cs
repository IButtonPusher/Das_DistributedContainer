using System;
using System.Threading.Tasks;

namespace Das.Container;

public class ParamValueWaiter : TaskCompletionSource<Object?>
{
   public ParamValueWaiter(Task<Object> promise,
                           Object?[] resultHolder,
                           Int32 valueIndex,
                           Object resultLock)
      #if NET40
      #else
      : base(TaskCreationOptions.RunContinuationsAsynchronously)
   #endif
   {
      _resultHolder = resultHolder;
      _valueIndex = valueIndex;
      _resultLock = resultLock;
      promise.ContinueWith(ValueIsReady);
   }

   private void ValueIsReady(Task<Object> promise)
   {
      var value = promise.Result;

      lock (_resultLock)
      {
         _resultHolder[_valueIndex] = value;
      }

      TrySetResult(value);
   }

   private readonly Object?[] _resultHolder;
   private readonly Object _resultLock;
   private readonly Int32 _valueIndex;
}