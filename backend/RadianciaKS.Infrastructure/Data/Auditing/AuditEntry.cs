using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Data.Auditing
{
    public class AuditEntry
    {
        public EntityEntry Entry { get; }
        public Guid? UserId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object?> OldValues { get; } = new();
        public Dictionary<string, object?> NewValues { get; } = new();

        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public AuditLog ToAuditLog()
        {
            return new AuditLog
            {
                CreatedAt = DateTime.UtcNow,
                UserId = UserId,
                TableName = TableName,
                Action = Action,
                OldValues = (OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues))!,
                NewValues = (NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues))!,
            };
        }
    }
}