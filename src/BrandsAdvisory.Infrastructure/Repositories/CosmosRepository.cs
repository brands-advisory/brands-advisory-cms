using BrandsAdvisory.Core.Interfaces;
using BrandsAdvisory.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace BrandsAdvisory.Infrastructure.Repositories;

/// <summary>
/// Generic Cosmos DB repository implementation.
/// </summary>
/// <typeparam name="T">The document type, must inherit <see cref="CosmosDocument"/>.</typeparam>
public class CosmosRepository<T>(CosmosClient client, IConfiguration configuration)
    : IRepository<T> where T : CosmosDocument
{
    /// <summary>The Cosmos DB container this repository targets.</summary>
    protected readonly Container Container = client.GetContainer(
        configuration["CosmosDb:DatabaseId"]!,
        configuration["CosmosDb:ContainerName"]!);

    private static string TypeKey => Activator.CreateInstance<T>().Type;

    /// <inheritdoc/>
    public async Task<T?> GetByIdAsync(string id)
    {
        try
        {
            var response = await Container.ReadItemAsync<T>(id, new PartitionKey(TypeKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<T>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type")
            .WithParameter("@type", TypeKey);
        var options = new QueryRequestOptions { PartitionKey = new PartitionKey(TypeKey) };

        return await ExecuteQueryAsync(query, options);
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(T document)
    {
        document.UpdatedAt = DateTime.UtcNow;
        await Container.UpsertItemAsync(document, new PartitionKey(document.Type));
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id)
    {
        await Container.DeleteItemAsync<T>(id, new PartitionKey(TypeKey));
    }

    /// <summary>
    /// Executes a parameterised query and returns all matching documents.
    /// </summary>
    /// <param name="query">The query definition.</param>
    /// <param name="options">Optional per-request options, e.g. partition key.</param>
    protected async Task<List<T>> ExecuteQueryAsync(
        QueryDefinition query, QueryRequestOptions options)
    {
        var iterator = Container.GetItemQueryIterator<T>(query, requestOptions: options);
        var results = new List<T>();

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            results.AddRange(page);
        }

        return results;
    }
}
