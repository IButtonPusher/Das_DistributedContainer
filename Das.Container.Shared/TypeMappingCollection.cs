using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#if NET40
using SemaphoreSlim = System.Threading.AsyncSemaphore;
#else
using TaskEx = System.Threading.Tasks.Task;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
#endif

namespace Das.Container;

public class TypeMappingCollection<TValue>
{
   public TypeMappingCollection()
   {
      _typeMapLock = new SemaphoreSlim(1);
      _typeMappings = new Dictionary<Type, TValue>();
      _waiters = new Dictionary<Type, List<IDeferredLoader<TValue>>>();
   }

   public TValue GetMapping(Type typeI)
   {
      var found = _typeMapLock.RunLockedFunc(_typeMappings, typeI, (objs,
                                                                    ti) =>
      {
         if (TryGetMappingImpl(objs, ti, out var foundMapping))
            return foundMapping;

         return default;
      });

      return found!;
   }

   public async Task<TValue> GetMappingAsync(Type typeI,
                                             CancellationToken cancellationToken,
                                             Boolean isWaitIfNotFound)
   {
      if (isWaitIfNotFound)
      {
         var found = await _typeMapLock.RunLockedFuncAsync(
                                          _typeMappings, typeI, isWaitIfNotFound, _waiters,
                                          cancellationToken, GetMappingImpl, cancellationToken)
                                       .ConfigureAwait(false);

         return await found.ConfigureAwait(false);
      }

      else
      {
         var found = await _typeMapLock.RunLockedTaskAsync(
                                          _typeMappings, typeI, isWaitIfNotFound, _waiters,
                                          cancellationToken, GetMappingImpl, cancellationToken)
                                       .ConfigureAwait(false);

         return found;
      }
   }

   private static Task<TValue> GetMappingImpl(Dictionary<Type, TValue> objs,
                                       Type ti,
                                       Boolean wait,
                                       Dictionary<Type, List<IDeferredLoader<TValue>>> waiters,
                                       CancellationToken ct)
   {
      if (TryGetMappingImpl(objs, ti, out var foundMapping))
         return TaskEx.FromResult(foundMapping);

      if (!wait)
         return TaskEx.FromResult<TValue>(default!);

      if (!waiters.TryGetValue(ti, out var items))
      {
         items = new List<IDeferredLoader<TValue>>();
         waiters.Add(ti, items);
      }

      var s = new MappingCompletionSource<TValue>(ct);

      items.Add(s);

      return s.Task;
   }

   //// ReSharper disable once UnusedMember.Global
   //public async Task<TValue> GetMappingByConcreteAsync(Type typeo,
   //                                                    CancellationToken cancellationToken)
   //{
   //   try
   //   {
   //      var found = await _typeMapLock.RunLockedFuncAsync(
   //         _typeMappings, typeo, _waiters, cancellationToken,
   //         (objs,
   //          ti,
   //          waiters,
   //          ct) =>
   //         {
   //            if (TryGetMappingImpl(objs, ti, out var foundMapping))
   //               return TaskEx.FromResult(foundMapping);

   //            foreach (var kvp in objs)
   //            {
   //               if (kvp.Value != null &&
   //                   ti.IsInstanceOfType(kvp.Value))
   //                  return TaskEx.FromResult(kvp.Value);
   //            }

   //            if (!waiters.TryGetValue(ti, out var items))
   //            {
   //               items = new List<IDeferredLoader<TValue>>();
   //               waiters.Add(ti, items);
   //            }

   //            var s = new MappingCompletionSource<TValue>(ct);

   //            items.Add(s);

   //            return s.Task;
   //         }, cancellationToken).ConfigureAwait(false);


   //      return found;
   //   }
   //   catch (TaskCanceledException x)
   //   {
   //      throw new TypeLoadException("Unable to resolve an object of type " + typeo, x);
   //   }
   //}

   public TValue SetMapping(Type typeI,
                            TValue value,
                            Boolean isThrowIfFailed) =>
      _typeMapLock.RunLockedFunc(_typeMappings, typeI, value,
         isThrowIfFailed, _waiters, SetMappingImpl);

   public async Task<TValue> SetMappingAsync(Type typeI,
                                             TValue value,
                                             Boolean isThrowIfFailed,
                                             CancellationToken cancellationToken)
   {
      var found = await _typeMapLock.RunLockedFuncAsync(SetMappingImpl,
                                       _typeMappings, typeI, value, isThrowIfFailed, _waiters,
                                       cancellationToken)
                                    .ConfigureAwait(false);

      return found;
   }


   public async Task<TValue> TryGetMappingByConcreteAsync(Type typeo,
                                                          CancellationToken cancellationToken)
   {
      try
      {
         var found = await _typeMapLock.RunLockedTaskAsync(
            _typeMappings, typeo, TryGetMappingByConcreteImpl, cancellationToken);

         return found;
      }
      catch (TaskCanceledException x)
      {
         throw new TypeLoadException("Unable to resolve an object of type " + typeo, x);
      }
   }

   private static Task<TValue> TryGetMappingByConcreteImpl(Dictionary<Type, TValue> objs,
                                                           Type ti)
   {
      if (TryGetMappingImpl(objs, ti, out var foundMapping))
         return TaskEx.FromResult(foundMapping);

      foreach (var kvp in objs)
      {
         if (kvp.Value != null &&
             ti.IsInstanceOfType(kvp.Value))
            return TaskEx.FromResult(kvp.Value);
      }

      return TaskEx.FromResult<TValue>(default!);
   }

   public async Task<IDeferredLoader?> TryGetWaiterAsync(Type type)
   {
      await _typeMapLock.WaitAsync().ConfigureAwait(false);
      try
      {
         if (_waiters.TryGetValue(type, out var res) && res.Count > 0)
            return res[0];

         return default;
      }
      finally
      {
         _typeMapLock.Release();
      }
   }

   private static TValue SetMappingImpl(Dictionary<Type, TValue> objs,
                                        Type ti,
                                        TValue value,
                                        Boolean isThrowIfFailed,
                                        Dictionary<Type, List<IDeferredLoader<TValue>>> waiters)
   {
      if (objs.TryGetValue(ti, out var foundMapping))
      {
         if (isThrowIfFailed)
            throw new InvalidOperationException($"{ti} already maps to {foundMapping}");
         return foundMapping;
      }

      objs.Add(ti, value);

      if (waiters.TryGetValue(ti, out var waiting))
      {
         waiters.Remove(ti);
         foreach (var hardestPart in waiting)
         {
            hardestPart.TrySetResult(value);
         }
      }

      return value;
   }


   private static Boolean TryGetMappingImpl(Dictionary<Type, TValue> objs,
                                            Type ti,
                                            out TValue foundMapping)
   {
      if (objs.TryGetValue(ti, out foundMapping))
         return true;
      foundMapping = default!;

      foreach (var kvp in objs)
      {
         if (ti.IsAssignableFrom(kvp.Key))
         {
            foundMapping = kvp.Value;
            break;
         }
      }

      if (foundMapping != null)
         objs.Add(ti, foundMapping);

      return foundMapping != null;
   }

   private readonly SemaphoreSlim _typeMapLock;


   private readonly Dictionary<Type, TValue> _typeMappings;
   private readonly Dictionary<Type, List<IDeferredLoader<TValue>>> _waiters;
}
