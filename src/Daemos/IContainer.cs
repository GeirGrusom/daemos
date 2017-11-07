// <copyright file="IContainer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos
{
    public interface IContainer : IDependencyResolver, IDependencyRegister
    {
        IContainer CreateProxy();
    }
}
