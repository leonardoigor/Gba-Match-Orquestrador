namespace Gba_Domain.Models;

public class GbaMatchInfo
{
    public Guid MatchId { get; set; }
    public string PodName { get; set; }
    public string VideoUrl { get; set; }
    public string InputUrl { get; set; }
    public string Status { get; set; }
}
