﻿using System;
using System.Threading.Tasks;

namespace Container.Tests.Interfaces
{
    public interface IMegaLog : ILog
    {
        void LogEx(String text);
    }
}
