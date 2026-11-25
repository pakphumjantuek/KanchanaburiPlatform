# Content ownership

Content has two separate relationships:

- `CreatedByUserId`: the account that wrote it.
- `OwnerShopId`: the publishing shop, only for `ContentType = Shop`.
- `ShopId`: the shop mentioned by a user, or the publishing shop for shop content. Mentioning a shop never grants management rights.

Personal publishing uses `POST /api/contents`. The server sets the type to `User`; linking any active shop is optional. Personal management uses `GET /api/contents/mine` and `GET /api/contents/mine/{id}`.

Shop publishing uses `POST /api/contents/shop`. The server resolves the signed-in account's shop and assigns both shop fields, ignoring a supplied shop ID. Shop management uses `GET /api/contents/shop` and `GET /api/contents/shop/{id}`.

Updates and archives continue to use `PUT /api/contents/{id}` and `DELETE /api/contents/{id}`. They preserve type and ownership. Shop articles cannot be reassigned through an update. Users manage their own personal content; current shop owners manage shop content; Admin can manage both. Schedules use the same ownership policy, including checking the destination content when moving a schedule.

Public requests support `contentType=User` or `contentType=Shop`. Shop management shows published user mentions in a separate read-only section. Public shop cards and article details identify the publishing type.

Migration `20261005064234_SeparateContentOwnership` keeps every existing article as `User`, preserves its writer and related shop, and leaves `OwnerShopId` null. This is intentional: historical shop links do not establish ownership. No legacy article is automatically converted to shop content.

Validation from the backend directory:

```powershell
dotnet run --project tests/ContentOwnershipChecks
dotnet build KanchanaburiPlatform.sln
```

The checks cover linked user articles, current shop ownership, Admin access, anonymous access, malformed ownership, personal drafts/archives, and separation of shop articles from public user mentions.
