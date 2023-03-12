using Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using Application.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace Api.Filters
{
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        private readonly IDictionary<Type, Action<ExceptionContext>> _exceptionHandlers;

        public ApiExceptionFilter()
        {
            _exceptionHandlers = new Dictionary<Type, Action<ExceptionContext>>
            {
                { typeof(InputValidationException), HandleInputValidationException },
                { typeof(BusinessValidationException), HandleBusinessValidationException }
            };
        }

        public override void OnException(ExceptionContext context)
        {
            HandleException(context);

            base.OnException(context);
        }

        private void HandleException(ExceptionContext context)
        {
            var type = context.Exception.GetType();

            if (_exceptionHandlers.ContainsKey(type))
            {
                _exceptionHandlers[type](context);

                return;
            }

            HandleUnknownException(context);
        }

        private void HandleUnknownException(ExceptionContext context)
        {
            const string message = "Произошла внутренняя ошибка сервера";
            
            var details = new ProblemDetails()
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = message
            };

            context.Result = new ObjectResult(details)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            context.ExceptionHandled = true;
        }

        private void HandleInputValidationException(ExceptionContext context)
        {
            var exception = context.Exception as InputValidationException;

            var details = new ValidationProblemDetails(exception.GroupErrorsByProperty());

            details.Title = exception?.Message;

            context.Result = new BadRequestObjectResult(details);

            context.ExceptionHandled = true;
        }

        private void HandleBusinessValidationException(ExceptionContext context)
        {
            var exception = context.Exception as BusinessValidationException;

            var details = new ValidationProblemDetails(exception.GroupErrorsByProperty());

            details.Title = exception?.Message;

            context.Result = new ConflictObjectResult(details);

            context.ExceptionHandled = true;
        }
    }
}
