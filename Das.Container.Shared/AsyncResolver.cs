using System;
using System.Reflection;
using System.Threading;
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

            await _typeMappings.SetMappingAsync(typeof(TInterface), instType, true, token);
            await __instanceMappings.SetMappingAsync(typeof(TInterface), oInstance, true, token);
        }

        public Task<T> ResolveAsync<T>(CancellationToken cancellation)
        {
            return ResolveAsyncImpl<T>(_emptyCtorParams, cancellation, true);
        }

        public async Task<Object> ResolveAsync(Type type,
                                               CancellationToken cancellation)
        {
            var typeO = await _typeMappings.GetMappingAsync(type, cancellation, true).ConfigureAwait(false);
            typeO = EnsureNotNull(typeO, type);

            return await ResolveAsyncImpl<Object>(type, typeO, _emptyCtorParams,
                cancellation).ConfigureAwait(false);
        }


        private async Task<Object?[]> GetConstructorArgsAsync(ConstructorInfo ctor,
                                                              Object[] ctorParams,
                                                              CancellationToken cancellationToken)
        {
            if (ctor.GetParameters().Length == 0)
                return _emptyCtorParams;

            var ctorWorker = new ConstructorWorker(ctor, ctorParams);

            foreach (var missing in ctorWorker.BuildValues())
            {
                var pType = missing.Item2.ParameterType;
                var pMap = await _typeMappings.GetMappingAsync(pType, cancellationToken, true);
                pMap = EnsureNotNull(pMap, pType);
                var pObj = await ResolveObjectImplAsync(pType, pMap, _emptyCtorParams,
                    cancellationToken).ConfigureAwait(false);
                ctorWorker.SetValue(missing.Item1, pObj);
            }

            var args = ctorWorker.GetParameterValues();
            return args;
        }

        private async Task<T> ResolveAsyncImpl<T>(Object[] ctorArgs,
                                                  CancellationToken cancellationToken,
                                                  Boolean isWaitIfNotFound)
        {
            var typeI = typeof(T);
            var typeO = await _typeMappings.GetMappingAsync(typeI, cancellationToken, isWaitIfNotFound);
            typeO = EnsureNotNull(typeO, typeI);

            return await ResolveAsyncImpl<T>(typeI, typeO, ctorArgs, cancellationToken);
        }

        private async Task<TInterface> ResolveAsyncImpl<TInterface>(Type typeI,
                                                                    Type typeO,
                                                                    Object[] ctorParams,
                                                                    CancellationToken cancellation)
        {
            var res = await ResolveObjectImplAsync(typeI, typeO, ctorParams, cancellation);


            if (res is TInterface good)
                return good;

            throw new NullReferenceException("Unable to resolve an object of type " + typeI);
        }

        private async Task<Object?> ResolveObjectImplAsync(Type typeI,
                                                           Type typeO,
                                                           Object[] ctorParams,
                                                           CancellationToken cancellationToken)
        {
            var found = await GetContainedAsync(typeI, typeO, cancellationToken, false);
            if (found != null)
                return found;

            var ctor = GetConstructor(typeO);
            var args = await GetConstructorArgsAsync(ctor, ctorParams, cancellationToken);


            var res = ctor.Invoke(args);

            if (res is IInitializeAsync initAsync)
                await initAsync.InitializeAsync().ConfigureAwait(false);

            res = await __instanceMappings.SetMappingAsync(typeI, res, false, cancellationToken);

            return res;
        }


       
    }

    
}