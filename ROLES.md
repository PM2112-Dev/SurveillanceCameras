# Huong dan su dung Roles trong SurveillanceCameras

Tai lieu nay mo ta ro: dung roles o file nao, ham nao, va dung nhu the nao theo tung truong hop.

## 1) Tong quan luong phan quyen hien tai

- **Khai bao role**: `src/Domain/Constants/Roles.cs`
- **Dang ky auth + identity**: `src/Infrastructure/DependencyInjection.cs` trong ham `AddInfrastructureServices(...)`
- **Gan role khi seed du lieu**: `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs` trong ham `TrySeedAsync()`
- **Bat auth pipeline MediatR**: `src/Application/DependencyInjection.cs` trong ham `AddApplicationServices(...)` (co `AuthorizationBehaviour<,>`)
- **Check role/policy thuc te**: `src/Application/Common/Behaviours/AuthorizationBehaviour.cs` trong ham `Handle(...)`
- **Gan auth cho endpoint**: cac file trong `src/Web/Endpoints/*.cs` bang `RequireAuthorization(...)`
- **Gan auth cho use case**: tren `Command/Query` bang `[Authorize(...)]` (custom attribute tai `src/Application/Common/Security/AuthorizeAttribute.cs`)

## 2) Dung roles o dau la dung?

Nen dung **2 lop** de an toan:

1. **Web Endpoint layer**: chan som request HTTP
2. **Application layer (MediatR request)**: chan o business boundary, tranh bypass

Neu chi dung endpoint ma bo qua application, co the bi bo sot khi request duoc goi tu flow khac.

## 3) Cac truong hop su dung

### 3.1 Chi can dang nhap (khong can role cu the)

**Endpoint:**

```csharp
groupBuilder.RequireAuthorization();
```

**Application request:**

```csharp
[Authorize]
public record GetSomethingQuery : IRequest<SomethingVm>;
```

### 3.2 Chi 1 role (vi du: Administrator)

**Application request:**

```csharp
using SurveillanceCameras.Application.Common.Security;
using SurveillanceCameras.Domain.Constants;

[Authorize(Roles = Roles.Administrator)]
public record CreateAreaCommand : IRequest<int>;
```

**Endpoint:**

```csharp
using Microsoft.AspNetCore.Authorization;
using SurveillanceCameras.Domain.Constants;

groupBuilder.MapPost(CreateArea)
    .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Administrator });
```

### 3.3 Cho phep 1 trong nhieu role (OR)

```csharp
[Authorize(Roles = $"{Roles.Administrator},{Roles.Manager}")]
public record ApproveAreaCommand(int Id) : IRequest;
```

Trong code hien tai (`AuthorizationBehaviour`), danh sach role cach nhau bang dau phay duoc hieu la **OR** (co mot role hop le la qua).

### 3.4 Can dong thoi nhieu dieu kien (AND)

Khi can logic chat hon (vi du role + quy tac nghiep vu), dung **Policy**:

1. Dang ky policy trong `AddInfrastructureServices(...)` (`src/Infrastructure/DependencyInjection.cs`)
2. Su dung tren request:

```csharp
[Authorize(Policy = "Area.AdminOnly")]
public record DeleteAreaCommand(int Id) : IRequest;
```

`AuthorizationBehaviour` se goi `IIdentityService.AuthorizeAsync(userId, policy)` de check policy.

## 4) File/Ham can sua theo tung viec

### A. Them role moi

- File: `src/Domain/Constants/Roles.cs`
- Viec: them constant moi, vi du `public const string Supervisor = nameof(Supervisor);`

### B. Tao role + gan role cho user (seed)

- File: `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs`
- Ham: `TrySeedAsync()`
- Viec: tao `IdentityRole`, sau do `_userManager.AddToRolesAsync(...)`

### C. Bat auth va policy

- File: `src/Infrastructure/DependencyInjection.cs`
- Ham: `AddInfrastructureServices(...)`
- Viec:
    - Da co `AddAuthentication(...).AddJwtBearer(...)`
    - Da co `AddAuthorizationBuilder()`
    - Neu can policy custom thi add vao day

### D. Bao ve endpoint

- File: `src/Web/Endpoints/<Feature>.cs`
- Ham: `Map(RouteGroupBuilder groupBuilder)`
- Viec: dung `RequireAuthorization()` / `RequireAuthorization(new AuthorizeAttribute { Roles = ... })`

### E. Bao ve use case

- File: `src/Application/.../Commands/...` hoac `src/Application/.../Queries/...`
- Vi tri: tren class/record `Command`/`Query`
- Viec: them `[Authorize]`, `[Authorize(Roles = ...)]`, hoac `[Authorize(Policy = ...)]`

## 5) Vi du thuc te trong repo

- `src/Application/Service/Areas/Commands/CreateArea/CreateArea.cs`
    - Dang dung: `[Authorize(Roles = Roles.Administrator)]`
- `src/Web/Endpoints/TodoLists.cs`, `src/Web/Endpoints/TodoItems.cs`, `src/Web/Endpoints/WeatherForecasts.cs`
    - Dang dung: `groupBuilder.RequireAuthorization();`

## 6) Luu y de tranh loi phan quyen

- Uu tien dung constant trong `Roles.cs`, khong hard-code chuoi role.
- Voi role list, tach boi dau phay: `"Administrator,Manager"`.
- Nen co test cho 3 case: anonymous (401), user khong du role (403), user dung role (200/204).
- Validator (FluentValidation) va Authorization la 2 lop khac nhau: validator khong thay the phan quyen.

## 7) Mau nhanh copy-paste

```csharp
// Application request
[Authorize(Roles = $"{Roles.Administrator},{Roles.Manager}")]
public record UpdateAreaStatusCommand(int Id) : IRequest;

// Endpoint
groupBuilder.MapPatch(UpdateAreaStatus, "{id}/status")
    .RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute
    {
        Roles = $"{Roles.Administrator},{Roles.Manager}"
    });
```

