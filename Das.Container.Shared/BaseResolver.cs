using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Das.Container.Shared;

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

            //var typeI = typeof(T);
            //var typeO = await GetMappingType(typeI);

            //return ResolveAsync<T, T>();
        }

        public async Task<T> ResolveAsync<T>(params Object[] ctorArgs)
        {
            var typeI = typeof(T);
            var typeO = await GetMappingType(typeI);

            return await ResolveAsyncImpl<T>(typeI, typeO, ctorArgs);
        }

        public async Task ResolveToAsync<TInterface, TObject>() 
            where TObject : class, TInterface
        {
            await RunLocked(typeof(TObject), o => _typeMappings[typeof(TInterface)] = o);
        }

        public virtual Task<TInterface> ResolveAsync<TObject, TInterface>()
            where TObject : TInterface
        {
            return ResolveAsyncImpl<TObject, TInterface>(_emptyCtorParams);
        }

        private async Task<TInterface> ResolveAsyncImpl<TInterface>(Type typeI,
                                                                             Type typeO,
                                                                             Object[] ctorParams)
        {
            var res = await RunLockedAsync(typeI, typeO, 
                async (i, o) => await ResolveAsyncNoLockImpl(i, o, ctorParams));

            if (res is TInterface good)
                return good;

            throw new NullReferenceException("Unable to resolve an object of type " + typeI);
        }

        private Task<TInterface> ResolveAsyncImpl<TObject, TInterface>(Object[] ctorParams)
            where TObject : TInterface
        {
            var typeI = typeof(TInterface);
            var typeO = typeof(TObject);

            return ResolveAsyncImpl<TInterface>(typeI, typeO, ctorParams);
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
                ctor = ctors.FirstOrDefault(c => c.GetCustomAttribute<ContainerConstructorAttribute>() != null);

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
                    pObj = await ResolveAsyncNoLockImpl(pType, pMap, _emptyCtorParams);
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
                await initAsync.InitializeAsync();

            _containedObjects[typeI] = res;

            return res;
        }

        public virtual async Task ResolveToAsync<TInterface, TObject>(TObject obj)
            where TObject : class, TInterface
        {
            await RunLocked(obj, o => _containedObjects[typeof(TInterface)] = o);
        }

        public async Task<Object> ResolveAsync(Type type)
        {
            var typeO = await GetMappingType(type);

            return await ResolveAsyncImpl<Object>(type, typeO, _emptyCtorParams);

            //var res = await RunLockedAsync(type, typeO, 
            //    async (i, o) => await ResolveAsyncNoLockImpl(i, o));

            //if (res == null)
            //    throw new NullReferenceException("Unable to resolve an object of type " + type);

            //return res;
        }

        private async Task<Type> GetMappingType(Type type)
        {
            return await RunLocked(type, GetMappingNoLock);
        }

        private Type GetMappingNoLock(Type tIn)
        {
            if (_typeMappings.TryGetValue(tIn, out var good))
                return good;

            return tIn;
        }

        // ReSharper disable once UnusedMember.Local
        private async Task RunLocked<TInput>(TInput input, 
                                             Action<TInput> action)
        {
            await _lockContained.WaitAsync();

            try
            {
                action(input);
            }
            finally
            {
                _lockContained.Release();
            }
        }

        private async Task<TOutput> RunLocked<TInput, TOutput>(TInput input, 
                                                              Func<TInput, TOutput> func)
        {
            await _lockContained.WaitAsync();

            try
            {
                return func(input);
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
            await _lockContained.WaitAsync();

            try
            {
                return await action(input1);
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
            await _lockContained.WaitAsync();

            try
            {
                return await action(input1, input2);
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
