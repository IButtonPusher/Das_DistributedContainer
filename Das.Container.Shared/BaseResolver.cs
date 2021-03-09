using System;
using System.Collections.Concurrent;
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
        public BaseResolver(TimeSpan defaultAsyncTimeout)
            : this((TimeSpan?) defaultAsyncTimeout)
        {
        }

        public BaseResolver() : this(null)
        {
        }

        private BaseResolver(TimeSpan? defaultTimeout)
        {
            _defaultAsyncTimeout = defaultTimeout;

            //_contractBuilders = new ConcurrentDictionary<Type, Task<Object>>();
            _contractBuilders2 = new ConcurrentDictionary<Type, IDeferredLoader>();

            //_ctorArgsTasks = new ConcurrentDictionary<ConstructorInfo, TaskCompletionSource<Object?[]?>>();
            _typeMappings = new TypeMappingCollection<Type>();
            _instanceMappings = new TypeMappingCollection<Object>();


            _emptyCtorParams = new Object[0];
        }

        private static Boolean TryGetConstructor(Type typeO,
                                                 out ConstructorInfo ctor)
        {
            var ctors = typeO.GetConstructors();
            if (ctors.Length == 1)
            {
                ctor = ctors[0];
                return true;
            }

            ctor = default!;

            for (var c = 0; c < ctors.Length; c++)
            {
                var current = ctors[c];
                var attribs = current.GetCustomAttributes(
                    typeof(ContainerConstructorAttribute), true);
                if (attribs.Length == 0)
                    continue;

                if (ctor == null)
                    ctor = current;
                else
                    return false; //we have more than one with the attribute.  Fail
            }

            return ctor != null;
        }

        private static ConstructorInfo GetConstructor(Type typeO)
        {
            if (!TryGetConstructor(typeO, out var ctor))
                throw new InvalidOperationException(
                    $"{typeO} must have exactly one accessible constructor or one of the constructors must be decorated with ContainerConstructorAttribute");

            return ctor;

            //var ctors = typeO.GetConstructors();

            //ConstructorInfo? ctor;

            //if (ctors.Length != 1)
            //{
            //    var dCtors = from c in ctors
            //        let attrs = c.GetCustomAttributes(
            //            typeof(ContainerConstructorAttribute),
            //            true).FirstOrDefault()
            //        where c != null
            //        select c;

            //    ctor = dCtors.FirstOrDefault();

            //    // !4.0
            //    //ctor = ctors.FirstOrDefault(c => c.GetCustomAttribute<ContainerConstructorAttribute>() != null);

            //    if (ctor == null)
            //        throw new InvalidOperationException(
            //            $"{typeO} must have exactly one accessible constructor or one of the constructors must be decorated with ContainerConstructorAttribute");
            //}
            //else
            //    ctor = ctors[0];

            //return ctor;
        }

        //private async Task<Object?> GetContainedAsync(Type contractType,
        //                                              CancellationToken cancellationToken,
        //                                              Boolean isWaitIfNotFound)
        //{
        //    var typeO = await _typeMappings.GetMappingAsync(contractType, cancellationToken, 
        //        isWaitIfNotFound);

        //    if (typeO == null)
        //        return default;

        //    return await GetContainedAsync(contractType, typeO, cancellationToken, isWaitIfNotFound);
        //}

        private async Task<Object?> GetContainedAsync(Type typeI,
                                                      Type typeO,
                                                      CancellationToken cancellationToken,
                                                      Boolean isWaitIfNotFound)
        {
            var found = await _instanceMappings.GetMappingAsync(typeI, cancellationToken, isWaitIfNotFound);
            // ReSharper disable once ConstantNullCoalescingCondition
            found ??= await _instanceMappings.TryGetMappingByConcreteAsync(typeO, cancellationToken);

            found = Util.ValidateTypes(found, typeI, typeO)!;
            return found;
        }

        private CancellationToken GetDefaultCancellationToken()
        {
            if (_defaultAsyncTimeout == null)
                return CancellationToken.None;

            #if NET40
            var source = new CancellationTokenSource();
            var timer = new Timer(self => {
                ((Timer)self).Dispose();
                try {
                    source.Cancel();
                } catch (ObjectDisposedException) {}
            });
            timer.Change((Int32)_defaultAsyncTimeout.Value.TotalMilliseconds, -1);
            return source.Token;
            
            //return CancellationToken.None;
            #else

            return _defaultAsyncTimeout == null
                ? CancellationToken.None
                : new CancellationTokenSource(_defaultAsyncTimeout.Value).Token;

            #endif
        }

        private Boolean TryGetContained(Type typeI,
                                        Type typeO,
                                        out Object found)
        {
            found = _instanceMappings.GetMapping(typeI);
            found = Util.ValidateTypes(found, typeI, typeO)!;

            return found != null;
        }

        //private static Object? ValidateTypes(Object? found,
        //                                     Type ti,
        //                                     Type to)
        //{
        //    if (found != null)
        //        return found;

        //    if (to.IsAbstract || to.IsInterface)
        //        throw new InvalidOperationException("Unable to map " +
        //                                            ti.Name + " to " +
        //                                            to.Name + " - it is not an instantiable type");

        //    return default;
        //}

        //private readonly ConcurrentDictionary<ConstructorInfo, TaskCompletionSource<Object?[]?>> _ctorArgsTasks;
        private readonly TimeSpan? _defaultAsyncTimeout;
        protected readonly Object[] _emptyCtorParams;

        protected readonly TypeMappingCollection<Object> _instanceMappings;
        private readonly TypeMappingCollection<Type> _typeMappings;
    }
}
