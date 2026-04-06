namespace SafeHarbor.Services;

public interface IAuditLogger
{
    void RecordMutation(string recordType, string operation, Guid recordId, string actor);
}

public sealed class AuditLogger(ILogger<AuditLogger> logger) : IAuditLogger
{
    public void RecordMutation(string recordType, string operation, Guid recordId, string actor)
    {
        logger.LogInformation(
            "AUDIT mutation: {RecordType} {Operation} for {RecordId} by {Actor} at {TimestampUtc}",
            recordType,
            operation,
            recordId,
            actor,
            DateTimeOffset.UtcNow);
    }
}
