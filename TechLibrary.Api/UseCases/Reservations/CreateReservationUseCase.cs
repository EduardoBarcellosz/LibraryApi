using TechLibrary.Api.Infrastructure.DataAccess;
using TechLibrary.Api.Services.LoggedUser;
using TechLibrary.Exception;

namespace TechLibrary.Api.UseCases.Reservations
{
    public class CreateReservationUseCase
    {
        private readonly LoggedUserService _loggedUser;

        public CreateReservationUseCase(LoggedUserService loggedUser)
        {
            _loggedUser = loggedUser;
        }

        public void Execute(Guid bookId)
        {
            var dbContext = new TechLibraryDbContext();

            Validate(dbContext, bookId);

            var user = _loggedUser.User(dbContext);

            var expectedReturnDate = dbContext.Checkouts
                .Where(c => c.BookId == bookId && c.ReturnedDate == null)
                .OrderBy(c => c.ExpectedReturnDate)
                .Select(c => c.ExpectedReturnDate)
                .FirstOrDefault();

            var entity = new Domain.Entities.Reservation
            {
                UserId = user.Id,
                BookId = bookId,
                ExpectedReturnDate = expectedReturnDate
            };

            dbContext.Reservations.Add(entity);
            dbContext.SaveChanges();
        }

        private void Validate(TechLibraryDbContext dbContext, Guid bookId)
        {
            var user = _loggedUser.User(dbContext);

            var book = dbContext.Books.FirstOrDefault(book => book.Id == bookId);
            if (book is null)
            {
                throw new NotFoundException("Livro não encontrado.");
            }

            // Verificar se o usuário já tem uma reserva ativa para este livro
            var existingReservation = dbContext.Reservations
                .FirstOrDefault(r => r.BookId == bookId && r.UserId == user.Id && r.IsActive);

            if (existingReservation is not null)
            {
                throw new ConflictException("Você já possui uma reserva ativa para este livro.");
            }

            // Verificar se o usuário já tem o livro emprestado
            var existingCheckout = dbContext.Checkouts
                .FirstOrDefault(c => c.BookId == bookId && c.UserId == user.Id && c.ReturnedDate == null);

            if (existingCheckout is not null)
            {
                throw new ConflictException("Você já possui este livro emprestado.");
            }

            // Verificar se o livro está disponível
            var checkedOutCount = dbContext.Checkouts
                .Count(c => c.BookId == bookId && c.ReturnedDate == null);

            if (checkedOutCount < book.Amount)
            {
                throw new ConflictException("Este livro está disponível para empréstimo. Não é necessário fazer uma reserva.");
            }
        }
    }
}
