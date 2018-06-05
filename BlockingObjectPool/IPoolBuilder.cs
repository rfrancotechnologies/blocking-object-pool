// Licensed to the Apache Software Foundation (ASF) under one or more
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership.
// The ASF licenses this file to You under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace BlockingObjectPool
{
    /// <summary>
    /// The interface defines the operations for an object pool configuration descriptor.
    /// The descriptor enables the client to define the key of the object pool, the initial size and max size of the object pool 
    /// and the way to create and destroy the objects contained in the pool.
    /// </summary>
    /// <typeparam name="T">The type of the object contained in the pool.</typeparam>
    public interface IPoolBuilder<T> where T : class
    {
        /// <summary>
        /// Sets the initial size of the object pool. If this method is not called before a pool
        /// is instantiated, it is set to default value 0.
        /// </summary>
        /// <param name="initialSize">The value.</param>
        /// <returns>The pool descriptor with updated initial size.</returns>
        IPoolBuilder<T> InitialSize(int initialSize);

        /// <summary>
        /// Sets the maximum size of the object pool. If this method is not called before a pool is 
        /// instantiated, the pool descriptor sets it to default value 10 and checks whether it's larger than <see cref="InitialSize"/>.
        /// If the value is invalid, the pool is not instantiated.
        /// </summary>
        /// <param name="maxSize">The value.</param>
        /// <returns>The pool descriptor with updated maximum size.</returns>
        IPoolBuilder<T> MaxSize(int maxSize);

        /// <summary>
        /// Defines the factory for creating and destroying the pooled objects.
        /// </summary>
        /// <param name="factory">The object factory.</param>
        /// <returns>The pool descriptor with updated factory.</returns>
        IPoolBuilder<T> WithFactory(IPooledObjectFactory<T> factory);

        /// <summary>
        /// Optional, objects validator. If not set, or null a default implementation will be used. The default implementation makes no validation at all.
        /// </summary>
        /// <param name="validator">The validator to use with the configured object pool</param>
        /// <returns>The pool descriptor with updated factory.</returns>
        IPoolBuilder<T> WithValidator(IPooledObjectValidator<T> validator);

        /// <summary>
        /// Sets the limit of attempts to acquire an if, internally, acquired objects are invalid after tested with the configured <see cref="IPooledObjectValidator{T}"/>.
        /// If this method is not called before a pool is instantiated, the pool descriptor sets it to default value 10.
        /// If the value is invalid (negative), the pool is not instantiated.
        /// </summary>
        /// <param name="acquiredInvalidLimit">The value.</param>
        /// <returns>The pool descriptor with updated maximum size.</returns>
        IPoolBuilder<T> AcquiredInvalidLimit(int acquiredInvalidLimit);

        /// <summary>
        /// Instantiate the object pool.
        /// </summary>
        /// <returns>The new object pool.</returns>
        IObjectPool<T> Instance();
    }
}
