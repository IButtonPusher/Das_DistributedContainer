using System;

namespace Das.Container
{
    public static class Util
    {
        public static Object? ValidateTypes(Object? found,
                                             Type ti,
                                             Type to)
        {
            if (found != null)
                return found;

            if (to.IsAbstract || to.IsInterface)
                throw new InvalidOperationException("Unable to map " +
                                                    ti.Name + " to " +
                                                    to.Name + " - it is not an instantiable type");

            return default;
        }
    }
}
