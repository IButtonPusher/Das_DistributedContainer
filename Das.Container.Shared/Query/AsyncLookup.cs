using System;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Container
{
    public partial class BaseResolver
    {
        public virtual Task<T> ResolveAsync<T>()
        {
            return ResolveAsyncImpl<T>(_emptyCtorParams, GetDefaultCancellationToken(), true);
        }

        public Task<T> ResolveAsync<T>(CancellationToken cancellation)
        {
            return ResolveAsyncImpl<T>(_emptyCtorParams, cancellation, true);
        }

        public async Task<Object> ResolveAsync(Type type,
                                               CancellationToken cancellation)
        {
            var res = await PerformObjectResolutionAsync(type, _emptyCtorParams, cancellation,
                true).ConfigureAwait(false);

            return res ?? throw new NullReferenceException();
        }

        public Task<T> ResolveAsync<T>(params Object[] ctorArgs)
        {
            return ResolveAsyncImpl<T>(ctorArgs, GetDefaultCancellationToken(), false);
        }

        public Task<Object> ResolveAsync(Type type)
        {
            return ResolveAsync(type, GetDefaultCancellationToken());
        }

        /// <summary>
        ///     1.
        /// </summary>
        private async Task<T> ResolveAsyncImpl<T>(Object[] ctorArgs,
                                                  CancellationToken cancellationToken,
                                                  Boolean isWaitIfNotFound)
        {
            var typeI = typeof(T);

            var res = await PerformObjectResolutionAsync(typeI, ctorArgs,
                cancellationToken, isWaitIfNotFound);

            switch (res)
            {
                case T good:
                    return good;

                default:
                    throw new InvalidCastException();
            }
        }

        /// <summary>
        ///     2. Lowest method that can be called and still avoid multiple instantiations
        ///         ...ALL ROADS LEAD THROUGH HERE...
        /// </summary>
        private async Task<Object> PerformObjectResolutionAsync(Type contractType,
                                                                Object[] ctorArgs,
                                                                CancellationToken cancellationToken,
                                                                Boolean isWaitIfNotFound)
        {
            var typeO = await _typeMappings.GetMappingAsync(contractType, cancellationToken, 
                isWaitIfNotFound);

            typeO = EnsureNotNull(typeO, contractType);

            //try to load it immediately if it's available
            var found = await GetContainedAsync(contractType, typeO, cancellationToken, false)
                .ConfigureAwait(false);
            if (found != null)
            {
                if (found is Task)
                {}
                return found;
            }

            //use an existing waiter if one exists
            var waiter = await _instanceMappings.TryGetWaiterAsync(typeO, cancellationToken);
            if (waiter != null)
                return waiter.GetAwaiter();

            if (!CanInstantiate(typeO, out var ctor))
            {
                //we can't construct our own so we have to wait
                found = await GetContainedAsync(contractType, typeO, cancellationToken, isWaitIfNotFound)
                    .ConfigureAwait(false);

                if (found is Task)
                {}

                return found ?? throw new NullReferenceException();
            }

            //if someone is already constructing, use that
            if (_contractBuilders2.TryGetValue(contractType, out var bldr))
            {
                found = await bldr.GetAwaiter();

                if (found is Task)
                {}

                return found;
            }

            var newObjBldr = new NewInstanceBuilder(ctor, contractType, //_instanceMappings,
                PerformObjectResolutionAsync, ctorArgs);

            bldr = _contractBuilders2.GetOrAdd(contractType, newObjBldr);
            if (ReferenceEquals(bldr, newObjBldr))
                newObjBldr.BeginBuilding();

            found = await bldr.GetAwaiter();

            if (found is Task)
            {}

            if (ReferenceEquals(bldr, newObjBldr))
            {
                //var found2 = await GetContainedAsync(contractType, typeO, cancellationToken, false)
                //    .ConfigureAwait(false);

                await RegisterInstanceImpl(found, typeO, contractType, cancellationToken, false);
            }

            return found;

            //var working = _contractBuilders.GetOrAdd(contractType, t =>
            //    PerformResolutionAsync(ctorArgs, cancellationToken,
            //        isWaitIfNotFound, t));

            //var res = await working;
            //return res;
        }

        ///// <summary>
        /////     3. Not protected by _contractBuilders. Should only be called by PerformObjectResolutionAsync
        /////     aka once per contract type
        ///// </summary>
        //private Task<Object> PerformResolutionAsync(Object[] ctorArgs,
        //                                            CancellationToken cancellationToken,
        //                                            Boolean isWaitIfNotFound,
        //                                            Type contractType)
        //{
        //    var worker = new PollyFunc<Object[], CancellationToken, Boolean, Type, Task<Object?>>(
        //        PerformResolutionAsyncImpl, ctorArgs, cancellationToken, isWaitIfNotFound, contractType);
        //    var completion = new InstanceCompletionSource(worker, contractType);
        //    return completion.Task;
        //}

        ///// <summary>
        /////     4. Not protected by _contractBuilders. Should only be called by PerformResolutionAsync
        /////     via the PollyFunc/CompletionSource
        ///// </summary>
        //private async Task<Object?> PerformResolutionAsyncImpl(Object[] ctorArgs,
        //                                                       CancellationToken cancellationToken,
        //                                                       Boolean isWaitIfNotFound,
        //                                                       Type typeI)
        //{
            

        //    if (!typeI.IsAbstract && !typeI.IsInterface)
        //        return await InstantiateObjectImplAsync(typeI, typeI, ctorArgs, cancellationToken);

        //    var typeO = await _typeMappings.GetMappingAsync(typeI, cancellationToken, isWaitIfNotFound);
        //    typeO = EnsureNotNull(typeO, typeI);

        //    return await InstantiateObjectImplAsync(typeI, typeO, ctorArgs, cancellationToken);
        //}

        ///// <summary>
        /////     5.
        ///// </summary>
        //private async Task<Object?> InstantiateObjectImplAsync(Type contractType,
        //                                                       Type typeO,
        //                                                       Object[] ctorParams,
        //                                                       CancellationToken cancellationToken)
        //{
        //    var found = await GetContainedAsync(contractType, typeO, cancellationToken, false)
        //        .ConfigureAwait(false);
        //    if (found != null)
        //        return found;

        //    var ctor = GetConstructor(typeO);
        //    //Debug.WriteLine("get ctor items for " + ctor.DeclaringType + " via " +
        //    //                typeI);
        //    var args = await GetConstructorArgsAsync(ctor, ctorParams)
        //        .ConfigureAwait(false);

        //    if (args == null)
        //        return default;

        //    var res = ctor.Invoke(args);

        //    if (res is IInitializeAsync initAsync)
        //        await initAsync.InitializeAsync().ConfigureAwait(false);

        //    res = await RegisterInstanceImpl(res, contractType, cancellationToken)
        //        .ConfigureAwait(false);

        //    //res = await _instanceMappings.SetMappingAsync(contractType, res, false, cancellationToken)
        //    //                             .ConfigureAwait(false);

        //    return res;
        //}
    }
}
