namespace ProjectX.DTOs;

public class HighlightRequest
{
    public required DateTime HighlightStart { get; set; }
    public required DateTime HighlightEnd { get; set; }
}