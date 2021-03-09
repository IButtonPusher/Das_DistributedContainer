using System;
using System.Threading.Tasks;
// ReSharper disable All

namespace Container.Tests.Implementations
{
    public class CannotCtor : ICannotAutoCtor
    {
        private readonly String _name;
        private readonly Int32 _id;

        public CannotCtor(String name,
                          Int32 id)
        {
            _name = name;
            _id = id;
        }
    }
}
