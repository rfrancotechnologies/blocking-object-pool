# blocking-object-pool

.NET blocking, light weight, thread safe object pool.

[![Build History](https://buildstats.info/travisci/chart/mediatechsolutions/blocking-object-pool?branch=master)](https://travis-ci.org/mediatechsolutions/blocking-object-pool)
[![NuGet Version](https://buildstats.info/nuget/BlockingObjectPool?includePreReleases=true)](https://www.nuget.org/packages/BlockingObjectPool)
[![Build Status](https://travis-ci.org/mediatechsolutions/blocking-object-pool.svg?branch=master)](https://travis-ci.org/mediatechsolutions/blocking-object-pool)

BlockingObjectPool is a work derived from https://github.com/yanggujun/commonsfornet, more specifically https://github.com/yanggujun/commonsfornet/wiki/Commons.Pool, also released under Apache 2.0 license. The main reasons to fork the project are:

* [Commons.Pool](https://github.com/yanggujun/commonsfornet/wiki/Commons.Pool) has not been maintained sin February 2017.
* It is not correctly packaged and requires dependencies that have to be manually managed.

## Why BlockingObjectPool

It is common to use objects that are expensive to create and must be instanced at a very high rate. In this situation the Object Pool Design Pattern provides a considerable performance boost. An object pool is a cache of already instanced objects. A client can prevent the creation of new instances of an expensive-to-create object by getting a cached instance from the pool. A very good article on object pools can be found at https://sourcemaking.com/design_patterns/object_pool.

BlockingObjectPool aims to fill a gap in the offer of .NET object pools, in which:

* Objects must not be concurrently shared between different consumers.
* The configured pool size must not be exceeded, so that consumers get blocked until some object from the pool is available.
* A validation can be performed over pooled objects when acquired from or returned to the pool, so that stale objects can automatically be invalidated and re-created.
* Few or no required dependencies.

## Features

BlockingObjectPool is packed with the following features:

* Thread safe object pool with configurable initial and maximum size.
* Absolute freedom in the type of objects that can be stored and how they are created and destroyed.
* Blocking of clients when no objects are available in the pool.
* Configurable validation when objects are acquired from or returned to the pool. Objects that fail to validate are invalidated and re-created.

## Installation

BlockingObjectPool is available at NuGet: https://www.nuget.org/packages/BlockingObjectPool

In order to install it using the NuGet Package Manager:
```bash
PM> Install-Package BlockingObjectPool
```

In order to install it using .NET CLI:
```bash
dotnet add package BlockingObjectPool
```

BlockingObjectPool has no external dependencies.

## How to use it

BlockingObjectPool provides a builder, `PoolBuilder`, that allows to build new object pools. On pool creation it is possible to specify an initial size, a maximum size, a factory for pooled objects and a pooled object validator. The pooled object validator is optional (if not provided, pooled objects will not be validated with acquired from or returned to the pool).

Objects can be acquired from the pool via `Acquire()` and returned to the pool via `Return()`.

When the pool is no longer needed `IObjectPool::Dispose()` will free it and all the pooled objects in it.

```csharp
{
    IPoolBuilder<IExpensiveObject> poolBuilder = new PoolBuilder<IExpensiveObject>();
    IObjectPool<IExpensiveObject> pool = poolBuilder.InitialSize(0)
            .MaxSize(20)
            .WithFactory(new ExpensiveObjectFactory())
            .WithValidator(new ExpensiveObjectValidator())
            .Instance();

    IExpensiveObject pooledObject = pool.Acquire();
    pooledObject.DoStuff();
    pool.Return(pooledObject);

    ...

    // Dispose the pool and pooled objects.
    pool.Dispose();
}

class ExpensiveObjectFactory: IPooledObjectFactory<IExpensiveObject>
{
    /// <summary>
    /// Creates an object to be pooled.
    /// </summary>
    /// <returns>The object created by the factory</returns>
    public IExpensiveObject Create()
    {
        ...
    }

    /// <summary>
    /// Destroys the object in the pool. When overriding this method, exceptions shall be caught, as it will break the 
    /// process and remaining pooled object cannot be destroyed.
    /// </summary>
    /// <param name="obj">The object to be destroyed.</param>
    public void Destroy(IExpensiveObject object)
    {
        ...
    }
}

class ExpensiveObjectValidator : IPooledObjectValidator<IExpensiveObject>
{
    /// <summary>
    /// Indicates if the pool must validate the object just before returning it to the client. If the validation fails the object must be discarded.
    /// </summary>
    public bool ValidateOnAcquire
    {
        get
        {
            return true;
        }
    }

    /// <summary>
    /// Indicates if the pool must validate the object just before returning it to the pool. If the validation fails the object must be discarded.
    /// </summary>
    public bool ValidateOnReturn
    {
        get
        {
            return false;
        }
    }

    /// <summary>
    /// Validates the given instance to check if it is still good to use. This method should never throw exceptions because of regular validation logic.
    /// </summary>
    /// <param name="obj">The instance to validate.</param>
    /// <returns>Return <see langword="true"/> if the object is still valid, <see langword="false"/> otherwise.</returns>
    public bool Validate(IExpensiveObject object)
    {
        ... // Check the object is valid and can still be used.
    }
}
```
