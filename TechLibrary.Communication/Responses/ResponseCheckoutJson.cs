namespace TechLibrary.Communication.Responses
{
    public class ResponseCheckoutJson
    {
        public Guid Id { get; set; }
        public DateTime CheckoutDate { get; set; }
        public Guid UserId { get; set; }
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public DateTime ExpectedReturnDate { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
    }
}
