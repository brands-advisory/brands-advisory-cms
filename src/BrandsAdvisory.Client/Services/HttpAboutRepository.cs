using BrandsAdvisory.Core.Interfaces;
using BrandsAdvisory.Core.Models;
using System.Net.Http.Json;

namespace BrandsAdvisory.Client.Services;

/// <summary>
/// WebAssembly client implementation of <see cref="IAboutRepository"/>
/// that calls the <c>/api/about</c> minimal API endpoints on the host server.
/// </summary>
public class HttpAboutRepository(HttpClient http) : IAboutRepository
{
    public async Task<AboutContent> GetOrCreateAsync()
    {
        var result = await http.GetFromJsonAsync<AboutContent>("/api/about");
        return result ?? new AboutContent { Id = "about" };
    }

    public async Task UpsertAsync(AboutContent document)
    {
        var response = await http.PutAsJsonAsync("/api/about", document);
        response.EnsureSuccessStatusCode();
    }

    public Task<AboutContent?> GetByIdAsync(string id) =>
        http.GetFromJsonAsync<AboutContent>($"/api/about");

    public async Task<List<AboutContent>> GetAllAsync()
    {
        var item = await GetOrCreateAsync();
        return [item];
    }

    public Task DeleteAsync(string id) =>
        throw new NotSupportedException("Deleting the About document is not supported.");
}
