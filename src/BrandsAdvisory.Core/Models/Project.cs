namespace BrandsAdvisory.Core.Models;

public class Project
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "project";
    public string Title { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public int SortOrder { get; set; }
}
