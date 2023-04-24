using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Das.Container.Construction;
#if NET40
using SemaphoreSlim = System.Threading.AsyncSemaphore;
#else
using TaskEx = System.Threading.Tasks.Task;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
#endif

namespace Das.Container;

// ReSharper disable once UnusedType.Global
public partial class BaseResolver
{
   public BaseResolver(TimeSpan defaultAsyncTimeout)
      : this((TimeSpan?)defaultAsyncTimeout)
   {
   }

   public BaseResolver() : this(null)
   {
   }

   private BaseResolver(TimeSpan? defaultTimeout)
   {
      _defaultAsyncTimeout = defaultTimeout;
      _contractBuilders = new ConcurrentDictionary<Type, IObjectBuilder>();
      _typeMappings = new TypeMappingCollection<Type>();
      _instanceMappings = new TypeMappingCollection<Object>();


      _emptyCtorParams = new Object[0];
   }

   private static ConstructorInfo GetConstructor(Type typeO)
   {
      if (!TryGetConstructor(typeO, out var ctor))
         throw new InvalidOperationException(
            $"{typeO} must have exactly one accessible constructor or one of the constructors must be decorated with ContainerConstructorAttribute");

      return ctor;
   }

   private async Task<Object?> GetContainedAsync(Type typeI,
                                                 Type typeO,
                                                 CancellationToken cancellationToken,
                                                 Boolean isWaitIfNotFound)
   {
      var found = await _instanceMappings.GetMappingAsync(typeI, cancellationToken, isWaitIfNotFound)
                                         .ConfigureAwait(false);

      // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
      found ??= await _instanceMappings.TryGetMappingByConcreteAsync(typeO, cancellationToken)
                                       .ConfigureAwait(false);

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

         if (ctor == null!)
            ctor = current;
         else
            return false; //we have more than one with the attribute.  Fail
      }

      return ctor != null!;
   }

   private Boolean TryGetContained(Type typeI,
                                   Type typeO,
                                   out Object found)
   {
      found = _instanceMappings.GetMapping(typeI);
      found = Util.ValidateTypes(found, typeI, typeO)!;

      return found != null!;
   }

   private readonly ConcurrentDictionary<Type, IObjectBuilder> _contractBuilders;

   private readonly TimeSpan? _defaultAsyncTimeout;
   protected readonly Object[] _emptyCtorParams;

   protected readonly TypeMappingCollection<Object> _instanceMappings;
   private readonly TypeMappingCollection<Type> _typeMappings;
}
