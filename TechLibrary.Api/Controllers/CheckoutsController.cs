using Microsoft.AspNetCore.Mvc;
using TechLibrary.Api.Domain.Entities;
using TechLibrary.Api.UseCases.Checkouts;

namespace TechLibrary.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CheckoutsController : ControllerBase
    {
        [HttpPost]
        [Route("{bookId}")]
        public IActionResult CheckoutBook(Guid bookId)
        {
            var useCase = new RegisterBookCheckoutUseCase();

            useCase.Execute(bookId);

            return NoContent();
        }
    }
}