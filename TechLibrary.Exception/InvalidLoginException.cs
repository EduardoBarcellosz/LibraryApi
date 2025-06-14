using System.Net;

namespace TechLibrary.Exception
{
    public class InvalidLoginException() : TechLibraryException("Email ou Senha Inválidos")
    {
        public override List<string> GetErrorsMessages() => [Message];
        
        public override HttpStatusCode GetStatusCode() => HttpStatusCode.Unauthorized;
    }
}
