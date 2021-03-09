using System;
using System.Threading.Tasks;

// ReSharper disable All

namespace Container.Tests.Implementations
{
    public class CannotCtor : ICannotAutoCtor
    {
        public CannotCtor(String name,
                          Int32 id)
        {
            _name = name;
            _id = id;
        }

        private readonly Int32 _id;
        private readonly String _name;
    }
}
