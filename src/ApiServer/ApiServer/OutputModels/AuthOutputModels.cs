using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.OutputModels {

    /// <summary>
    /// Full POS login information.
    /// </summary>
    public record AuthPosLoginInfo(
        string Id,
        string Name,
        string Url,
        string PrivateKey,
        string PublicKey
    );

    /// <summary>
    /// Full Merchant login information.
    /// </summary>
    public record AuthMerchantLoginInfo(
        string Id,
        string Name,
        string FiscalCode,
        string Address,
        string ZipCode,
        string City,
        string Country,
        string Url,
        AuthPosLoginInfo[] Pos
    );

    /// <summary>
    /// Full Source login information.
    /// </summary>
    public record AuthSourceLoginInfo(
        string Id,
        string Name,
        string Url,
        string PrivateKey,
        string PublicKey
    );

}
