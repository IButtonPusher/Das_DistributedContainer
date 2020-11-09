using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using System.Threading.Tasks;
using Das.Container.Shared;

#if NET40
using SemaphoreSlim = System.Threading.AsyncSemaphore;
#else
using System.Threading;
#endif

namespace Das.Container
{
    // ReSharper disable once UnusedType.Global
    public class BaseResolver : IResolver
    {
        public BaseResolver()
        {

            _lockContained = new SemaphoreSlim(1);
            _containedObjects = new Dictionary<Type, Object>();
            _typeMappings = new Dictionary<Type, Type>();
            _emptyCtorParams = new Object[0];
        }

        public virtual Task<T> ResolveAsync<T>()
        {
            return ResolveAsync<T>(_emptyCtorParams);
        }

        public T Resolve<T>()
        {
            return Resolve<T>(_emptyCtorParams);
        }

        public async Task<T> ResolveAsync<T>(params Object[] ctorArgs)
        {
            var typeI = typeof(T);
            var typeO = await GetMappingTypeAsync(typeI);

            return await ResolveAsyncImpl<T>(typeI, typeO, ctorArgs);
        }

        public T Resolve<T>(params Object[] ctorArgs)
        {
            var typeI = typeof(T);
            var typeO = GetMappingType(typeI);

            return ResolveImpl<T>(typeI, typeO, ctorArgs);
        }


        public async Task ResolveToAsync<TInterface, TObject>() 
            where TObject : class, TInterface
        {
            await RunLockedAsync(typeof(TObject), o => _typeMappings[typeof(TInterface)] = o);
        }


        private async Task<TInterface> ResolveAsyncImpl<TInterface>(Type typeI,
                                                                    Type typeO,
                                                                    Object[] ctorParams)
        {
            var res = await RunLockedAsync(typeI, typeO, 
                async (i, o) => await ResolveAsyncNoLockImpl(i, o, ctorParams)
                    .ConfigureAwait(false))
                .ConfigureAwait(false);

            if (res is TInterface good)
                return good;

            throw new NullReferenceException("Unable to resolve an object of type " + typeI);
        }

        private TInterface ResolveImpl<TInterface>(Type typeI,
                                                                    Type typeO,
                                                                    Object[] ctorParams)
        {
            return ResolveAsyncImpl<TInterface>(typeI, typeO, ctorParams).Result;
        }

        private async Task<Object?> ResolveAsyncNoLockImpl(Type typeI,
                                                           Type typeO,
                                                           Object[] ctorParams)
        {
            if (_containedObjects.TryGetValue(typeI, out var found))
                return found;

            if (typeO.IsAbstract || typeO.IsInterface)
                throw new InvalidOperationException("Unable to map " + 
                                                    typeI.Name + " to " + 
                                                    typeO.Name + " - it is not an instantiable type");

            var ctors = typeO.GetConstructors();

            ConstructorInfo? ctor;

            if (ctors.Length != 1)
            {
                var dCtors = from c in ctors
                    let attrs = c.GetCustomAttributes(
                        typeof(ContainerConstructorAttribute), 
                        true).FirstOrDefault()
                    where c != null
                    select c;

                ctor = dCtors.FirstOrDefault();
                
                // !4.0
                //ctor = ctors.FirstOrDefault(c => c.GetCustomAttribute<ContainerConstructorAttribute>() != null);

                if (ctor == null)
                    throw new InvalidOperationException(
                        $"{typeO} must have exactly one accessible constructor or one of the constructors must be decorated with ContainerConstructorAttribute");
            }
            else
            {
                ctor = ctors[0];
            }


            var prms = ctor.GetParameters();

            var providedParamIndex = 0;

            var args = new Object[prms.Length];

            for (var c = 0; c < prms.Length; c++)
            {
                Object? pObj;

                var pType = prms[c].ParameterType;
                if (providedParamIndex < ctorParams.Length &&
                    pType.IsInstanceOfType(ctorParams[providedParamIndex]))
                    pObj = ctorParams[providedParamIndex++];

                else
                {
                    var pMap = GetMappingNoLock(pType);
                    pObj = await ResolveAsyncNoLockImpl(pType, pMap, _emptyCtorParams).ConfigureAwait(false);
                }

                switch (pObj)
                {
                    case null:
                        throw new Exception($"Cannot resolve ctor parameter of type {pType}");
                }

                args[c] = pObj;
            }

            var res = ctor.Invoke(args);

            if (res is IInitializeAsync initAsync)
                await initAsync.InitializeAsync().ConfigureAwait(false);

            _containedObjects[typeI] = res;

            return res;
        }

        public virtual void ResolveTo<TInterface>(TInterface instance)
        {
            RunLockedAction(instance, o => _containedObjects[typeof(TInterface)] = o!);
        }

        public virtual void ResolveTo<TInterface, TObject>()
            where TObject : class, TInterface
        {
            RunLockedAction(typeof(TObject), o => _typeMappings[typeof(TInterface)] = o);
        }

        public virtual async Task ResolveToAsync<TInterface>(TInterface obj)
        {
            await RunLockedAsync(obj, o => _containedObjects[typeof(TInterface)] = o!).
                ConfigureAwait(false);
        }

        public async Task<Object> ResolveAsync(Type type)
        {
            var typeO = await GetMappingTypeAsync(type).ConfigureAwait(false);

            return await ResolveAsyncImpl<Object>(type, typeO, _emptyCtorParams).ConfigureAwait(false);
        }

        public Object Resolve(Type type)
        {
            var typeO = GetMappingType(type);
            return ResolveImpl<Object>(type, typeO, _emptyCtorParams);
        }


        private async Task<Type> GetMappingTypeAsync(Type type)
        {
            return await RunLockedAsync(type, GetMappingNoLock).ConfigureAwait(false);
        }

        private Type GetMappingType(Type type)
        {
            return RunLockedFunc(type, GetMappingNoLock);
        }

        private Type GetMappingNoLock(Type tIn)
        {
            if (_typeMappings.TryGetValue(tIn, out var good))
                return good;

            foreach (var kvp in _typeMappings)
            {
                if (tIn.IsAssignableFrom(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            return tIn;
        }

        // ReSharper disable once UnusedMember.Local
        private async Task RunLocked<TInput>(TInput input, 
                                             Action<TInput> action)
        {
            await _lockContained.WaitAsync().ConfigureAwait(false);

            try
            {
                action(input);
            }
            finally
            {
                _lockContained.Release();
            }
        }

        private async Task<TOutput> RunLockedAsync<TInput, TOutput>(TInput input, 
                                                              Func<TInput, TOutput> func)
        {
            await _lockContained.WaitAsync().ConfigureAwait(false);

            try
            {
                return func(input);
            }
            finally
            {
                _lockContained.Release();
            }
        }

        private TOutput RunLockedFunc<TInput, TOutput>(TInput input,
                                                       Func<TInput, TOutput> func)
        {
            _lockContained.Wait();

            try
            {
                return func(input);
            }
            finally
            {
                _lockContained.Release();
            }
        }

        private void RunLockedAction<TInput>(TInput input, 
                                             Action<TInput> func)
        {
            _lockContained.Wait();

            try
            {
                func(input);
            }
            finally
            {
                _lockContained.Release();
            }
        }

        // ReSharper disable once UnusedMember.Local
        private async Task<TOutput> RunLockedAsync<TInput1, TOutput>(TInput1 input1,
                                                                     Func<TInput1, Task<TOutput>> action)
        {
            await _lockContained.WaitAsync().ConfigureAwait(false);

            try
            {
                return await action(input1).ConfigureAwait(false);
            }
            finally
            {
                _lockContained.Release();
            }
        }

        private async Task<TOutput> RunLockedAsync<TInput1, TInput2, TOutput>(TInput1 input1,
                                                            TInput2 input2,
                                                  Func<TInput1, TInput2, Task<TOutput>> action)
        {
            await _lockContained.WaitAsync().ConfigureAwait(false);

            try
            {
                return await action(input1, input2).ConfigureAwait(false);
            }
            finally
            {
                _lockContained.Release();
            }
        }

        protected readonly Dictionary<Type, Object> _containedObjects;
        private readonly Dictionary<Type, Type> _typeMappings;
        private readonly SemaphoreSlim _lockContained;
        private readonly Object[] _emptyCtorParams;
    }
}
