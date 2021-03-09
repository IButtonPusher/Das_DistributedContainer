using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
#if !NET40
using TaskEx = System.Threading.Tasks.Task;
#endif



namespace Das.Container
{
    public delegate Task<Object> PerformObjectResolutionAsync(Type contractType,
                                                              Object[] ctorArgs,
                                                              CancellationToken cancellationToken,
                                                              Boolean isWaitIfNotFound);

    public class NewInstanceBuilder : TaskCompletionSource<Object>,
                                      IDeferredLoader
    {
        //private readonly IPollyPromise<Object?> _builder;
        private readonly ConstructorInfo _ctor;
        private readonly Type _contractType;
        //private readonly TypeMappingCollection<Object> _instanceMappings;

        private readonly PerformObjectResolutionAsync _objectResolution;

        private readonly Object[] _providedCtorParams;
        //private readonly TypeMappingCollection<Type> _typeMappings;

        private readonly Dictionary<Type, Int32> _paramTypeIndexMapping;
        private readonly Object?[] _paramValues;

        private readonly Dictionary<Type, ParamValueWaiter> _paramValueWaiters;

        private static readonly Object[] _emptyCtorParams = new Object[0];

        private readonly Object _lock;

        public NewInstanceBuilder( //IPollyPromise<Object?> builder,
                ConstructorInfo ctor,
                Type contractType,
              //  TypeMappingCollection<Object> instanceMappings,
                PerformObjectResolutionAsync objectResolution,
                Object[] providedCtorParams)
            //TypeMappingCollection<Type> typeMappings,
            
            #if NET40
            #else
            : base(TaskCreationOptions.RunContinuationsAsynchronously)
        #endif
        {
            _lock = new Object();

            //_builder = builder;
            _ctor = ctor;
            _contractType = contractType;
            //_typeMappings = typeMappings;
            //_instanceMappings = instanceMappings;
            _objectResolution = objectResolution;
            _providedCtorParams = providedCtorParams;

            _paramTypeIndexMapping = new Dictionary<Type, Int32>();
            _paramValueWaiters = new Dictionary<Type, ParamValueWaiter>();

            var ctorParams = ctor.GetParameters();
            _paramValues = new Object?[ctorParams.Length];

            for (var c = 0; c < ctorParams.Length; c++)
            {
                if (_paramTypeIndexMapping.TryGetValue(ctorParams[c].ParameterType, out _))
                {
                    //if we have multiple params of the same type then we can't accept outside help
                    _paramTypeIndexMapping[ctorParams[c].ParameterType] = -1;
                }
                else
                    _paramTypeIndexMapping[ctorParams[c].ParameterType] = c;
            }
        }

        public void BeginBuilding()
        {
            //var building = _builder.ExecuteAsync();
            //building.ContinueWith(OnBuilt);

            var bulding = InstantiateObjectImplAsync();
            bulding.ContinueWith(OnBuilt);
        }

        private void OnBuilt(Task<Object?> promise)
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
                return;
                //SetCanceled();
            }

            var res = promise.Result;
            if (res != default)
                TrySetResult(res);
        }

        //public void NotifyOfLoad<T>(T obj)
        //{
        //    lock (_lock)
        //    {
        //        if (_paramTypeIndexMapping.TryGetValue(typeof(T), out var pIndex))
        //        {
        //            _paramTypeIndexMapping[typeof(T)] = -1;
        //        }
        //        else return;

        //        if (pIndex == -1 || _paramValues[pIndex] != null)
        //            return;

        //        _paramValues[pIndex] = obj;

        //        if (_paramValueWaiters.TryGetValue(typeof(T), out var waiter))
        //            waiter.TrySetResult(obj);
        //    }

        //    OnNewValueAvailable();
        //}

        public void NotifyOfLoad(Object obj,
                                 Type contractType)
        {
            lock (_lock)
            {
                if (_paramTypeIndexMapping.TryGetValue(contractType, out var pIndex))
                {
                    _paramTypeIndexMapping[contractType] = -1;
                }
                else return;

                if (pIndex == -1 || _paramValues[pIndex] != null)
                    return;

                _paramValues[pIndex] = obj;

                if (_paramValueWaiters.TryGetValue(contractType, out var waiter))
                    waiter.TrySetResult(obj);
            }

            //OnNewValueAvailable();
        }

        //private void OnNewValueAvailable()
        //{
        //    throw new NotImplementedException();
        //}

        Task<Object> IDeferredLoader.GetAwaiter()
        {
            return Task;
        }

        /// <summary>
        ///     5.
        /// </summary>
        private async Task<Object?> InstantiateObjectImplAsync()
            //Type contractType,
            //                                                   Type typeO,
            //                                                   Object[] ctorParams,
                                                               //CancellationToken cancellationToken)
        {
            //var found = await GetContainedAsync(contractType, typeO, cancellationToken, false)
            //    .ConfigureAwait(false);
            //if (found != null)
            //    return found;

            //var ctor = GetConstructor(typeO);
            //Debug.WriteLine("get ctor items for " + ctor.DeclaringType + " via " +
            //                typeI);
            var args = await GetConstructorArgValuesAsync().ConfigureAwait(false);

            if (args == null)
                return default;

            var res = _ctor.Invoke(args);

            if (res is IInitializeAsync initAsync)
                await initAsync.InitializeAsync().ConfigureAwait(false);

            //res = await RegisterInstanceImpl(res, contractType, cancellationToken)
            //    .ConfigureAwait(false);

            //res = await _instanceMappings.SetMappingAsync(contractType, res, false, cancellationToken)
            //                             .ConfigureAwait(false);

            return res;
        }

        private async Task<Object?[]?> GetConstructorArgValuesAsync()
            //ConstructorInfo ctor,
            //                                                   Object[] ctorParams)
        {
            if (_ctor.GetParameters().Length == 0)
            {
                //you're not supposed to be here...
                return _emptyCtorParams;
            }

            var ok = GetConstructorArgsAsyncImpl(_ctor, _providedCtorParams);
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

            await TaskEx.WhenAll(running).ConfigureAwait(false);

            


            var args = ctorWorker.GetParameterValues();
            return args;
        }

        private async Task MapResolveAndSetAsync(Tuple<Int32, ParameterInfo> item,
                                                 ConstructorWorker ctorWorker)
        {
             try
            {
                var contractType = item.Item2.ParameterType;
                Object? letsUse;

                if (typeof(Task).IsAssignableFrom(contractType))
                {
                    await PerformPromiseResolutionAsync(contractType, item.Item1, ctorWorker);

                    //var gargs = contractType.GetGenericArguments();

                    //if (!contractType.IsGenericType || gargs.Length != 1)
                    //    throw new InvalidOperationException();

                    //var promise = _instanceMappings.GetMappingAsync(contractType, 
                    //    CancellationToken.None, true);

                    //if (!promise.IsCompleted)
                    //{
                    //    var waiter = new ParamValueWaiter(promise, _paramValues, item.Item1, _lock);
                    //    lock (_lock)
                    //    {
                    //        _paramValueWaiters[contractType] = waiter;
                    //        promise = waiter.Task;
                    //    }
                    //}

                    //var gObj = await promise;


                    ////var gObj = await PerformObjectResolutionAsync(gargs[0], _emptyCtorParams,
                    ////    //cancellationToken, 
                    ////    GetDefaultCancellationToken(),
                    ////    true);
                    ////var gObj = await MapAndResolveAsync(gargs[0], cancellationToken).ConfigureAwait(false);

                    //#if NET40
                    //var gTaskFromResult = typeof(TaskEx).GetMethod(nameof(TaskEx.FromResult),
                    //                          BindingFlags.Static | BindingFlags.Public)
                    //                      ?? throw new MissingMethodException(nameof(TaskEx.FromResult));

                    //#else
                    //var gTaskFromResult = typeof(Task).GetMethod(nameof(Task.FromResult),
                    //                          BindingFlags.Static | BindingFlags.Public)
                    //                      ?? throw new MissingMethodException(nameof(Task.FromResult));

                    //#endif

                    //var fromResultMethod = gTaskFromResult.MakeGenericMethod(gargs[0]);
                    //letsUse = fromResultMethod.Invoke(null, new[] {gObj});

                    //ctorWorker.SetValue(item.Item1, letsUse);
                }
                else
                {
                    letsUse = await PerformObjectResolutionAsync(contractType, item.Item1);
                    ctorWorker.SetValue(item.Item1, letsUse);
                }
            }
            catch (Exception ex)
            {
                throw new AggregateException("Exception loading parameter: " + item.Item2.Name +
                                             " for ctor: " + ctorWorker.ConstructorBuilding.DeclaringType +
                                             "->" + ctorWorker.ConstructorBuilding, ex);
            }
        }

        private async Task PerformPromiseResolutionAsync(Type contractType,
                                                                 Int32 valueIndex,
                                                                 ConstructorWorker ctorWorker)
        {
            var gargs = contractType.GetGenericArguments();

            if (!contractType.IsGenericType || gargs.Length != 1)
                throw new InvalidOperationException();


            //_objectResolution(gargs[0], _emptyCtorParams, 
            //    CancellationToken.None, true)

            var gObj = await PerformObjectResolutionAsync(gargs[0], valueIndex);


            //var gObj = await PerformObjectResolutionAsync(gargs[0], _emptyCtorParams,
            //    //cancellationToken, 
            //    GetDefaultCancellationToken(),
            //    true);
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
            var letsUse = fromResultMethod.Invoke(null, new[] {gObj});
            ctorWorker.SetValue(valueIndex, letsUse);
        }

        private async Task<Object> PerformObjectResolutionAsync(Type contractType,
                                                                Int32 valueIndex)
        {
            var promise = _objectResolution(contractType, _emptyCtorParams,
                CancellationToken.None, true);

            if (!promise.IsCompleted)
            {
                var waiter = new ParamValueWaiter(promise, _paramValues, valueIndex, _lock);
                lock (_lock)
                {
                    _paramValueWaiters[contractType] = waiter;
                    promise = waiter.Task!;
                }
            }

            return await promise;
        }


        //private async Task<Object?> GetContainedAsync(Type typeI,
        //                                              Type typeO,
        //                                              CancellationToken cancellationToken,
        //                                              Boolean isWaitIfNotFound)
        //{
        //    var found = await _instanceMappings.GetMappingAsync(typeI, cancellationToken, isWaitIfNotFound);
        //    // ReSharper disable once ConstantNullCoalescingCondition
        //    found ??= await _instanceMappings.TryGetMappingByConcreteAsync(typeO, cancellationToken);

        //    found = Util.ValidateTypes(found, typeI, typeO)!;
        //    return found;
        //}
    }
}
