using Microsoft.AspNetCore.Mvc;
using TechLibrary.Api.Services.LoggedUser;
using TechLibrary.Api.UseCases.Books.Filter;
using TechLibrary.Communication.Requests;
using TechLibrary.Communication.Responses;

namespace TechLibrary.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        [HttpGet("Filter")]
        [ProducesResponseType(typeof(ResponseBooksJson), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Filter(int pageNumber, string? title)
        {
            // Tentar obter o usuário logado se estiver autenticado
            LoggedUserService? loggedUser = null;
            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                loggedUser = new LoggedUserService(HttpContext);
            }

            var useCase = new FilterBookUseCase(loggedUser);

            var request = new RequestFilterBooksJson
            {
                PageNumber = pageNumber,
                Title = title
            };

            var result = useCase.Execute(request);

            // Sempre retornar 200 com estrutura consistente (lista pode estar vazia)
            return Ok(result);
        }
    }
}
