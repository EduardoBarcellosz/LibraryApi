using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TechLibrary.Communication.Responses;
using TechLibrary.Exception;

namespace TechLibrary.Api.Filters;

public class ExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        // Logar detalhes no console para facilitar o diagnóstico em desenvolvimento
        try
        {
            System.Console.WriteLine($"[Exception] {DateTime.UtcNow:o}\n{context.Exception}");
        }
        catch { }

        if (context.Exception is TechLibraryException techLibraryException)
        {
            context.HttpContext.Response.StatusCode = (int)techLibraryException.GetStatusCode();
            context.Result = new ObjectResult(new ResponseErrorMessagesJson
            {
                Errors = techLibraryException.GetErrorsMessages()
            });
        }
        else
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            // Expor a mensagem real durante o desenvolvimento para facilitar o diagnóstico
            var message = context.Exception?.Message ?? "Erro desconhecido!";
            context.Result = new ObjectResult(new ResponseErrorMessagesJson
            {
                Errors = [message]
            });
        }
    }
}