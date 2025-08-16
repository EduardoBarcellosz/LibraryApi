using TechLibrary.Api.Infrastructure.DataAccess;
using TechLibrary.Api.Services.LoggedUser;
using TechLibrary.Exception;

namespace TechLibrary.Api.UseCases.Checkouts
{
    public class ReturnBookUseCase
    {
        private readonly LoggedUserService _loggedUser;

        public ReturnBookUseCase(LoggedUserService loggedUser)
        {
            _loggedUser = loggedUser;
        }

        public void Execute(Guid checkoutId)
        {
            var dbContext = new TechLibraryDbContext();
            var user = _loggedUser.User(dbContext);

            var checkout = dbContext.Checkouts
                .FirstOrDefault(c => c.Id == checkoutId && c.UserId == user.Id);

            if (checkout is null)
            {
                throw new NotFoundException("Checkout not found.");
            }

            if (checkout.ReturnedDate != null)
            {
                throw new ConflictException("Este livro já foi devolvido.");
            }

            checkout.ReturnedDate = DateTime.UtcNow;
            dbContext.SaveChanges();
        }
    }
}
