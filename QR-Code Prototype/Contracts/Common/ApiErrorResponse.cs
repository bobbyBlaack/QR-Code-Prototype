namespace QR_Code_Prototype.Contracts.Common;

public sealed record ApiErrorResponse(string Code, string Message, object? Details = null);
