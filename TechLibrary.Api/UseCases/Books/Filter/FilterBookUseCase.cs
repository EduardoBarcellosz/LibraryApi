using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechLibrary.Api.Domain.Entities;
using TechLibrary.Api.Infrastructure.DataAccess;
using TechLibrary.Api.Services.LoggedUser;
using TechLibrary.Communication.Requests;
using TechLibrary.Communication.Responses;

namespace TechLibrary.Api.UseCases.Books.Filter
{
    public class FilterBookUseCase
    {
        private const int PAGE_SIZE = 9;
        private readonly LoggedUserService? _loggedUser;

        public FilterBookUseCase(LoggedUserService? loggedUser = null)
        {
            _loggedUser = loggedUser;
        }

        public ResponseBooksJson Execute(RequestFilterBooksJson request)
        {
            var dbContext = new TechLibraryDbContext();

            var query = dbContext.Books.AsQueryable();
            if (string.IsNullOrWhiteSpace(request.Title) == false)
            {
                var titleFilter = request.Title.ToLower();
                query = dbContext.Books.Where(book => book.Title.ToLower().Contains(titleFilter));
            }

            var books = query
                .OrderBy(book => book.Title).ThenBy(book => book.Author)
                .Skip((request.PageNumber - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToList();

            var totalCount = string.IsNullOrWhiteSpace(request.Title)
                ? dbContext.Books.Count()
                : dbContext.Books.Count(book => book.Title.ToLower().Contains(request.Title.ToLower()));

            // Obter o usuário logado se disponível
            Guid? currentUserId = null;
            try
            {
                if (_loggedUser != null)
                {
                    var user = _loggedUser.User(dbContext);
                    currentUserId = user.Id;
                }
            }
            catch
            {
                // Usuário não autenticado ou erro ao obter usuário
            }

            // Calcular disponibilidade para cada livro
            var booksWithAvailability = books.Select(book =>
            {
                var checkedOutCount = dbContext.Checkouts
                    .Count(c => c.BookId == book.Id && c.ReturnedDate == null);

                var availableAmount = book.Amount - checkedOutCount;

                var activeReservationsCount = dbContext.Reservations
                    .Count(r => r.BookId == book.Id && r.IsActive);

                var userHasActiveReservation = false;
                if (currentUserId.HasValue)
                {
                    var currentUserGuid = currentUserId.Value;
                    userHasActiveReservation = dbContext.Reservations.Any(r => r.BookId == book.Id && r.UserId == currentUserGuid && r.IsActive);
                }

                return new ResponseBookJson
                {
                    Id = book.Id,
                    Title = book.Title,
                    Author = book.Author,
                    Amount = book.Amount,
                    AvailableAmount = availableAmount,
                    IsAvailable = availableAmount > 0,
                    ActiveReservationsCount = activeReservationsCount,
                    UserHasActiveReservation = userHasActiveReservation
                };
            }).ToList();

            return new ResponseBooksJson
            {
                Pagination = new ResponsePaginationJson
                {
                    PageNumber = request.PageNumber,
                    TotalCount = totalCount,
                    PageSize = PAGE_SIZE,
                    TotalPages = (int)Math.Ceiling((double)totalCount / PAGE_SIZE)
                },
                Books = booksWithAvailability
            };
        }
    }
}
