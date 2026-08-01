/*====================================================================
  District / Salesperson Management — schema
  Target: Microsoft SQL Server / Azure SQL

  Versioned and re-runnable (drop-then-create). The schema grows one
  business rule at a time, each driven by a failing integration test.
====================================================================*/

-- Drop in reverse dependency order so the file is freely re-runnable.
IF OBJECT_ID('dbo.Store',    'U') IS NOT NULL DROP TABLE dbo.Store;
IF OBJECT_ID('dbo.District', 'U') IS NOT NULL DROP TABLE dbo.District;
GO

CREATE TABLE dbo.District
(
    Id    INT           IDENTITY(1,1) NOT NULL,
    Name  NVARCHAR(100)               NOT NULL,           -- BR-1: every district has a name

    CONSTRAINT PK_District           PRIMARY KEY (Id),
    CONSTRAINT UQ_District_Name       UNIQUE (Name),       -- names don't repeat
    CONSTRAINT CK_District_Name_NotBlank CHECK (LEN(Name) > 0)  -- BR-1: not an empty/blank name
);
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
