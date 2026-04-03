namespace BrandsAdvisory.Core.Models;

public class Article
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "article";
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? PublishedDate { get; set; }
    public bool IsPublished { get; set; }
    public List<string> Tags { get; set; } = [];
}
