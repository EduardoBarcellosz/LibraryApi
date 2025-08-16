namespace TechLibrary.Api.Domain.Entities
{
    public class Reservation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
        public Guid BookId { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CancelledDate { get; set; }
        public DateTime? FulfilledDate { get; set; } // Quando a reserva foi atendida (livro emprestado)

        // Navigation properties
        public Book? Book { get; set; }
        public User? User { get; set; }
    }
}
