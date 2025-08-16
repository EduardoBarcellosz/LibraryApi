using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechLibrary.Api.Services.LoggedUser;
using TechLibrary.Api.UseCases.Reservations;

namespace TechLibrary.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        [HttpPost]
        [Route("{bookId}")]
        public IActionResult CreateReservation(Guid bookId)
        {
            var loggedUser = new LoggedUserService(HttpContext);

            var useCase = new CreateReservationUseCase(loggedUser);

            useCase.Execute(bookId);

            return NoContent();
        }

        [HttpGet]
        public IActionResult GetUserReservations()
        {
            var loggedUser = new LoggedUserService(HttpContext);

            var useCase = new GetUserReservationsUseCase(loggedUser);

            var response = useCase.Execute();

            return Ok(response);
        }

        [HttpDelete]
        [Route("{reservationId}")]
        public IActionResult CancelReservation(Guid reservationId)
        {
            var loggedUser = new LoggedUserService(HttpContext);

            var useCase = new CancelReservationUseCase(loggedUser);

            useCase.Execute(reservationId);

            return NoContent();
        }
    }
}
