namespace SafeHarbor.DTOs;

public sealed record ApiErrorEnvelope(string ErrorCode, string Message, string TraceId);
