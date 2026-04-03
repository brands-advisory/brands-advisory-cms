namespace BrandsAdvisory.Core.Models;

public class AboutContent : CosmosDocument
{
    public override string Type => "about";
    public string Subtitle { get; set; } = string.Empty;
    public string ContactHint { get; set; } = string.Empty;
    public string ProfileUrl { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<ProfileLink> Links { get; set; } = [];
}
