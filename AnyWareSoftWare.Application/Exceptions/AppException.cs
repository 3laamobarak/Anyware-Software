using System;

namespace AnyWareSoftWare.Application.Exceptions
{
    public class AppException : Exception
    {
        public int StatusCode { get; }

        public AppException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(404, message) { }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(409, message) { }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message) : base(401, message) { }
    }
}
