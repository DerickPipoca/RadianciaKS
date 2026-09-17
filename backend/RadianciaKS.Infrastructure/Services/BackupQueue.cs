using System.Threading.Channels;
using RadianciaKS.Application.Interfaces;

namespace RadianciaKS.Infrastructure.Services
{
    public class BackupQueue : IBackupQueue
    {
        private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

        public ValueTask QueueBackupAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            return _queue.Writer.WriteAsync(tenantId, cancellationToken);
        }

        public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAllAsync(cancellationToken);
        }
    }
}