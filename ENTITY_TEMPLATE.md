# Data Model Template

**Project:** [Project Name]
**Version:** 1.0
**Date:** [Date]

---

## 1. Entity List

| Entity | Description | Type | Count (Est.) |
|--------|-------------|------|--------------|
| [EntityName] | [Mô tả ngắn] | [Core/Lookup/Link] | [X records] |
| Story | [Mô tả] | Core | [X records] |
| Chapter | [Mô tả] | Core | [X records] |
| Account | [Mô tả] | Lookup | [X records] |

**Entity Types:**
- **Core**: Business entities chính
- **Lookup**: Bảng tham chiếu, ít thay đổi
- **Link**: Bảng trung gian cho many-to-many

---

## 2. Entity Details

### 2.1 [EntityName]

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           [EntityName]                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │
│  │ Id (PK)        │  │ CreatedAt       │  │ UpdatedAt       │          │
│  │ int            │  │ DateTime        │  │ DateTime?       │          │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘          │
│                                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │
│  │ [Field1]       │  │ [Field2]        │  │ [Field3]        │          │
│  │ [Type]         │  │ [Type]          │  │ [Type]          │          │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Field Details:**

| Field | Type | Nullable | Default | Description | Validation |
|-------|------|----------|---------|-------------|------------|
| Id | int | No | Auto | Primary key | Required |
| [Field1] | string | No | - | [Mô tả] | Max 100 chars |
| [Field2] | int? | Yes | null | [Mô tả] | FK → [Entity] |
| [Field3] | bool | No | false | [Mô tả] | - |
| CreatedAt | DateTime | No | DateTime.UtcNow | Timestamp | - |
| UpdatedAt | DateTime? | Yes | null | Last update | - |

**Code Example:**

```csharp
public class [EntityName] : BaseAuditableEntity
{
    public int Id { get; set; }

    public string [Field1] { get; set; } = string.Empty;

    public int? [ForeignKey]Id { get; set; }
    public [ForeignEntity]? [ForeignKey] { get; set; }

    public bool [Field3] { get; set; }

    // Navigation properties
    public virtual ICollection<[RelatedEntity]> [RelatedEntities] { get; set; } = new List<[RelatedEntity]>();
}
```

---

### 2.2 Story (Template)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                 Story                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────┐ ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌─────────────┐     │
│  │Id (PK) │ │ Title    │ │ Author   │ │ImageUrl   │ │TotalChapter│     │
│  │int      │ │string(500)│ │string(200)│ │string(500)│ │s (int)     │     │
│  └─────────┘ └──────────┘ └──────────┘ └───────────┘ └─────────────┘     │
│                                                                             │
│  ┌──────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────────┐      │
│  │Description   │ │ Genres     │ │IsScraped  │ │IsEdited       │      │
│  │string?      │ │string?    │ │bool       │ │bool           │      │
│  └──────────────┘ └────────────┘ └────────────┘ └────────────────┘      │
│                                                                             │
│  ┌────────────┐ ┌──────────────┐ ┌───────────────┐ ┌─────────────┐      │
│  │ Publish    │ │TytAccountId │ │TytStoryId    │ │CreatedAt   │      │
│  │enum       │ │(FK)         │ │string?       │ │DateTime    │      │
│  └────────────┘ └──────────────┘ └───────────────┘ └─────────────┘      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Fields:**

| Field | Type | Nullable | Default | Description |
|-------|------|----------|---------|-------------|
| Id | int | No | Auto | Primary key |
| Title | string | No | - | Tên truyện |
| Author | string | Yes | null | Tác giả |
| ImageUrl | string | Yes | null | URL cover image |
| TotalChapters | int | No | 0 | Tổng số chapters |
| Description | string | Yes | null | Mô tả truyện |
| Genres | string | Yes | null | JSON array of genres |
| IsScraped | bool | No | false | Đã crawl info |
| IsEdited | bool | No | false | Đã edit content |
| Publish | Publish | No | Private | Trạng thái publish |
| TytAccountId | int? | Yes | null | FK → Account |
| TytStoryId | string | Yes | null | Story ID trên TYT |
| CreatedAt | DateTime | No | Now | Ngày tạo |
| UpdatedAt | DateTime? | Yes | null | Ngày update |

---

## 3. Entity Relationships

### 3.1 Relationship Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ENTITY RELATIONSHIP DIAGRAM                          │
└─────────────────────────────────────────────────────────────────────────────┘

                                    ┌─────────────┐
                                    │   Account   │
                                    │   (TYT/YT)  │
                                    └──────┬──────┘
                                           │
                                           │ 1:N (optional)
                                           │
                                           ▼
    ┌─────────────┐                ┌─────────────┐                ┌─────────────┐
    │   User      │                │    Story     │                │   Chapter   │
    │             │                │             │                │             │
    │ 1        N  │                │ 1        N  │                │             │
    └─────────────┘                └──────┬──────┘                └──────┬──────┘
                                          │                              │
                                          │ 1:N                           │ N:1
                                          │ (required)                   │
                                          ▼                              ▼
                                   ┌─────────────┐                ┌─────────────┐
                                   │   Account   │                │    Story    │
                                   │   (TYT)     │                │             │
                                   └─────────────┘                └─────────────┘
```

### 3.2 Relationship Details

| # | From Entity | Relationship | To Entity | Cardinality | FK Location | Description |
|---|-------------|--------------|-----------|-------------|-------------|-------------|
| 1 | Story | → | Chapter | 1:N | Chapter.StoryId | 1 Story có N Chapters |
| 2 | Story | → | Account | N:1 | Story.TytAccountId | N Story dùng 1 Account |
| 3 | User | → | Story | 1:N | Story.CreatedBy | 1 User tạo N Stories |
| 4 | Story | → | Account (YT) | N:1 | Story.YoutubeAccountId | N Story dùng 1 YT Account |

### 3.3 Relationship Cards

#### Relationship #1: Story → Chapter

| Attribute | Value |
|-----------|-------|
| Type | One-to-Many |
| Parent | Story |
| Child | Chapter |
| FK Field | Chapter.StoryId |
| On Delete | Cascade |
| On Update | No Action |

```
┌─────────┐ 1          N ┌─────────┐
│  Story  │─────────────▶│ Chapter │
└─────────┘              └─────────┘
     │                        ▲
     │                        │ StoryId (FK)
     │                        │
     └────────────────────────┘
```

#### Relationship #2: Story → Account (TYT)

| Attribute | Value |
|-----------|-------|
| Type | Many-to-One |
| Parent | Account |
| Child | Story |
| FK Field | Story.TytAccountId |
| On Delete | Restrict |
| On Update | No Action |

```
┌─────────┐ N          1 ┌─────────┐
│  Story  │─────────────▶│ Account │
└─────────┘              └─────────┘
     │                        │
     │ TytAccountId (FK)     │
     │                        │
     │◀────────────────────────┘
```

---

## 4. Database Schema (SQL)

### 4.1 Table Definitions

```sql
-- Stories table
CREATE TABLE [Stories] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Title] nvarchar(500) NOT NULL,
    [Author] nvarchar(200) NULL,
    [ImageUrl] nvarchar(500) NULL,
    [TotalChapters] int NOT NULL DEFAULT(0),
    [Description] nvarchar(max) NULL,
    [Genres] nvarchar(max) NULL,
    [IsScraped] bit NOT NULL DEFAULT(0),
    [IsEdited] bit NOT NULL DEFAULT(0),
    [Publish] int NOT NULL DEFAULT(0),
    [TytAccountId] int NULL,
    [TytStoryId] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,

    CONSTRAINT [PK_Stories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Stories_Accounts_TytAccountId]
        FOREIGN KEY ([TytAccountId]) REFERENCES [Accounts] ([Id])
        ON DELETE NO ACTION
);

-- Chapters table
CREATE TABLE [Chapters] (
    [Id] int NOT NULL IDENTITY(1,1),
    [StoryId] int NOT NULL,
    [ChapterNumber] int NOT NULL,
    [NameRaw] nvarchar(200) NULL,
    [NameEdit] nvarchar(200) NULL,
    [ContentRaw] nvarchar(max) NULL,
    [ContentEdit] nvarchar(max) NULL,
    [IsUploaded] bit NOT NULL DEFAULT(0),
    [ChapterTytId] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,

    CONSTRAINT [PK_Chapters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Chapters_Stories_StoryId]
        FOREIGN KEY ([StoryId]) REFERENCES [Stories] ([Id])
        ON DELETE CASCADE
);

-- Accounts table
CREATE TABLE [Accounts] (
    [Id] int NOT NULL IDENTITY(1,1),
    [AccountType] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Cookie] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT(1),
    [ExpiredAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,

    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id])
);
```

### 4.2 Indexes

```sql
-- Indexes for Stories
CREATE INDEX [IX_Stories_TytAccountId] ON [Stories] ([TytAccountId]);
CREATE INDEX [IX_Stories_Publish] ON [Stories] ([Publish]);
CREATE INDEX [IX_Stories_CreatedAt] ON [Stories] ([CreatedAt]);

-- Indexes for Chapters
CREATE INDEX [IX_Chapters_StoryId] ON [Chapters] ([StoryId]);
CREATE INDEX [IX_Chapters_StoryId_ChapterNumber]
    ON [Chapters] ([StoryId], [ChapterNumber]);
```

---

## 5. EF Core Configuration

### 5.1 Story Configuration

```csharp
public class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        builder.ToTable("Stories");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.Author)
            .HasMaxLength(200);

        builder.Property(s => s.ImageUrl)
            .HasMaxLength(500);

        builder.Property(s => s.TotalChapters)
            .HasDefaultValue(0);

        builder.Property(s => s.Publish)
            .HasConversion<int>();

        // Relationships
        builder.HasOne(s => s.TytAccount)
            .WithMany()
            .HasForeignKey(s => s.TytAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Chapters)
            .WithOne(c => c.Story)
            .HasForeignKey(c => c.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.TytAccountId);
        builder.HasIndex(s => s.Publish);
        builder.HasIndex(s => s.CreatedAt);
    }
}
```

### 5.2 Chapter Configuration

```csharp
public class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> builder)
    {
        builder.ToTable("Chapters");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NameRaw)
            .HasMaxLength(200);

        builder.Property(c => c.ChapterNumber)
            .IsRequired();

        builder.HasIndex(c => c.StoryId);
        builder.HasIndex(c => new { c.StoryId, c.ChapterNumber }).IsUnique();
    }
}
```

---

## 6. Enums

```csharp
public enum Publish
{
    Private = 0,
    Public = 1
}

public enum AccountType
{
    Tyt = 1,
    YouTube = 2
}

public enum ProcessingStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public enum ChapterStatus
{
    Raw = 0,
    Translated = 1,
    Uploaded = 2
}
```

---

## 7. Naming Conventions

### 7.1 Table Naming
- **Singular**: `Story` not `Stories`
- **PascalCase**: `UserAccount` not `user_account`

### 7.2 Column Naming
- **PascalCase**: `CreatedAt` not `created_at`
- **Primary Key**: `Id`
- **Foreign Key**: `[EntityName]Id` → `StoryId`, `AccountId`
- **Boolean**: Prefix `Is`, `Has`, `Can` → `IsActive`, `HasPermission`

### 7.3 Navigation Properties
- **Single**: Tên entity gốc → `Story`, `Account`
- **Collection**: Số nhiều → `Stories`, `Chapters`

---

## Appendix: Quick Reference

### Common Relationships

| Relationship | C# Code | SQL |
|--------------|---------|-----|
| 1:N Required | `.WithMany().HasForeignKey()` | FK NOT NULL |
| 1:N Optional | `.WithMany().HasForeignKey().IsRequired(false)` | FK NULL |
| 1:1 | `.WithOne().HasForeignKey()` | Unique FK |
| N:N | `[ForeignTable]` + `[JoinTable]` | Junction table |

### Cascade Delete

| Behavior | When Parent Deleted | Use Case |
|----------|---------------------|----------|
| Cascade | Delete children | Story → Chapters |
| Restrict | Prevent delete | Story → Account |
| SetNull | Set FK to null | Rare |
| SetDefault | Set FK to default | Rare |

---

*Template End*
