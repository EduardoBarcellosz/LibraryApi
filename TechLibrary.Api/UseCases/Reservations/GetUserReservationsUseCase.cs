using TechLibrary.Api.Infrastructure.DataAccess;
using TechLibrary.Api.Services.LoggedUser;
using TechLibrary.Communication.Responses;
using Microsoft.EntityFrameworkCore;

namespace TechLibrary.Api.UseCases.Reservations
{
    public class GetUserReservationsUseCase
    {
        private readonly LoggedUserService _loggedUser;

        public GetUserReservationsUseCase(LoggedUserService loggedUser)
        {
            _loggedUser = loggedUser;
        }

        public ResponseReservationsJson Execute()
        {
            var dbContext = new TechLibraryDbContext();
            var user = _loggedUser.User(dbContext);

            var reservations = dbContext.Reservations
                .Include(r => r.Book)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.ReservationDate)
                .ToList();

            var response = new ResponseReservationsJson();

            foreach (var reservation in reservations)
            {
                var isExpired = reservation.IsActive && DateTime.UtcNow > reservation.ExpirationDate;
                var daysUntilExpiration = reservation.IsActive
                    ? (reservation.ExpirationDate - DateTime.UtcNow).Days
                    : 0;

                response.Reservations.Add(new ResponseReservationJson
                {
                    Id = reservation.Id,
                    ReservationDate = reservation.ReservationDate,
                    UserId = reservation.UserId,
                    BookId = reservation.BookId,
                    BookTitle = reservation.Book?.Title ?? string.Empty,
                    BookAuthor = reservation.Book?.Author ?? string.Empty,
                    ExpirationDate = reservation.ExpirationDate,
                    IsActive = reservation.IsActive,
                    CancelledDate = reservation.CancelledDate,
                    FulfilledDate = reservation.FulfilledDate,
                    IsExpired = isExpired,
                    DaysUntilExpiration = Math.Max(0, daysUntilExpiration)
                });
            }

            return response;
        }
    }
}
