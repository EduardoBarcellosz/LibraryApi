using TechLibrary.Api.Infrastructure.DataAccess;
using TechLibrary.Exception;

namespace TechLibrary.Api.UseCases.Checkouts
{
    public class RegisterBookCheckoutUseCase
    {
        public void Execute(Guid bookId)
        {
            var dbContext = new TechLibraryDbContext();
            Validate(dbContext, bookId);

            dbContext.Checkouts.Add(new Domain.Entities.Checkout { BookId = bookId, CheckoutDate = DateTime.UtcNow });

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
                .Count(checkout => checkout.BookId == bookId && checkout.ReturnDate == null);
            if(amountBookNotReturned == book.Amount)
            {
                throw new System.Exception("All copies of this book are currently checked out. Please try again later.");
            }
        }
    }
}