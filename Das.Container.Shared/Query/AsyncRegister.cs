using System;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Container;

public partial class BaseResolver
{
   public Task ResolveToAsync<TContract, TObject>()
      where TObject : class, TContract =>
      _typeMappings.SetMappingAsync(typeof(TContract), typeof(TObject), true,
         GetDefaultCancellationToken());

   public virtual async Task ResolveToAsync<TInstance>(TInstance instance)
   {
      var token = GetDefaultCancellationToken();

      await RegisterInstanceImpl(instance!, typeof(TInstance), typeof(TInstance),
            token, true)
         .ConfigureAwait(false);
   }

   public async Task ResolveToAsync<TContract, TObject>(TObject instance)
      where TObject : class, TContract
   {
      var token = GetDefaultCancellationToken();
      await RegisterInstanceImpl(instance, typeof(TObject),
            typeof(TContract), token, true)
         .ConfigureAwait(false);
   }

   private async Task RegisterInstanceImpl(Object instance,
                                           Type instanceType,
                                           Type contractType,
                                           CancellationToken token,
                                           Boolean setTypeMapping)
   {
      if (setTypeMapping)
         await _typeMappings.SetMappingAsync(contractType, instanceType, true, token).ConfigureAwait(false);
      await _instanceMappings.SetMappingAsync(instanceType, instance, true, token)
                             .ConfigureAwait(false);

      foreach (var bldr in _contractBuilders.Values)
      {
         bldr.NotifyOfLoad(instance, contractType);
      }
   }
}
