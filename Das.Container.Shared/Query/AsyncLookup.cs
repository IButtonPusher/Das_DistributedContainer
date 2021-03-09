using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Das.Container.Construction;

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

        private static Boolean CanInstantiate(Type instanceType,
                                              out ConstructorInfo ctor)
        {
            if (!instanceType.IsAbstract && !instanceType.IsInterface &&
                TryGetConstructor(instanceType, out ctor))
                return true;

            ctor = default!;
            return false;
        }

        /// <summary>
        ///     2. Lowest method that can be called and still avoid multiple instantiations
        ///     ...ALL ROADS LEAD THROUGH HERE...
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
                return found;

            //use an existing waiter if one exists
            var waiter = await _instanceMappings.TryGetWaiterAsync(typeO, cancellationToken);
            if (waiter != null)
                return waiter.GetAwaiter();

            if (!CanInstantiate(typeO, out var ctor))
            {
                //we can't construct our own so we have to wait
                found = await GetContainedAsync(contractType, typeO, cancellationToken, isWaitIfNotFound)
                    .ConfigureAwait(false);

                return found ?? throw new NullReferenceException();
            }

            //if someone is already constructing, use that
            if (_contractBuilders.TryGetValue(contractType, out var bldr))
            {
                found = await bldr.GetAwaiter();
                return found;
            }

            IObjectBuilder newObjBldr = ctor.GetParameters().Length > 0
                ? new NewInstanceBuilder(ctor, contractType, PerformObjectResolutionAsync, ctorArgs)
                : new DefaultCtorBuilder(ctor);

            bldr = _contractBuilders.GetOrAdd(contractType, newObjBldr);

            if (ReferenceEquals(bldr, newObjBldr))
                newObjBldr.BeginBuilding();

            found = await bldr.GetAwaiter();

            if (ReferenceEquals(bldr, newObjBldr))
                await RegisterInstanceImpl(found, typeO, contractType, cancellationToken, false);


            return found;
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
    }
}
