using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
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
       

       

       

       

        //private async Task<Object?[]?> GetConstructorArgsAsync(ConstructorInfo ctor,
        //                                                       Object[] ctorParams)
        //{
        //    if (ctor.GetParameters().Length == 0)
        //        return _emptyCtorParams;

        //    var ok = GetConstructorArgsAsyncImpl(ctor, ctorParams);
        //    return await ok.ConfigureAwait(false);
        //}


        //private async Task<Object?[]?> GetConstructorArgsAsyncImpl(ConstructorInfo ctor,
        //                                                           Object[] ctorParams)
        //{
        //    var ctorWorker = new ConstructorWorker(ctor, ctorParams);
        //    var running = new List<Task>();

        //    foreach (var missing in ctorWorker.BuildValues())
        //    {
        //        running.Add(MapResolveAndSetAsync(missing, ctorWorker));
        //    }


        //    #if NET40
        //    await TaskEx.WhenAll(running).ConfigureAwait(false);

        //    #else
        //    await Task.WhenAll(running).ConfigureAwait(false);

        //    #endif


        //    var args = ctorWorker.GetParameterValues();
        //    return args;
        //}

       

        ///// <summary>
        /////     Circles back to (2) PerformObjectResolutionAsync to avoid multiple instantiations
        ///// </summary>
        //private async Task MapResolveAndSetAsync(Tuple<Int32, ParameterInfo> item,
        //                                         ConstructorWorker ctorWorker)
        //{
        //    try
        //    {
        //        var contractType = item.Item2.ParameterType;
        //        Object? letsUse;

        //        if (typeof(Task).IsAssignableFrom(contractType))
        //        {
        //            var gargs = contractType.GetGenericArguments();

        //            if (!contractType.IsGenericType || gargs.Length != 1)
        //                throw new InvalidOperationException();

        //            var gObj = await PerformObjectResolutionAsync(gargs[0], _emptyCtorParams,
        //                //cancellationToken, 
        //                GetDefaultCancellationToken(),
        //                true);
        //            //var gObj = await MapAndResolveAsync(gargs[0], cancellationToken).ConfigureAwait(false);

        //            #if NET40
        //            var gTaskFromResult = typeof(TaskEx).GetMethod(nameof(TaskEx.FromResult),
        //                                      BindingFlags.Static | BindingFlags.Public)
        //                                  ?? throw new MissingMethodException(nameof(TaskEx.FromResult));

        //            #else
        //            var gTaskFromResult = typeof(Task).GetMethod(nameof(Task.FromResult),
        //                                      BindingFlags.Static | BindingFlags.Public)
        //                                  ?? throw new MissingMethodException(nameof(Task.FromResult));

        //            #endif

        //            var fromResultMethod = gTaskFromResult.MakeGenericMethod(gargs[0]);
        //            letsUse = fromResultMethod.Invoke(null, new[] {gObj});

        //            ctorWorker.SetValue(item.Item1, letsUse);
        //        }
        //        else
        //            letsUse = await PerformObjectResolutionAsync(contractType, _emptyCtorParams,
        //                GetDefaultCancellationToken(),
        //                true);

        //        ctorWorker.SetValue(item.Item1, letsUse);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new AggregateException("Exception loading parameter: " + item.Item2.Name +
        //                                     " for ctor: " + ctorWorker.ConstructorBuilding.DeclaringType +
        //                                     "->" + ctorWorker.ConstructorBuilding, ex);
        //    }
        //}

        

        ///// <summary>
        /////     2. Lowest method that can be called and still avoid multiple instantiations
        ///// </summary>
        //private async Task<TContract> PerformObjectResolutionAsync<TContract>(Object[] ctorArgs,
        //                                                                      CancellationToken cancellationToken,
        //                                                                      Boolean isWaitIfNotFound)
        //{
        //    var working = _contractBuilders.GetOrAdd(contractType, t =>
        //        PerformResolutionAsync(ctorArgs, cancellationToken,
        //            isWaitIfNotFound, t));

        //    var res = await working;
        //    return res;
        //}

       

       

        private static Boolean CanInstantiate(Type instanceType, 
                                              out ConstructorInfo ctor)
        {
            if (!instanceType.IsAbstract && !instanceType.IsInterface &&
                TryGetConstructor(instanceType, out ctor))
                return true;

            ctor = default!;
            return false;

            //if (contractType.IsAbstract || contractType.IsInterface)
            //    return false;

            //if (!TryGetConstructor(contractType, out var ctor))
            //    return false;
        }

       

        //private readonly ConcurrentDictionary<Type, Task<Object>> _contractBuilders;
        private readonly ConcurrentDictionary<Type, IDeferredLoader> _contractBuilders2;
    }
}
