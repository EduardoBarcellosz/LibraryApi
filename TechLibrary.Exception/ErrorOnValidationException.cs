using System.Net;

namespace TechLibrary.Exception;

public class ErrorOnValidationException : TechLibraryException
{
    private readonly List<string> _errors;

    public ErrorOnValidationException(List<string> errorMessages)
    {
        _errors = errorMessages;
    }

    public override List<string> GetErrorsMessages()
    {
        return _errors;
    } 

    public override HttpStatusCode GetHttpStatusCode() => HttpStatusCode.BadRequest;

}