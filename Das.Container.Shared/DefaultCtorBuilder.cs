using System;
using System.Reflection;
using System.Threading.Tasks;
using Das.Container.Construction;

namespace Das.Container;

public class DefaultCtorBuilder : TaskCompletionSource<Object>,
                                  IObjectBuilder
{
   public DefaultCtorBuilder(ConstructorInfo ctor)
      #if NET40
      #else
      : base(TaskCreationOptions.RunContinuationsAsynchronously)
   #endif
   {
      _ctor = ctor;
   }

   public void NotifyOfLoad(Object obj,
                            Type contractType)
   {
      //intentionally left blank
   }

   public Task<Object> GetAwaiter()
   {
      return Task;
   }

   public void BeginBuilding()
   {
      var res = _ctor.Invoke(_emptyObjs);
      SetResult(res);
   }

   private static readonly Object[] _emptyObjs = 
      #if NET40
         new Object[0];
      #else
      Array.Empty<Object>();
   #endif
   private readonly ConstructorInfo _ctor;
}