using System.Net;

namespace TechLibrary.Exception
{
    public class InvalidLoginException : TechLibraryException
    {
        public override List<string> GetErrorsMessages() => ["Email ou Senha Inválidos"];
        
        public override HttpStatusCode GetStatusCode() => HttpStatusCode.Unauthorized;
    }
}
