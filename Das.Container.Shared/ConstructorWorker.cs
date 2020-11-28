using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Container
{
    public class ConstructorWorker //: IEnumerable<ValueTuple<Int32, ParameterInfo>>
    {
        public ConstructorWorker(ConstructorInfo ctor,
                                 Object?[] ctorParams)
        {
            _ctor = ctor;
            _ctorParams = ctorParams;
        }

        public IEnumerable<Tuple<Int32, ParameterInfo>> BuildValues()
        {
            var prms = _ctor.GetParameters();

            var providedParamIndex = 0;

            _parameterValues = new Object?[prms.Length];

            for (var c = 0; c < prms.Length; c++)
            {
                Object? pObj;

                var pType = prms[c].ParameterType;
                if (providedParamIndex < _ctorParams.Length &&
                    pType.IsInstanceOfType(_ctorParams[providedParamIndex]))
                {
                    pObj = _ctorParams[providedParamIndex++];

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

        public Object?[] GetParameterValues()
        {
            var args = GetParameterValuesImpl();

            return args;
        }

        public void SetValue(Int32 index,
                             Object? value)
        {
            var args = GetParameterValuesImpl();

            args[index] = value;
        }

        private Object?[] GetParameterValuesImpl()
        {
            if (!(_parameterValues is { } args))
                throw new InvalidOperationException("Call " + nameof(BuildValues) + " first");

            return args;
        }

        private readonly ConstructorInfo _ctor;
        private readonly Object?[] _ctorParams;
        private Object?[]? _parameterValues;
    }
}