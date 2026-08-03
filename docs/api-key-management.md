# API Key Management

[繁體中文](api-key-management.zh-TW.md) · [← Docs Index](README.md)

An API key answers **which application is calling**. It is not user authentication — that stays with
the Bearer access token, and no key grants any data access on its own. The two travel together on
every remote call: `X-Api-Key` says *what* is calling, `Authorization: Bearer` says *who*.

> A key held by a client is not a secret in the cryptographic sense: it ships inside your desktop or
> mobile application and can be recovered from it. Treat it as an application identity that can be
> revoked, not as a password.

## 1. The gate turns itself on

A deployment that has never issued a key keeps the pre-gate behaviour — any non-empty `X-Api-Key`
passes — so upgrading the framework locks nobody out. **Issuing the first enabled key closes the
gate**, after which only issued keys are accepted. There is no setting to flip.

| State | Behaviour |
|---|---|
| `st_api_key` absent, or holds no enabled key | Gate not in force: any non-empty header passes. `UseBeeFramework` logs a startup warning. |
| At least one enabled key | Gate in force: the header must carry a valid, enabled, unexpired key. |

Rejections are deliberately merged into one outcome — malformed, unknown, disabled and expired are
indistinguishable to the caller, so the API cannot be used to probe which identifiers exist. The
reasons are separated in the audit record only.

## 2. Issuing a key

`SystemBO.CreateApiKey` generates the secret server-side and returns the complete plaintext key
**once**. Only a salted hash is stored, so the framework cannot show the key again — losing it means
issuing a replacement, which is the rotation procedure anyway.

```csharp
var response = await connector.CreateApiKeyAsync(
    sysId: "acme-portal",              // lowercase letters, digits and hyphens
    sysName: "ACME Customer Portal",
    keyType: ApiKeyType.ThirdParty,
    contact: "ops@acme.example",       // so an incident has someone to reach
    expiredAt: null);                  // or a UTC instant

// The only moment this value exists outside the server:
Console.WriteLine(response.ApiKey);    // "acme-portal.<secret>"
```

The identifier is the leading segment of the key itself and is **not** a secret — it appears in logs
and audit records by design, which is what makes "which application caused this" answerable.

## 3. Who may manage keys

A key belongs to the **installation**, not to any company, so company roles do not govern it. Key
management is gated on the deployment-level axis instead:

- **A remote caller must be a deployment administrator** (`st_user.deployment_admin`). Being merely
  signed in is not enough, and a company administrator gains nothing here.
- **An in-process (local) call passes without one.** That is the bootstrap path: a deployment with
  no administrator yet must still be able to mint its first key on the host.

See [Permission & Authorization, Part 3](permission-authorization.md) for the deployment-level model
and how to appoint the first administrator.

## 4. Rotating a key

Rotation never has a window where the application is locked out, because the old and new keys are
both live in the middle of it.

1. **Issue a replacement.** `sys_id` is unique, so the new key needs its own identifier — the
   convention is a suffix: `acme-portal` → `acme-portal-2`. Both keys are enabled and both work.
2. **Move the clients over.** Update the stored key on each installation (see §6). Traffic shifts
   gradually; nothing breaks while it does.
3. **Confirm the old key is idle.** The audit trail records `api_key_id` on every call, so a query
   over the login and change logs shows whether anything still presents the old identifier.
4. **Disable the old key.** `SetApiKeyEnabled(sysId, false)` revokes it **immediately** across every
   server process — the invalidation travels with the write, so no cache keeps it alive.

Instead of step 4 you can set an expiry (`SetApiKeyExpiry`) to schedule the retirement rather than
performing it. A past instant is accepted there, which retires a key as of now while leaving the
reason visible in the row.

> **On a deployment holding only one key, disabling it re-opens the gate** — with no enabled key
> left, the pre-gate behaviour returns and any non-empty header is accepted again. This is why
> rotation issues the replacement *first*. Never disable your way down to zero.

Keys are never deleted by the framework. A disabled row keeps `api_key_id` in the audit trail
resolvable; deleting it would leave historical log rows pointing at nothing.

## 5. Third-party integrations

Issue a **separate key per third party** and set `KeyType = ApiKeyType.ThirdParty` with a `Contact`.
The type is a label with no authorization meaning, but together with the contact it is what makes an
incident actionable — you can tell whose key is misbehaving and whom to call.

Give third-party keys an expiry. An integration that goes quiet is otherwise indistinguishable from
one that is still live, and a key that outlives the relationship is the one nobody remembers to
revoke.

## 6. Where clients keep their key

Clients read and persist their key through `IApiKeyStorage` (`Bee.UI.Core`), assigned as
`ClientInfo.ApiKeyStorage`. `ClientInfo.ApplyApiKey(defaultApiKey)` seeds empty storage with the
application's built-in value and otherwise uses what is stored, so changing a key never requires
recompiling a client. Platform-appropriate implementations ship for file-backed and browser-backed
hosts; see the `Bee.UI.Core` and `Bee.UI.Avalonia` READMEs.

## 7. What gets recorded

Every audit row carries `api_key_id` and `api_key_name`, so "which application did this" is
answerable on the login, change, access and API-anomaly axes without a join.

Key management operations are themselves recorded, on the change axis under the `System` prog id,
marked sensitive, with before/after values — an issued key logs its identifier, name, type, contact
and expiry, and **never the secret or its hash**.

## See also

- [Permission & Authorization](permission-authorization.md) — the deployment-level authorization model
- [API Method Reference](api-method-reference.md) — the key management methods and their protection levels
- [Framework-Reserved Names](framework-reserved-names.md) — `st_api_key` and the other framework tables
