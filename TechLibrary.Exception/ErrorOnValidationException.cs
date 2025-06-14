using System.Net;

namespace TechLibrary.Exception;

public class ErrorOnValidationException(List<string> errorMessages) : TechLibraryException(string.Empty)
{
    private readonly List<string> _errors = errorMessages;
    public override List<string> GetErrorsMessages() => _errors;
    public override HttpStatusCode GetStatusCode() => HttpStatusCode.BadRequest;

} 