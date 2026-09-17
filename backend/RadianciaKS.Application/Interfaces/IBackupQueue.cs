namespace RadianciaKS.Application.Interfaces
{
    public interface IBackupQueue
    {
        ValueTask QueueBackupAsync(Guid tenantId, CancellationToken cancellationToken = default);
        IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
    }
}