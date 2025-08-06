namespace Gba_Match_Orquestrador.Models
{
    public class MatchRequest
    {
        public string MatchId { get; set; } = $"match-{Guid.NewGuid().ToString().Substring(0, 6)}";
        public int PortVideo { get; set; } = RandomPort();
        public int PortInput { get; set; } = RandomPort();

        private static int RandomPort() => new Random().Next(30000, 32767);
    }
}
