using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace BlockingObjectPool
{
    public class GenericObjectPoolTests
    {
        private static Random random = new Random();
        [Fact]
        public void ShouldShowCorrectNumberOfActiveAndIdleElements() 
        {
            Mock<IPooledObjectFactory<NonShareable>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<NonShareable>>();
            mockPooledObjectFactory.Setup(x => x.Create()).Returns(() => new NonShareable());

            var pool = new PoolBuilder<NonShareable>()
                    .InitialSize(0)
                    .MaxSize(10)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .Instance();

            Assert.Equal(0, pool.ActiveCount);
            Assert.Equal(0, pool.IdleCount);
            var acquired = pool.Acquire();
            Assert.Equal(1, pool.ActiveCount);
            Assert.Equal(0, pool.IdleCount);
            var acquired2 = pool.Acquire();
            Assert.Equal(2, pool.ActiveCount);
            Assert.Equal(0, pool.IdleCount);
            pool.Return(acquired);
            Assert.Equal(1, pool.ActiveCount);
            Assert.Equal(1, pool.IdleCount);
            pool.Return(acquired2);
            Assert.Equal(0, pool.ActiveCount);
            Assert.Equal(2, pool.IdleCount);

            pool.Dispose();
        }

        [Fact]
        public void ShouldNotReturnObjectsInUse() 
        {
            Mock<IPooledObjectFactory<NonShareable>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<NonShareable>>();
            mockPooledObjectFactory.Setup(x => x.Create()).Returns(() => new NonShareable());

            var pool = new PoolBuilder<NonShareable>()
                    .InitialSize(0)
                    .MaxSize(10)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .Instance();

            HashSet<object> acquiredObjects = new HashSet<object>();
            for(int i=0; i<10; i++) {
                acquiredObjects.Add(pool.Acquire());
            }
            // All the acquired items are different.
            Assert.Equal(10, acquiredObjects.Count());

            pool.Dispose();
        }

        [Fact]
        public void ShouldReturnIdleObjects() 
        {
            Mock<IPooledObjectFactory<NonShareable>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<NonShareable>>();
            mockPooledObjectFactory.Setup(x => x.Create()).Returns(() => new NonShareable());

            var pool = new PoolBuilder<NonShareable>()
                    .InitialSize(0)
                    .MaxSize(10)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .Instance();

            var acquired = pool.Acquire();
            pool.Return(acquired);
            var acquired2 = pool.Acquire();
            
            // All the acquired items are different.
            Assert.Equal(acquired, acquired2);

            pool.Dispose();
        }

        [Fact]
        public void ShouldBlockClientsUntilObjectsAreAvailable() 
        {
            Mock<IPooledObjectFactory<NonShareable>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<NonShareable>>();
            mockPooledObjectFactory.Setup(x => x.Create()).Returns(() => new NonShareable());

            var pool = new PoolBuilder<NonShareable>()
                    .InitialSize(0)
                    .MaxSize(1)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .Instance();

            AutoResetEvent autoResetEvent = new AutoResetEvent(false); 
            var acquired = pool.Acquire();
            Task.Run(() => {
                pool.Acquire();
                autoResetEvent.Set();
            });

            Assert.False(autoResetEvent.WaitOne(100));
            pool.Return(acquired);
            Assert.True(autoResetEvent.WaitOne(100));

            pool.Dispose();
        }

        [Fact]
        public void ShouldStopWaitingAndReturnFalseWhenTryAcquireTimesOut() 
        {
            Mock<IPooledObjectFactory<NonShareable>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<NonShareable>>();
            mockPooledObjectFactory.Setup(x => x.Create()).Returns(() => new NonShareable());

            var pool = new PoolBuilder<NonShareable>()
                    .InitialSize(0)
                    .MaxSize(1)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .Instance();

            var acquired = pool.Acquire();
            NonShareable acquiredObjectInTry;
            Assert.Equal(false, pool.TryAcquire(100, out acquiredObjectInTry));

            pool.Dispose();
        }

        [Fact]
        public void ShouldReturnTrueAndAcquiredObjectOnTryAcquireWithAvailableObjects() 
        {
            Mock<IPooledObjectFactory<NonShareable>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<NonShareable>>();
            mockPooledObjectFactory.Setup(x => x.Create()).Returns(() => new NonShareable());

            var pool = new PoolBuilder<NonShareable>()
                    .InitialSize(0)
                    .MaxSize(1)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .Instance();

            NonShareable acquired = pool.Acquire();
            pool.Return(acquired);
            NonShareable acquiredObjectInTry;
            Assert.Equal(true, pool.TryAcquire(100, out acquiredObjectInTry));
            Assert.Equal(acquiredObjectInTry, acquired);

            pool.Dispose();
        }

        [Fact]
        public void ShouldKeepThreadsafetyWithSeveralConcurrentUsers() 
        {
            Mock<IPooledObjectFactory<NonShareable>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<NonShareable>>();
            mockPooledObjectFactory.Setup(x => x.Create()).Returns(() => new NonShareable());

            var pool = new PoolBuilder<NonShareable>()
                    .InitialSize(0)
                    .MaxSize(10)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .Instance();

            List<Task> tasks = new List<Task>();
            for(int i=0; i<20; i++) {
                tasks.Add(Task.Run(() => {
                    for (int j=0; j<10; j++) {
                        var acquired = pool.Acquire();
                        acquired.DoStuff();
                        pool.Return(acquired);
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());
            pool.Dispose();
        }

        [Fact]
        public void ShouldValidateObjectsWhenAcquired() 
        {
            Mock<IPooledObjectFactory<NonShareable>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<NonShareable>>();
            mockPooledObjectFactory.Setup(x => x.Create()).Returns(() => new NonShareable());
            Mock<IPooledObjectValidator<NonShareable>> mockPooledObjectValidator = new Mock<IPooledObjectValidator<NonShareable>>();
            mockPooledObjectValidator.Setup(x => x.Validate(It.IsAny<NonShareable>())).Returns(true);
            mockPooledObjectValidator.Setup(x => x.ValidateOnAcquire).Returns(true);
            mockPooledObjectValidator.Setup(x => x.ValidateOnReturn).Returns(false);

            var pool = new PoolBuilder<NonShareable>()
                    .InitialSize(0)
                    .MaxSize(10)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .WithValidator(mockPooledObjectValidator.Object)
                    .Instance();

            mockPooledObjectValidator.Verify(x => x.Validate(It.IsAny<NonShareable>()), Times.Never);
            var acquired = pool.Acquire();
            mockPooledObjectValidator.Verify(x => x.Validate(It.IsAny<NonShareable>()), Times.Once);
            pool.Return(acquired);
            mockPooledObjectValidator.Verify(x => x.Validate(It.IsAny<NonShareable>()), Times.Once);
            
            pool.Dispose();
        }

        [Fact]
        public void ShouldValidateObjectsWhenReturned() 
        {
            Mock<IPooledObjectFactory<NonShareable>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<NonShareable>>();
            mockPooledObjectFactory.Setup(x => x.Create()).Returns(() => new NonShareable());
            Mock<IPooledObjectValidator<NonShareable>> mockPooledObjectValidator = new Mock<IPooledObjectValidator<NonShareable>>();
            mockPooledObjectValidator.Setup(x => x.Validate(It.IsAny<NonShareable>())).Returns(true);
            mockPooledObjectValidator.Setup(x => x.ValidateOnAcquire).Returns(false);
            mockPooledObjectValidator.Setup(x => x.ValidateOnReturn).Returns(true);

            var pool = new PoolBuilder<NonShareable>()
                    .InitialSize(0)
                    .MaxSize(10)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .WithValidator(mockPooledObjectValidator.Object)
                    .Instance();

            mockPooledObjectValidator.Verify(x => x.Validate(It.IsAny<NonShareable>()), Times.Never);
            var acquired = pool.Acquire();
            mockPooledObjectValidator.Verify(x => x.Validate(It.IsAny<NonShareable>()), Times.Never);
            pool.Return(acquired);
            mockPooledObjectValidator.Verify(x => x.Validate(It.IsAny<NonShareable>()), Times.Once);
            
            pool.Dispose();
        }

        [Fact]
        public void ShouldNotReturnObjectsThatFailToValidate() 
        {
            Mock<IPooledObjectFactory<string>> mockPooledObjectFactory = new Mock<IPooledObjectFactory<string>>();
            mockPooledObjectFactory.SetupSequence(x => x.Create())
                .Returns("Object1")
                .Returns("Object2");
            Mock<IPooledObjectValidator<string>> mockPooledObjectValidator = new Mock<IPooledObjectValidator<string>>();
            mockPooledObjectValidator.Setup(x => x.ValidateOnAcquire).Returns(true);
            mockPooledObjectValidator.Setup(x => x.ValidateOnReturn).Returns(false);
            mockPooledObjectValidator.SetupSequence(x => x.Validate(It.IsAny<string>()))
                .Returns(false)
                .Returns(true);

            var pool = new PoolBuilder<string>()
                    .InitialSize(0)
                    .MaxSize(10)
                    .WithFactory(mockPooledObjectFactory.Object)
                    .WithValidator(mockPooledObjectValidator.Object)
                    .Instance();

            var acquired = pool.Acquire();
            Assert.Equal("Object2", acquired);
            
            pool.Dispose();
        }
    }

    public class NonShareable {
        private int concurrentUsers = 0;

        public void DoStuff() {
            Interlocked.Increment(ref concurrentUsers);
            Assert.Equal(1, concurrentUsers);
            Thread.Sleep(10);
            Interlocked.Decrement(ref concurrentUsers);
        }
    }
}