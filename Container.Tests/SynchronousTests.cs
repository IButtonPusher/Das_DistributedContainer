using System;
using System.Threading.Tasks;
using Container.Tests.Implementations;
using Container.Tests.Interfaces;
using Das.Container;
using Xunit;

namespace Container.Tests
{
    public class SynchronousTests
    {
        [Fact]
        public void SimpleLinearDependencies()
        {
            var container = new BaseResolver();
            container.ResolveTo<ILog, NullLog>();
            container.ResolveTo<ILogFileProvider, TestLogFileProvider>();
            container.ResolveTo<ILogSizeProvider, TestLogSizeProvider>();

            var testLogFileProvider = container.Resolve<ILogFileProvider>();
            var fi = testLogFileProvider.GetLogFile();
            Assert.NotNull(fi);

            var testLogSizeProvider = container.Resolve(typeof(ILogSizeProvider)) as ILogSizeProvider;
            Assert.NotNull(testLogSizeProvider);

            var size = testLogSizeProvider!.GetLogSize();
            Assert.True(size >= 0);
        }
    }
}
