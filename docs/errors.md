# Error reporting

The WOM&nbsp;Registry API uses "Problem Details for HTTP APIs" ([RFC&nbsp;7807](https://datatracker.ietf.org/doc/html/rfc7807)) for all error reporting.
In general, if the HTTP Status response is _not_ 200, it is expected that the Registry API will generate a Problem Details response (with Content-Type `application/json`).

## Sample responses

### Voucher redemption

```
POST https://wom.social/api/v1/voucher/redeem
```

When providing a message payload that cannot be decrypted with the Registry's public key, the response's status is 403:

```
{
  "type": "https://wom.social/api/problems/payload-verification-failure",
  "title": "Failed to verify request contents",
  "status": 403
}
```

### Payment processing

```
POST https://wom.social/api/v1/payment/confirm
```

When providing a message payload that cannot be decrypted with the Registry's public key, the response's status is 403:

```
{
  "type": "https://wom.social/api/problems/payload-verification-failure",
  "title": "Failed to verify request contents",
  "status": 403
}
```

## Table of problem types

All ‘type’ codes in the table below are preceded by the URL `https://wom.social/api/problems/`, that is the full “Wrong parameter” type will be `https://wom.social/api/problems/wrong-parameter`.

| Type | Status | Default title | Scenario | Additional parameters |
| --- | --- | --- | --- | --- |
| `wrong-parameter` | 422 | Request parameter not valid | Some parameter of the operation is not valid. | None. |
| `request-void` | 410 | Request instance is void | Voucher request has been performed or has been canceled. Payment request has been performed or has been canceled. | None. |
| `source-not-found` | 404 | Source with the specified ID does not exist | The instrument that has requested the voucher generation does not exist. | None. |
| `pos-not-found` | 404 | POS with the specified ID does not exist | The POS that has requested the payment does not exist. | None. |
| `payload-verification-failure` | 403 | Failed to verify request contents | The encrypted contents of the request do not match the unencrypted (i.e., the request is forged and cannot be accepted). | None. |
| `password-unacceptable` | 422 | Supplied password is not acceptable | The password supplied by the client is not acceptable (e.g., too short). | None. |
| `otc-not-valid` | 404 | OTC code does not exist | The supplied OTC code is invalid, was not generated correctly or was never verified by the creator. | None. |
| `operation-already-performed` | 400 | Operation already performed | The payment or voucher redemption operation has been already performed and cannot be repeated. | None. |
| `wrong-password` | 422 | Wrong password | The password supplied does not match. | None. |
| `wrong-number-of-vouchers` | 400 | Wrong number of vouchers supplied | Number of vouchers supplied as payment does not match required vouchers. | `required` (string), `supplied` (string). |
| `insufficient-valid-vouchers` | 400 | Insufficient valid vouchers supplied | Number of vouchers supplied as payment does not match required vouchers. | `required` (string), `supplied` (string). |
| `location-not-provided` | 400 | User location not provided | Voucher redemption requires user location. | None. |
