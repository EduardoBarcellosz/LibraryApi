using TechLibrary.Api.Infrastructure.DataAccess;
using TechLibrary.Api.Services.LoggedUser;
using TechLibrary.Communication.Responses;
using Microsoft.EntityFrameworkCore;

namespace TechLibrary.Api.UseCases.Checkouts
{
    public class GetUserCheckoutsUseCase
    {
        private readonly LoggedUserService _loggedUser;

        public GetUserCheckoutsUseCase(LoggedUserService loggedUser)
        {
            _loggedUser = loggedUser;
        }

        public ResponseCheckoutsJson Execute()
        {
            var dbContext = new TechLibraryDbContext();
            var user = _loggedUser.User(dbContext);

            var checkouts = dbContext.Checkouts
                .Include(c => c.Book)
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CheckoutDate)
                .ToList();

            var response = new ResponseCheckoutsJson();

            foreach (var checkout in checkouts)
            {
                var isOverdue = checkout.ReturnedDate == null && DateTime.UtcNow > checkout.ExpectedReturnDate;
                var daysOverdue = isOverdue ? (DateTime.UtcNow - checkout.ExpectedReturnDate).Days : 0;

                response.Checkouts.Add(new ResponseCheckoutJson
                {
                    Id = checkout.Id,
                    CheckoutDate = checkout.CheckoutDate,
                    UserId = checkout.UserId,
                    BookId = checkout.BookId,
                    BookTitle = checkout.Book?.Title ?? string.Empty,
                    BookAuthor = checkout.Book?.Author ?? string.Empty,
                    ExpectedReturnDate = checkout.ExpectedReturnDate,
                    ReturnedDate = checkout.ReturnedDate,
                    IsOverdue = isOverdue,
                    DaysOverdue = daysOverdue
                });
            }

            return response;
        }
    }
}
