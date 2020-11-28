using System;
using System.IO;
using System.Threading.Tasks;

namespace Container.Tests.Interfaces
{
    public interface ILogFileProvider
    {
        FileInfo GetLogFile();
    }
}
