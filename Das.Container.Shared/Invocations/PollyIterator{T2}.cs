using System;
using System.Collections.Generic;

namespace Das.Container.Invocations
{
   public readonly struct PollyIterator<TIn1, TIn2, TOut> : IPollyIterator<TIn1, TIn2, TOut>
   {
      private readonly Func<TIn1, TIn2, IEnumerable<TOut>> _builder;

      public PollyIterator(Func<TIn1, TIn2, IEnumerable<TOut>> builder)
      {
         _builder = builder;
      }

      public IEnumerable<TOut> Execute(TIn1 arg1, 
                                       TIn2 arg2)
      {
         foreach (var r in _builder(arg1, arg2))
            yield return r;
      }
   }
}
