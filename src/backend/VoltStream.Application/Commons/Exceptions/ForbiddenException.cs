namespace VoltStream.Application.Commons.Exceptions;

using System.Net;

[Serializable]
public class ForbiddenException(string? message) : AppException(message, HttpStatusCode.Forbidden);
