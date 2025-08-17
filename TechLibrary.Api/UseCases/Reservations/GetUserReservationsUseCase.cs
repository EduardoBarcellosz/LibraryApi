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

                // Cálculo da previsão de disponibilidade:
                // Se já existe exemplar livre (loans ativos < Amount) então agora.
                // Caso contrário, pega a menor ExpectedReturnDate entre os empréstimos ativos.
                DateTime predictedAvailabilityDate;
                if (reservation.Book is null)
                {
                    predictedAvailabilityDate = DateTime.UtcNow; // fallback
                }
                else
                {
                    var activeLoans = dbContext.Checkouts
                        .Where(c => c.BookId == reservation.BookId && c.ReturnedDate == null)
                        .Select(c => c.ExpectedReturnDate)
                        .ToList();

                    if (activeLoans.Count < reservation.Book.Amount)
                    {
                        predictedAvailabilityDate = DateTime.UtcNow; // já disponível
                    }
                    else
                    {
                        predictedAvailabilityDate = activeLoans.Min();
                    }
                }

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
                    DaysUntilExpiration = Math.Max(0, daysUntilExpiration),
                    
                });
            }

            return response;
        }
    }
}
