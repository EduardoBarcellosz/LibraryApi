using System.Net;
using TechLibrary.Exception;

namespace TechLibrary.Exception;

public class ConflictException(string message) : TechLibraryException(message)
{
    public override List<string> GetErrorsMessages() => [Message];
    public override HttpStatusCode GetStatusCode() => HttpStatusCode.Conflict;

}


