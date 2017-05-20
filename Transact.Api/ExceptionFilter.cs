using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Transact
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if(context.Exception is TransactionConflictException)
            {
                context.Result = new StatusCodeResult(409);
                context.ExceptionHandled = true;
            }
            else if(context.Exception is TransactionMissingException)
            {
                context.Result = new StatusCodeResult(404);
                context.ExceptionHandled = true;
            }
            else if(context.Exception is TransactionException)
            {
                context.Result = new StatusCodeResult(500);
                context.ExceptionHandled = true;
            }
            else if(context.Exception is TimeoutException)
            {
                context.Result = new StatusCodeResult(504);
                context.ExceptionHandled = true;
            }
        }
    }
}
