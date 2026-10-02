using Electronic.Application.Common.Constants;
using System.Net;

namespace Electronic.Application.Common.Responses
{
    public sealed class ApiResponse<T>
    {
        public int Code { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Result { get; init; }

        public static ApiResponse<T> Success(T? result, string message = Messages.Succes, HttpStatusCode code = HttpStatusCode.OK) =>
            new() { Code = (int)code, Message = message, Result = result };

        public static ApiResponse<T> Fail(HttpStatusCode code, string message) =>
            new() { Code = (int)code, Message = message, Result = default };
    }
}
