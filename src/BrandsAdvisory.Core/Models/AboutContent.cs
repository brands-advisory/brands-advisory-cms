namespace BrandsAdvisory.Core.Models;

public class AboutContent
{
    public string Id { get; set; } = "about";
    public string Type { get; set; } = "about";
    public string Subtitle { get; set; } = string.Empty;
    public string ContactHint { get; set; } = string.Empty;
    public string ProfileUrl { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<ProfileLink> Links { get; set; } = [];
}
