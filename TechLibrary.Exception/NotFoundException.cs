using System.Net;

namespace TechLibrary.Exception
{
    public class NotFoundException(string message) : TechLibraryException(message)
    {
        public override List<string> GetErrorsMessages() => [Message];        
        public override HttpStatusCode GetStatusCode() => HttpStatusCode.NotFound;
         
    }
}
