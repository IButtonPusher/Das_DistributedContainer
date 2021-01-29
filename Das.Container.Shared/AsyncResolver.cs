using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Das.Container.Invocations;
#if NET40
using SemaphoreSlim = System.Threading.AsyncSemaphore;
#else
using TaskEx = System.Threading.Tasks.Task;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
#endif

namespace Das.Container
{
    // ReSharper disable once UnusedType.Global
    public partial class BaseResolver
    {
        public virtual Task<T> ResolveAsync<T>()
        {
            return ResolveAsyncImpl<T>(_emptyCtorParams, GetDefaultCancellationToken(), true);
        }

        public Task<T> ResolveAsync<T>(params Object[] ctorArgs)
        {
            return ResolveAsyncImpl<T>(ctorArgs, GetDefaultCancellationToken(), false);
        }

        public Task<Object> ResolveAsync(Type type)
        {
            return ResolveAsync(type, GetDefaultCancellationToken());
        }

        public Task ResolveToAsync<TInterface, TObject>()
            where TObject : class, TInterface
        {
            return _typeMappings.SetMappingAsync(typeof(TInterface), typeof(TObject), true,
                GetDefaultCancellationToken());
        }

        public virtual async Task ResolveToAsync<TInterface>(TInterface instance)
        {
            Object oInstance = instance!;
            var instType = oInstance.GetType();

            var token = GetDefaultCancellationToken();

            await _typeMappings.SetMappingAsync(typeof(TInterface), instType, true, token).ConfigureAwait(false);
            await _instanceMappings.SetMappingAsync(typeof(TInterface), oInstance, true, token).ConfigureAwait(false);
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

        private async Task<Object?[]?> GetConstructorArgsAsync(ConstructorInfo ctor,
                                                               Object[] ctorParams)
        {
            if (ctor.GetParameters().Length == 0)
                return _emptyCtorParams;

            var ok = GetConstructorArgsAsyncImpl(ctor, ctorParams);
            return await ok.ConfigureAwait(false);
        }


        private async Task<Object?[]?> GetConstructorArgsAsyncImpl(ConstructorInfo ctor,
                                                                   Object[] ctorParams)
        {
            var ctorWorker = new ConstructorWorker(ctor, ctorParams);
            var running = new List<Task>();

            foreach (var missing in ctorWorker.BuildValues())
            {
                running.Add(MapResolveAndSetAsync(missing, ctorWorker));
            }


            #if NET40
            await TaskEx.WhenAll(running).ConfigureAwait(false);

            #else
            await Task.WhenAll(running).ConfigureAwait(false);

            #endif


            var args = ctorWorker.GetParameterValues();
            return args;
        }

        /// <summary>
        ///     5.
        /// </summary>
        private async Task<Object?> InstantiateObjectImplAsync(Type typeI,
                                                               Type typeO,
                                                               Object[] ctorParams,
                                                               CancellationToken cancellationToken)
        {
            var found = await GetContainedAsync(typeI, typeO, cancellationToken, false)
                .ConfigureAwait(false);
            if (found != null)
                return found;

            var ctor = GetConstructor(typeO);
            //Debug.WriteLine("get ctor items for " + ctor.DeclaringType + " via " +
            //                typeI);
            var args = await GetConstructorArgsAsync(ctor, ctorParams)
                .ConfigureAwait(false);

            if (args == null)
                return default;

            var res = ctor.Invoke(args);

            if (res is IInitializeAsync initAsync)
                await initAsync.InitializeAsync().ConfigureAwait(false);

            res = await _instanceMappings.SetMappingAsync(typeI, res, false, cancellationToken)
                                         .ConfigureAwait(false);

            return res;
        }

        /// <summary>
        ///     Circles back to (2) PerformObjectResolutionAsync to avoid multiple instantiations
        /// </summary>
        private async Task MapResolveAndSetAsync(Tuple<Int32, ParameterInfo> item,
                                                 ConstructorWorker ctorWorker)
        {
            try
            {
                var contractType = item.Item2.ParameterType;
                Object? letsUse;

                if (typeof(Task).IsAssignableFrom(contractType))
                {
                    var gargs = contractType.GetGenericArguments();

                    if (!contractType.IsGenericType || gargs.Length != 1)
                        throw new InvalidOperationException();

                    var gObj = await PerformObjectResolutionAsync(gargs[0], _emptyCtorParams,
                        //cancellationToken, 
                        GetDefaultCancellationToken(),
                        true);
                    //var gObj = await MapAndResolveAsync(gargs[0], cancellationToken).ConfigureAwait(false);

                    #if NET40
                    var gTaskFromResult = typeof(TaskEx).GetMethod(nameof(TaskEx.FromResult),
                                              BindingFlags.Static | BindingFlags.Public)
                                          ?? throw new MissingMethodException(nameof(TaskEx.FromResult));

                    #else
                    var gTaskFromResult = typeof(Task).GetMethod(nameof(Task.FromResult),
                                              BindingFlags.Static | BindingFlags.Public)
                                          ?? throw new MissingMethodException(nameof(Task.FromResult));

                    #endif

                    var fromResultMethod = gTaskFromResult.MakeGenericMethod(gargs[0]);
                    letsUse = fromResultMethod.Invoke(null, new[] {gObj});

                    ctorWorker.SetValue(item.Item1, letsUse);
                }
                else
                    letsUse = await PerformObjectResolutionAsync(contractType, _emptyCtorParams,
                        GetDefaultCancellationToken(),
                        true);

                ctorWorker.SetValue(item.Item1, letsUse);
            }
            catch (Exception ex)
            {
                throw new AggregateException("Exception loading parameter: " + item.Item2.Name +
                                             " for ctor: " + ctorWorker.ConstructorBuilding.DeclaringType +
                                             "->" + ctorWorker.ConstructorBuilding, ex);
            }
        }

        /// <summary>
        ///     2. Lowest method that can be called and still avoid multiple instantiations
        /// </summary>
        private async Task<Object> PerformObjectResolutionAsync(Type typeI,
                                                                Object[] ctorArgs,
                                                                CancellationToken cancellationToken,
                                                                Boolean isWaitIfNotFound)
        {
            var working = _contractBuilders.GetOrAdd(typeI, t =>
                PerformResolutionAsync(ctorArgs, cancellationToken,
                    isWaitIfNotFound, t));

            var res = await working;
            return res;
        }

        /// <summary>
        ///     3. Not protected by _contractBuilders. Should only be called by PerformObjectResolutionAsync
        ///     aka once per contract type
        /// </summary>
        private Task<Object> PerformResolutionAsync(Object[] ctorArgs,
                                                    CancellationToken cancellationToken,
                                                    Boolean isWaitIfNotFound,
                                                    Type contractType)
        {
            var worker = new PollyFunc<Object[], CancellationToken, Boolean, Type, Task<Object?>>(
                PerformResolutionAsyncImpl, ctorArgs, cancellationToken, isWaitIfNotFound, contractType);
            var completion = new InstanceCompletionSource(worker, contractType);
            return completion.Task;
        }

        /// <summary>
        ///     4. Not protected by _contractBuilders. Should only be called by PerformResolutionAsync
        ///     via the PollyFunc/CompletionSource
        /// </summary>
        private async Task<Object?> PerformResolutionAsyncImpl(Object[] ctorArgs,
                                                               CancellationToken cancellationToken,
                                                               Boolean isWaitIfNotFound,
                                                               Type typeI)
        {
            if (!typeI.IsAbstract && !typeI.IsInterface)
                return await InstantiateObjectImplAsync(typeI, typeI, ctorArgs, cancellationToken);

            var typeO = await _typeMappings.GetMappingAsync(typeI, cancellationToken, isWaitIfNotFound);
            typeO = EnsureNotNull(typeO, typeI);

            return await InstantiateObjectImplAsync(typeI, typeO, ctorArgs, cancellationToken);
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

        private readonly ConcurrentDictionary<Type, Task<Object>> _contractBuilders;
    }
}
