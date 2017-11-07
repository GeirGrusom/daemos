// <copyright file="ErrorController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Defines a controller to handle and display error messages
    /// </summary>
    public class ErrorController : Controller
    {
        /// <summary>
        /// Returns a 404 Not Found
        /// </summary>
        /// <returns><see cref="NotFoundResult"/></returns>
        [HttpGet]
        public NotFoundResult ResourceNotFound()
        {
            return this.NotFound();
        }
    }
}
