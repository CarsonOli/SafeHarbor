namespace SafeHarbor.Services;

public interface IDataRetentionRedactionService
{
    T ApplyRetentionPolicy<T>(T source, string reportType);
    string RedactFreeText(string value);
}

public sealed class DataRetentionRedactionService : IDataRetentionRedactionService
{
    public T ApplyRetentionPolicy<T>(T source, string reportType)
    {
        // NOTE: Hook for injecting report/export specific retention windows.
        // The implementation returns source unchanged until policy details are finalized.
        _ = reportType;
        return source;
    }

    public string RedactFreeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return "[REDACTED]";
    }
}
