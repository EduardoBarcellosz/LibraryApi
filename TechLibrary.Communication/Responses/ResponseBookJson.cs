namespace TechLibrary.Communication.Responses
{
    public class ResponseBookJson
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int Amount { get; set; }
        public int AvailableAmount { get; set; }
        public bool IsAvailable { get; set; }
        public int ActiveReservationsCount { get; set; }
        public bool UserHasActiveReservation { get; set; }
    }
}
