using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using BlockingObjectPool;

namespace BlockingObjectPool.Tests
{
    public class PoolBuilderTests
    {
        [Fact]
        public void ShouldThrowErrorOnInitialSizeBelowZero() 
        {
            Assert.Throws<ArgumentException>(() => new PoolBuilder<object>().InitialSize(-1));
        }

        [Fact]
        public void ShouldThrowErrorOnMaximumSizeBelowMinusOne() 
        {
            Assert.Throws<ArgumentException>(() => new PoolBuilder<object>().MaxSize(-2));
        }

        [Fact]
        public void ShouldThrowErrorOnInitialSizeBiggerThanMaximumSize() 
        {
            Assert.Throws<ArgumentException>(() => new PoolBuilder<object>().InitialSize(10).MaxSize(5));
        }        

        [Fact]
        public void ShouldBuildPoolWithGivenInitialSizeCapacityAndAcquiredInvalidLimit() 
        {
            Mock<IPooledObjectFactory<object>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<object>>();
            var pool = new PoolBuilder<object>()
                    .InitialSize(0)
                    .MaxSize(10)
                    .AcquiredInvalidLimit(5)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .Instance();
            Assert.Equal(0, pool.InitialSize);
            Assert.Equal(10, pool.Capacity);
            Assert.Equal(5, pool.AcquiredInvalidLimit);
        }

        [Fact]
        public void ShouldBuildPoolsThatUseTheProvidedFactory() 
        {
            Mock<IPooledObjectFactory<object>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<object>>();
            var pool = new PoolBuilder<object>()
                    .InitialSize(0)
                    .MaxSize(10)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .Instance();

            mockPooledObjectFactory.Setup(x => x.Create()).Returns("TestObject");

            pool.Acquire();
            mockPooledObjectFactory.Verify(x => x.Create(), Times.Once());

            pool.Dispose();
            mockPooledObjectFactory.Verify(x => x.Destroy(It.IsAny<object>()), Times.Once());
        }
    }
}
