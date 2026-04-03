using BrandsAdvisory.Core.Models;

namespace BrandsAdvisory.Core.Interfaces;

/// <summary>
/// Generic repository interface for Cosmos DB documents.
/// </summary>
/// <typeparam name="T">The document type, must inherit <see cref="CosmosDocument"/>.</typeparam>
public interface IRepository<T> where T : CosmosDocument
{
    /// <summary>Returns a document by its ID, or <c>null</c> if not found.</summary>
    /// <param name="id">The Cosmos DB document ID.</param>
    Task<T?> GetByIdAsync(string id);

    /// <summary>Returns all documents of this type.</summary>
    Task<List<T>> GetAllAsync();

    /// <summary>Inserts or replaces a document.</summary>
    /// <param name="document">The document to upsert.</param>
    Task UpsertAsync(T document);

    /// <summary>Deletes a document by its ID.</summary>
    /// <param name="id">The Cosmos DB document ID.</param>
    Task DeleteAsync(string id);
}
