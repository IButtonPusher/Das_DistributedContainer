using System;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Container;

public class MappingCompletionSource<TValue> : TaskCompletionSource<TValue>,
                                               IDeferredLoader<TValue>
{
   public MappingCompletionSource(CancellationToken cancellationToken)
      #if NET40
      #else
      : base(TaskCreationOptions.RunContinuationsAsynchronously)
   #endif
   {
      cancellationToken.Register(OnTokenCancelled);
   }


   public void NotifyOfLoad(Object obj,
                            Type contractType)
   {
   }

   async Task<Object> IDeferredLoader.GetAwaiter()
   {
      return (await Task)!;
   }


   private void OnTokenCancelled()
   {
      TrySetCanceled();
   }
}