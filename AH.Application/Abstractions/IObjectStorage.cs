namespace AH.Application.Abstractions;

// Binary objects (issued certificates, later uploads) that do not belong in PostgreSQL. Keys are
// '/'-separated paths; the database keeps the key next to the row that owns the object.
public interface IObjectStorage
{
    Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    // Null when the object does not exist.
    Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
