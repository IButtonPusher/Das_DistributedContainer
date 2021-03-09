using System;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Container
{
    public partial class BaseResolver
    {
        public Task ResolveToAsync<TContract, TObject>()
            where TObject : class, TContract
        {
            return _typeMappings.SetMappingAsync(typeof(TContract), typeof(TObject), true,
                GetDefaultCancellationToken());
        }

        public async Task ResolveToAsync<TContract, TObject>(TObject instance) 
            where TObject : class, TContract
        {
            var token = GetDefaultCancellationToken();
            await RegisterInstanceImpl(instance, typeof(TObject), 
                typeof(TContract), token, true).ConfigureAwait(false);
        }

        public virtual async Task ResolveToAsync<TInstance>(TInstance instance)
        {
            //Object oInstance = instance!;
            //var instType = oInstance.GetType();

            var token = GetDefaultCancellationToken();

            //var t = typeof(TInstance);

            await RegisterInstanceImpl(instance!, typeof(TInstance), typeof(TInstance), 
                token, true).ConfigureAwait(false);

            //await _typeMappings.SetMappingAsync(t, instType, true, token).ConfigureAwait(false);
            //await _instanceMappings.SetMappingAsync(t, oInstance, true, token).ConfigureAwait(false);
        }

        private async Task<Object?> RegisterInstanceImpl(Object instance,
                                                         Type instanceType,
                                                         Type contractType,
                                                         CancellationToken token,
                                                         Boolean setTypeMapping)
        {
            if (setTypeMapping)
                await _typeMappings.SetMappingAsync(contractType, instanceType, true, token).
                                    ConfigureAwait(false);
            var res = await _instanceMappings.SetMappingAsync(instanceType, instance, true, token)
                                             .ConfigureAwait(false);

            foreach (var bldr in _contractBuilders2.Values)
            {
                bldr.NotifyOfLoad(instance, contractType);
            }

            return res;
        }

        //private async Task<Object?> RegisterInstanceImpl<TInstance>(TInstance instance,
        //                                                           Type contractType,
        //                                                           CancellationToken token)
        //{
        //    Object oInstance = instance!;
        //    var instType = oInstance.GetType();

        //    await _typeMappings.SetMappingAsync(contractType, instType, true, token).
        //                        ConfigureAwait(false);
        //    var res = await _instanceMappings.SetMappingAsync(instType, oInstance, true, token).
        //                            ConfigureAwait(false);

        //    foreach (var bldr in _contractBuilders2.Values)
        //    {
        //        bldr.NotifyOfLoad(instance);
        //    }

        //    return res;
        //}
    }
}
