using TechLibrary.Api.Infrastructure.DataAccess;
using TechLibrary.Api.Services.LoggedUser;
using TechLibrary.Exception;

namespace TechLibrary.Api.UseCases.Checkouts
{
    public class RegisterBookCheckoutUseCase
    {
        private const int MAX_LOAN_DAYS = 14;

        private readonly LoggedUserService _loggedUser;
        public RegisterBookCheckoutUseCase(LoggedUserService loggedUser)
        {
            _loggedUser = loggedUser;
        }

        public void Execute(Guid bookId)
        {
            var dbContext = new TechLibraryDbContext();

            Validate(dbContext, bookId);

            var user = _loggedUser.User(dbContext);

            // Verifica se o usuário já possui um empréstimo ativo deste mesmo livro
            var userAlreadyHasActiveLoan = dbContext.Checkouts.Any(c => c.BookId == bookId && c.UserId == user.Id && c.ReturnedDate == null);
            if (userAlreadyHasActiveLoan)
            {
                throw new ConflictException("Usuário já possui este livro em empréstimo ativo.");
            }

            var entity = new Domain.Entities.Checkout
            {
                UserId = user.Id,
                BookId = bookId,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(MAX_LOAN_DAYS)
            };

            dbContext.Checkouts.Add(entity);

            // Se o usuário tinha uma reserva ativa para este livro, marcar como atendida
            var userReservation = dbContext.Reservations
                .FirstOrDefault(r => r.BookId == bookId && r.UserId == user.Id && r.IsActive);

            if (userReservation is not null)
            {
                userReservation.IsActive = false;
                userReservation.FulfilledDate = DateTime.UtcNow;
            }

            dbContext.SaveChanges();
        }

        private void Validate(TechLibraryDbContext dbContext, Guid bookId)
        {
            var book = dbContext.Books.FirstOrDefault(book => book.Id == bookId);

            if (book is null)
            {
                throw new NotFoundException("Book not found.");
            }

            var amountBookNotReturned = dbContext
                .Checkouts
                .Count(checkout => checkout.BookId == bookId && checkout.ReturnedDate == null);
            if (amountBookNotReturned == book.Amount)
            {
                throw new ConflictException("Livro não está disponível.");
            }
        }
    }
}