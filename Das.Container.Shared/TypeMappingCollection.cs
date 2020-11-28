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
            _waiters = new Dictionary<Type, List<MappingCompletionSource<TValue>>>();
        }

        public TValue GetMapping(Type typeI)
        {
            var found = _typeMapLock.RunLockedFunc(_typeMappings, typeI, (objs, ti) =>
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
                (objs, ti, wait, waiters) =>
                {
                    if (TryGetMappingImpl(objs, ti, out var foundMapping))
                        return TaskEx.FromResult(foundMapping);

                    //if (objs.TryGetValue(ti, out var foundMapping))
                    //    return TaskEx.FromResult(foundMapping);

                    if (!wait)
                        return TaskEx.FromResult<TValue>(default!);

                    if (!waiters.TryGetValue(ti, out var items))
                    {
                        items = new List<MappingCompletionSource<TValue>>();
                        waiters.Add(ti, items);
                    }

                    var s = new MappingCompletionSource<TValue>(cancellationToken);

                    items.Add(s);

                    return s.Task;
                }, cancellationToken);

            return await found;
            //if (!isWaitIfNotFound || found != null)
            //    return found;

            //// not available and we want to wait

            //var src = new MappingCompletionSource<TValue>(cancellationToken);

            //await _typeMapLock.RunLockedActionAsync(_waiters, src, typeI, (w, s, t) =>
            //{
            //    if (!w.TryGetValue(t, out var items))
            //    {
            //        items = new List<MappingCompletionSource<TValue>>();
            //        w.Add(t, items);
            //    }

            //    items.Add(s);
            //}, cancellationToken);

            //return await src.Task;
        }

        private static Boolean TryGetMappingImpl(Dictionary<Type, TValue> objs,
                                             Type ti,
                                             out TValue foundMapping)
        {
            if (objs.TryGetValue(ti, out foundMapping))
                return true;
            else
                foundMapping = default!;

            foreach (var kvp in objs)
            {
                //if (kvp.Key.IsAssignableFrom(ti))
                if (ti.IsAssignableFrom(kvp.Key))
                {
                    foundMapping = kvp.Value;
                    break;
                }
            }

            if (foundMapping != null)
                objs.Add(ti, foundMapping);

            return foundMapping != null;
            //foundMapping = default!;
            //return false;
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
            var found = await _typeMapLock.RunLockedFuncAsync(_typeMappings,
                typeI, value, isThrowIfFailed, _waiters,
                SetMappingImpl, cancellationToken);

            return found;
        }

        private static TValue SetMappingImpl(Dictionary<Type, TValue> objs,
                                             Type ti,
                                             TValue value,
                                             Boolean isThrowIfFailed,
                                             Dictionary<Type, List<MappingCompletionSource<TValue>>> waiters)
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
                foreach (var hardestPart in waiting) hardestPart.TrySetResult(value);
            }

            return value;
        }

        private readonly SemaphoreSlim _typeMapLock;


        private readonly Dictionary<Type, TValue> _typeMappings;
        private readonly Dictionary<Type, List<MappingCompletionSource<TValue>>> _waiters;
    }
}