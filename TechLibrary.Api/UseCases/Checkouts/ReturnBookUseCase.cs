using TechLibrary.Api.Infrastructure.DataAccess;
using TechLibrary.Api.Services.LoggedUser;
using TechLibrary.Exception;
using Microsoft.EntityFrameworkCore;

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
            // Estratégia:
            // 1. Validar o checkout do usuário atual e marcar devolução.
            // 2. Verificar se, após a devolução, há reserva(s) ativa(s) para o livro.
            // 3. Se houver, pegar a reserva mais antiga (FIFO) ainda ativa.
            // 4. Antes de criar novo empréstimo, garantir que o usuário da reserva não já pegou o livro.
            // 5. Criar novo checkout para o usuário da reserva e marcar reserva como atendida (FulfilledDate e IsActive = false).
            // Obs: Caso existam várias reservas e múltiplos exemplares fiquem disponíveis, a próxima devolução repetirá o processo.

            var dbContext = new TechLibraryDbContext();
            var user = _loggedUser.User(dbContext);

            var checkout = dbContext.Checkouts
                .Include(c => c.Book)
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

            // Persistir a devolução primeiro para liberar a cópia.
            dbContext.SaveChanges();

            var bookId = checkout.BookId;

            // Verificar quantos exemplares continuam emprestados após essa devolução
            var activeLoansAfterReturn = dbContext.Checkouts
                .Count(c => c.BookId == bookId && c.ReturnedDate == null);

            var book = dbContext.Books.FirstOrDefault(b => b.Id == bookId);
            if (book is null)
            {
                return; // book removido? nada a fazer
            }

            // Quantidade de exemplares disponíveis agora
            var currentlyAvailable = book.Amount - activeLoansAfterReturn;

            if (currentlyAvailable <= 0)
            {
                return; // ainda não há cópia livre (caso de múltiplos returns concorrentes)
            }

            // Selecionar a próxima reserva ativa mais antiga para este livro (excluindo a do usuário que acabou de devolver, se existir)
            var nextReservation = dbContext.Reservations
                .Where(r => r.BookId == bookId && r.IsActive && r.UserId != user.Id)
                .OrderBy(r => r.ReservationDate)
                .FirstOrDefault();

            if (nextReservation is null)
            {
                return; // Ninguém esperando
            }

            // Verifica se o usuário da reserva já possui o livro (pode ter feito checkout em corrida de condição)
            var reservationUserAlreadyHasBook = dbContext.Checkouts.Any(c => c.BookId == bookId && c.UserId == nextReservation.UserId && c.ReturnedDate == null);
            if (reservationUserAlreadyHasBook)
            {
                // Cancelar/atender a reserva para evitar ficar presa (marcar como atendida sem novo empréstimo)
                nextReservation.IsActive = false;
                nextReservation.FulfilledDate = DateTime.UtcNow;
                dbContext.SaveChanges();
                return;
            }

            // Criar novo empréstimo automático para o usuário da reserva
            var autoCheckout = new Domain.Entities.Checkout
            {
                UserId = nextReservation.UserId,
                BookId = bookId,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(14) // mesmo padrão de RegisterBookCheckoutUseCase
            };
            dbContext.Checkouts.Add(autoCheckout);

            // Marcar reserva como atendida
            nextReservation.IsActive = false;
            nextReservation.FulfilledDate = DateTime.UtcNow;

            dbContext.SaveChanges();
        }
    }
}
