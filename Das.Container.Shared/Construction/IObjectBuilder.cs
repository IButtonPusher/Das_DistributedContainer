using System;
using System.Threading.Tasks;

namespace Das.Container.Construction;

public interface IObjectBuilder : IDeferredLoader
{
   void BeginBuilding();
}