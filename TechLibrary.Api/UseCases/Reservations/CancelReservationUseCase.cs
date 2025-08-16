using TechLibrary.Api.Infrastructure.DataAccess;
using TechLibrary.Api.Services.LoggedUser;
using TechLibrary.Exception;

namespace TechLibrary.Api.UseCases.Reservations
{
    public class CancelReservationUseCase
    {
        private readonly LoggedUserService _loggedUser;

        public CancelReservationUseCase(LoggedUserService loggedUser)
        {
            _loggedUser = loggedUser;
        }

        public void Execute(Guid reservationId)
        {
            var dbContext = new TechLibraryDbContext();
            var user = _loggedUser.User(dbContext);

            var reservation = dbContext.Reservations
                .FirstOrDefault(r => r.Id == reservationId && r.UserId == user.Id);

            if (reservation is null)
            {
                throw new NotFoundException("Reserva não encontrada.");
            }

            if (!reservation.IsActive)
            {
                throw new ConflictException("Esta reserva já foi cancelada ou atendida.");
            }

            reservation.IsActive = false;
            reservation.CancelledDate = DateTime.UtcNow;
            dbContext.SaveChanges();
        }
    }
}
