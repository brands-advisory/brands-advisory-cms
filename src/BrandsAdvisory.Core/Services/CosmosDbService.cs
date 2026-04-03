using BrandsAdvisory.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace BrandsAdvisory.Core.Services;

public class CosmosDbService : ICosmosDbService
{
    private readonly Container _container;

    public CosmosDbService(IConfiguration configuration)
    {
        var endpoint = configuration["CosmosDb:EndpointUri"]!;
        var key = configuration["CosmosDb:PrimaryKey"]!;
        var databaseId = configuration["CosmosDb:DatabaseId"]!;
        var containerName = configuration["CosmosDb:ContainerName"]!;

        var client = new CosmosClient(endpoint, key, new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        });

        _container = client.GetContainer(databaseId, containerName);
    }

    public async Task<AboutContent> GetAboutAsync()
    {
        var response = await _container.ReadItemAsync<AboutContent>(
            "about", new PartitionKey("about"));
        return response.Resource;
    }

    public async Task UpsertAboutAsync(AboutContent about)
    {
        await _container.UpsertItemAsync(about, new PartitionKey("about"));
    }

    public async Task<List<Article>> GetArticlesAsync(bool publishedOnly)
    {
        var sql = publishedOnly
            ? "SELECT * FROM c WHERE c.type = 'article' AND c.isPublished = true"
            : "SELECT * FROM c WHERE c.type = 'article'";

        var query = new QueryDefinition(sql);
        var options = new QueryRequestOptions { PartitionKey = new PartitionKey("article") };

        return await ExecuteQueryAsync<Article>(query, options);
    }

    public async Task<Article?> GetArticleBySlugAsync(string slug)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = 'article' AND c.slug = @slug")
            .WithParameter("@slug", slug);
        var options = new QueryRequestOptions { PartitionKey = new PartitionKey("article") };

        var results = await ExecuteQueryAsync<Article>(query, options);
        return results.FirstOrDefault();
    }

    public async Task<Article?> GetArticleByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Article>(id, new PartitionKey("article"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task UpsertArticleAsync(Article article)
    {
        await _container.UpsertItemAsync(article, new PartitionKey("article"));
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = 'project' ORDER BY c.sortOrder");
        var options = new QueryRequestOptions { PartitionKey = new PartitionKey("project") };

        return await ExecuteQueryAsync<Project>(query, options);
    }

    public async Task UpsertProjectAsync(Project project)
    {
        await _container.UpsertItemAsync(project, new PartitionKey("project"));
    }

    public async Task DeleteProjectAsync(string id)
    {
        await _container.DeleteItemAsync<Project>(id, new PartitionKey("project"));
    }

    private async Task<List<T>> ExecuteQueryAsync<T>(
        QueryDefinition query, QueryRequestOptions options)
    {
        var iterator = _container.GetItemQueryIterator<T>(query, requestOptions: options);
        var results = new List<T>();

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            results.AddRange(page);
        }

        return results;
    }
}
