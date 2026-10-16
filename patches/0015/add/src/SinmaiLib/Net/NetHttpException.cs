using System;
using System.Net;

namespace Net;

internal sealed class NetHttpException(
    NetErrorKind errorKind,
    string message,
    HttpStatusCode? httpStatus = null,
    Exception? innerException = null)
    : Exception(message, innerException)
{
    public NetErrorKind ErrorKind { get; } = errorKind;

    public HttpStatusCode? HttpStatus { get; } = httpStatus;
}
