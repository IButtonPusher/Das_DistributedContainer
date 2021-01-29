using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Container
{
    public class ConstructorWorker
    {
        public ConstructorWorker(ConstructorInfo ctor,
                                 Object?[] ctorParams)
        {
            ConstructorBuilding = ctor;
            _ctorParams = ctorParams;
            _brokenAtIndex = -1;
            _lock = new Object();
        }

        public ConstructorInfo ConstructorBuilding { get; }

        public IEnumerable<Tuple<Int32, ParameterInfo>> BuildValues()
        {
            lock (_lock)
            {
                var prms = ConstructorBuilding.GetParameters();

                var providedParamIndex = 0;

                _parameterValues = new Object?[prms.Length];


                for (var c = 0; c < prms.Length; c++)
                {
                    if (_brokenAtIndex >= 0)
                        yield break;

                    var pType = prms[c].ParameterType;
                    if (providedParamIndex < _ctorParams.Length &&
                        pType.IsInstanceOfType(_ctorParams[providedParamIndex]))
                    {
                        var pObj = _ctorParams[providedParamIndex++];

                        switch (pObj)
                        {
                            case null:
                                throw new Exception($"Cannot resolve ctor parameter of type {pType}");
                        }

                        _parameterValues[c] = pObj;
                    }

                    else
                        yield return new Tuple<Int32, ParameterInfo>(c, prms[c]);
                }
            }
        }

        public Object?[]? GetParameterValues()
        {
            lock (_lock)
            {
                if (_brokenAtIndex >= 0)
                    return default;

                var args = GetParameterValuesImpl();

                return args;
            }
        }

        public void NotifyValueNotAvailable(Int32 index)
        {
            _brokenAtIndex = index;
        }

        public void SetValue(Int32 index,
                             Object? value)
        {
            lock (_lock)
            {
                var args = GetParameterValuesImpl();
                args[index] = value;
            }
        }

        private Object?[] GetParameterValuesImpl()
        {
            if (!(_parameterValues is { } args))
                throw new InvalidOperationException("Call " + nameof(BuildValues) + " first");

            return args;
        }

        private readonly Object?[] _ctorParams;

        private readonly Object _lock;
        private Int32 _brokenAtIndex;
        private Object?[]? _parameterValues;
    }
}
