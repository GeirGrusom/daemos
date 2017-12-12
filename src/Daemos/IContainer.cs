// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    /// <summary>
    /// This interface specifies an entire container.
    /// </summary>
    public interface IContainer : IDependencyResolver, IDependencyRegister
    {
        /// <summary>
        /// Creates a proxy of this container. This is used to create a new lifetime.
        /// </summary>
        /// <returns>The proxy container.</returns>
        IContainer CreateProxy();
    }
}
