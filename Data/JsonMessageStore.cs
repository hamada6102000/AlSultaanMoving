using System.Text.Json;
using AlSultaanMoving.Models;

namespace AlSultaanMoving.Data;

/// <summary>
/// Thread-safe file-backed store for contact submissions (App_Data/messages.json).
/// Good enough for a brochure site with no admin; replace with EF Core for scale.
/// </summary>
public class JsonMessageStore : IMessageStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public JsonMessageStore(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "messages.json");
    }

    private async Task<List<ContactMessage>> LoadAsync()
    {
        if (!File.Exists(_path)) return new List<ContactMessage>();
        var json = await File.ReadAllTextAsync(_path);
        if (string.IsNullOrWhiteSpace(json)) return new List<ContactMessage>();
        return JsonSerializer.Deserialize<List<ContactMessage>>(json) ?? new List<ContactMessage>();
    }

    public async Task AddAsync(ContactMessage message)
    {
        await _lock.WaitAsync();
        try
        {
            var list = await LoadAsync();
            message.Id = (list.Count == 0 ? 0 : list.Max(m => m.Id)) + 1;
            message.CreatedAt = DateTime.UtcNow;
            list.Add(message);
            await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(list, _json));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<ContactMessage>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try { return await LoadAsync(); }
        finally { _lock.Release(); }
    }
}
