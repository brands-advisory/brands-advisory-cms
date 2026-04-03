using BrandsAdvisory.Core.Models;

namespace BrandsAdvisory.Core.Services;

public interface ICosmosDbService
{
    Task<AboutContent> GetAboutAsync();
    Task UpsertAboutAsync(AboutContent about);
    Task<List<Article>> GetArticlesAsync(bool publishedOnly);
    Task<Article?> GetArticleBySlugAsync(string slug);
    Task<Article?> GetArticleByIdAsync(string id);
    Task UpsertArticleAsync(Article article);
    Task<List<Project>> GetProjectsAsync();
    Task UpsertProjectAsync(Project project);
    Task DeleteProjectAsync(string id);
}
