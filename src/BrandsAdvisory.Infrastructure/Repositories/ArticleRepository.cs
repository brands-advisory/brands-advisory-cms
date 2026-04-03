using BrandsAdvisory.Core.Interfaces;
using BrandsAdvisory.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace BrandsAdvisory.Infrastructure.Repositories;

/// <summary>
/// Cosmos DB repository for <see cref="Article"/> documents.
/// </summary>
public class ArticleRepository(CosmosClient client, IConfiguration configuration)
    : CosmosRepository<Article>(client, configuration), IArticleRepository
{
    /// <inheritdoc/>
    public async Task<Article?> GetBySlugAsync(string slug)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = 'article' AND c.slug = @slug")
            .WithParameter("@slug", slug);
        var options = new QueryRequestOptions { PartitionKey = new PartitionKey("article") };

        var results = await ExecuteQueryAsync(query, options);
        return results.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<List<Article>> GetPublishedAsync()
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = 'article' AND c.isPublished = true " +
            "ORDER BY c.publishedDate DESC");
        var options = new QueryRequestOptions { PartitionKey = new PartitionKey("article") };

        return await ExecuteQueryAsync(query, options);
    }
}
