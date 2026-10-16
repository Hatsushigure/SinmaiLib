using System.Net;

namespace Net;

internal sealed record NetHttpResponse(
    HttpStatusCode HttpStatus,
    byte[] ResponseData,
    CookieCollection Cookie
);
