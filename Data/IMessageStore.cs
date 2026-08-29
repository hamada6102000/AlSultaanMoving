using AlSultaanMoving.Models;

namespace AlSultaanMoving.Data;

/// <summary>
/// Persists contact / booking form submissions. The default implementation
/// appends to App_Data/messages.json. Swap for an EF Core-backed store to write
/// to SQL Server / SQLite (see README) — the interface stays the same.
/// </summary>
public interface IMessageStore
{
    Task AddAsync(ContactMessage message);
    Task<IReadOnlyList<ContactMessage>> GetAllAsync();
}
