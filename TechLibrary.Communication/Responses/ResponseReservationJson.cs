namespace TechLibrary.Communication.Responses
{
    public class ResponseReservationJson
    {
        public Guid Id { get; set; }
        public DateTime ReservationDate { get; set; }
        public Guid UserId { get; set; }
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CancelledDate { get; set; }
        public DateTime? FulfilledDate { get; set; }
        public bool IsExpired { get; set; }
        public int DaysUntilExpiration { get; set; }
        // Nova informação: previsão de quando haverá pelo menos um exemplar disponível
        public DateTime PredictedAvailabilityDate { get; set; }
    }
}
