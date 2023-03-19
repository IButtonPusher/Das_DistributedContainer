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

namespace Das.Container
{
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
            var found = await _typeMapLock.RunLockedFuncAsync(
                _typeMappings, typeI, isWaitIfNotFound, _waiters,
                (objs,
                 ti,
                 wait,
                 waiters) =>
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

                    var s = new MappingCompletionSource<TValue>(cancellationToken);

                    items.Add(s);

                    return s.Task;
                }, cancellationToken);

            return await found;
        }

        // ReSharper disable once UnusedMember.Global
        public async Task<TValue> GetMappingByConcreteAsync(Type typeo,
                                                            CancellationToken cancellationToken)
        {
            var found = await _typeMapLock.RunLockedFuncAsync(
                _typeMappings, typeo, _waiters,
                (objs,
                 ti,
                 waiters) =>
                {
                    if (TryGetMappingImpl(objs, ti, out var foundMapping))
                        return TaskEx.FromResult(foundMapping);

                    foreach (var kvp in objs)
                    {
                        if (kvp.Value != null &&
                            ti.IsInstanceOfType(kvp.Value))
                            return TaskEx.FromResult(kvp.Value);
                    }

                    if (!waiters.TryGetValue(ti, out var items))
                    {
                        items = new List<IDeferredLoader<TValue>>();
                        waiters.Add(ti, items);
                    }

                    var s = new MappingCompletionSource<TValue>(cancellationToken);

                    items.Add(s);

                    return s.Task;
                }, cancellationToken);

            try
            {
                return await found;
            }
            catch (TaskCanceledException x)
            {
                throw new TypeLoadException("Unable to resolve an object of type " + typeo, x);
            }
        }

        public TValue SetMapping(Type typeI,
                                 TValue value,
                                 Boolean isThrowIfFailed)
        {
            return _typeMapLock.RunLockedFunc(_typeMappings, typeI, value,
                isThrowIfFailed, _waiters, SetMappingImpl);
        }

        public async Task<TValue> SetMappingAsync(Type typeI,
                                                  TValue value,
                                                  Boolean isThrowIfFailed,
                                                  CancellationToken cancellationToken)
        {
            var found = await _typeMapLock.RunLockedFuncAsync(SetMappingImpl,
                _typeMappings, typeI, value, isThrowIfFailed, _waiters,
                cancellationToken);

            return found;
        }


        public async Task<TValue> TryGetMappingByConcreteAsync(Type typeo,
                                                               CancellationToken cancellationToken)
        {
            var found = await _typeMapLock.RunLockedFuncAsync(
                _typeMappings, typeo, _waiters,
                (objs,
                 ti,
                 _) =>
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
                }, cancellationToken);

            try
            {
                return await found;
            }
            catch (TaskCanceledException x)
            {
                throw new TypeLoadException("Unable to resolve an object of type " + typeo, x);
            }
        }

        public async Task<IDeferredLoader?> TryGetWaiterAsync(Type type,
                                                              CancellationToken cancellationToken)
        {

           await _typeMapLock.WaitAsync();
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

            //return await _typeMapLock.RunLockedFuncAsync((w,
            //                                              t) =>
            //{
            //    if (w.TryGetValue(t, out var res) && res.Count > 0)
            //        return res[0];

            //    return default;
            //}, _waiters, type, cancellationToken);
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
}
