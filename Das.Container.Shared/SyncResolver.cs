#if NET40
using SemaphoreSlim = System.Threading.AsyncSemaphore;
#else
using TaskEx = System.Threading.Tasks.Task;
#endif
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Container
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public partial class BaseResolver : IResolver
    {
        public T Resolve<T>()
        {
            return Resolve<T>(_emptyCtorParams);
        }

        public T Resolve<T>(params Object[] ctorArgs)
        {
            var typeI = typeof(T);
            var typeO = EnsureNotNull(_typeMappings.GetMapping(typeI), typeI);

            return ResolveImpl<T>(typeI, typeO, ctorArgs);
        }


        public virtual void ResolveTo<TInterface>(TInterface instance)
        {
            Object oInstance = instance!;
            var instType = oInstance.GetType();

            _typeMappings.SetMapping(typeof(TInterface), instType, true);
            __instanceMappings.SetMapping(typeof(TInterface), oInstance, true);
        }

        public virtual void ResolveTo<TInterface, TObject>()
            where TObject : class, TInterface
        {
            var instType = typeof(TObject);
            _typeMappings.SetMapping(typeof(TInterface), instType, true);
        }

        public Object Resolve(Type type)
        {
            var typeO = EnsureNotNull(_typeMappings.GetMapping(type), type);
            return ResolveImpl<Object>(type, typeO, _emptyCtorParams);
        }

        public Boolean TryResolve<TInstance>(Type type,
                                             out TInstance resolved)
        {
            var typeO = _typeMappings.GetMapping(type);
            if (typeO == null)
            {
                resolved = default!;
                return false;
            }

            resolved = ResolveImpl<TInstance>(type, typeO, _emptyCtorParams);
            return resolved != null;
        }

        private static Type EnsureNotNull(Type? typeo,
                                          Type typeI)
        {
            if (typeo != null)
                return typeo;

            throw new Exception("Unable to resolve an object of type " + typeI);
        }


        private Object?[] GetConstructorArgs(ConstructorInfo ctor,
                                             Object[] ctorParams)
        {
            if (ctor.GetParameters().Length == 0)
                return _emptyCtorParams;

            var ctorWorker = new ConstructorWorker(ctor, ctorParams);

            foreach (var missing in ctorWorker.BuildValues())
            {
                var pType = missing.Item2.ParameterType;
                var pMap = EnsureNotNull(_typeMappings.GetMapping(pType), pType);
                var pObj = ResolveObjectImpl(pType, pMap, _emptyCtorParams);
                ctorWorker.SetValue(missing.Item1, pObj);
            }

            var args = ctorWorker.GetParameterValues();
            return args;
        }


        private TInterface ResolveImpl<TInterface>(Type typeI,
                                                   Type typeO,
                                                   Object[] ctorParams)
        {
            var res = ResolveObjectImpl(typeI, typeO, ctorParams);

            if (res is TInterface good)
                return good;

            throw new NullReferenceException("Unable to resolve an object of type " + typeI);
        }


        private Object? ResolveObjectImpl(Type typeI,
                                          Type typeO,
                                          Object[] ctorParams)
        {
            if (TryGetContained(typeI, typeO, out var found))
                return found;

            var ctor = GetConstructor(typeO);
            var args = GetConstructorArgs(ctor, ctorParams);

            var res = ctor.Invoke(args);

            if (res is IInitializeAsync initAsync)
            {
                var awaitable = TaskEx.Run(async () => { await initAsync.InitializeAsync().ConfigureAwait(false); });
                awaitable.ConfigureAwait(false);
                awaitable.Wait();
            }

            res = __instanceMappings.SetMapping(typeI, res, false);

            return res;
        }


      
    }
}