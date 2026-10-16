namespace Net;

internal enum NetErrorKind
{
    None,
    HttpStatus,
    Timeout,
    Canceled,
    Connection,
    Protocol,
    Internal,
}