/*====================================================================
  District / Salesperson Management — schema
  Target: Microsoft SQL Server / Azure SQL

  Versioned and re-runnable (drop-then-create). The schema grows one
  business rule at a time, each driven by a failing integration test.
====================================================================*/

-- Drop in reverse dependency order so the file is freely re-runnable.
IF OBJECT_ID('dbo.DistrictSecondarySalesperson', 'U') IS NOT NULL DROP TABLE dbo.DistrictSecondarySalesperson;
IF OBJECT_ID('dbo.Store',       'U') IS NOT NULL DROP TABLE dbo.Store;
IF OBJECT_ID('dbo.District',    'U') IS NOT NULL DROP TABLE dbo.District;
IF OBJECT_ID('dbo.Salesperson', 'U') IS NOT NULL DROP TABLE dbo.Salesperson;
GO

CREATE TABLE dbo.Salesperson
(
    Id    INT           IDENTITY(1,1) NOT NULL,
    Name  NVARCHAR(100)               NOT NULL,
    CONSTRAINT PK_Salesperson PRIMARY KEY (Id)
);
GO

CREATE TABLE dbo.District
(
    Id                    INT           IDENTITY(1,1) NOT NULL,
    Name                  NVARCHAR(100)               NOT NULL,   -- BR-1: every district has a name
    PrimarySalespersonId  INT                         NOT NULL,   -- BR-4: always exactly one primary
    RowVersion            ROWVERSION,                             -- optimistic-concurrency token

    CONSTRAINT PK_District               PRIMARY KEY (Id),
    CONSTRAINT UQ_District_Name          UNIQUE (Name),           -- names don't repeat
    CONSTRAINT CK_District_Name_NotBlank CHECK (LEN(Name) > 0),   -- BR-1: not an empty/blank name

    -- BR-4: the primary must be a real salesperson. No UNIQUE on this column, so the same
    -- salesperson may be primary of several districts (BR-6). No cascade: a salesperson who is a
    -- primary cannot be deleted out from under a district — the FK blocks it.
    CONSTRAINT FK_District_PrimarySalesperson
        FOREIGN KEY (PrimarySalespersonId) REFERENCES dbo.Salesperson (Id)
);
GO
CREATE INDEX IX_District_PrimarySalespersonId ON dbo.District (PrimarySalespersonId);
GO

CREATE TABLE dbo.Store
(
    Id          INT           IDENTITY(1,1) NOT NULL,
    Name        NVARCHAR(100)               NOT NULL,
    DistrictId  INT                         NOT NULL,      -- BR-2: a store must have a district

    CONSTRAINT PK_Store PRIMARY KEY (Id),
    CONSTRAINT FK_Store_District
        FOREIGN KEY (DistrictId) REFERENCES dbo.District (Id)
        ON DELETE CASCADE        -- delete a district -> its stores go too
);
GO
CREATE INDEX IX_Store_DistrictId ON dbo.Store (DistrictId);
GO

-- Only SECONDARY links live here (0..n per district). The primary is the FK column on District,
-- so there is no Role column and no "which role" ambiguity.
CREATE TABLE dbo.DistrictSecondarySalesperson
(
    DistrictId     INT NOT NULL,
    SalespersonId  INT NOT NULL,

    -- BR-7: composite PK => a person can't be listed twice as a secondary in the same district.
    CONSTRAINT PK_DistrictSecondarySalesperson PRIMARY KEY (DistrictId, SalespersonId),

    CONSTRAINT FK_DSS_District
        FOREIGN KEY (DistrictId) REFERENCES dbo.District (Id) ON DELETE CASCADE,
    CONSTRAINT FK_DSS_Salesperson
        FOREIGN KEY (SalespersonId) REFERENCES dbo.Salesperson (Id)
);
GO
CREATE INDEX IX_DSS_SalespersonId ON dbo.DistrictSecondarySalesperson (SalespersonId);
GO

/*  BR-7, cross-table half — a salesperson can't be BOTH primary and secondary of the SAME district.
    This compares District.PrimarySalespersonId against a DistrictSecondarySalesperson row for the
    same DistrictId; a CHECK constraint cannot reference another table, so it is enforced in the
    domain aggregate (add-secondary rejects the current primary; promoting a secondary drops the
    secondary row in the same write). The headline rule (BR-4) is a pure constraint here, which is
    what the brief asks for; this lesser consistency rule is owned by the single write path.  */
