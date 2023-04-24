using System;
using System.Threading.Tasks;

namespace Das.Container;

public class LoadAheadResolver : BaseResolver
{
   public override void ResolveTo<TInterface, TObject>()
   {
      base.ResolveTo<TInterface, TObject>();

      ResolveObjectImpl(typeof(TInterface), typeof(TObject), _emptyCtorParams);
   }
}