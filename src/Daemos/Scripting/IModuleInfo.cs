// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    /// <summary>
    /// Defines an interface for describing a module
    /// </summary>
    public interface IModuleInfo
    {
        /// <summary>
        /// Initializes dependency register. This is where dependency injection is defined
        /// </summary>
        /// <param name="register">Register for dependency injection</param>
        void OnInitializeRegister(IDependencyRegister register);
    }
}
