/*====================================================================
  District / Salesperson Management — seed data
  Target: Microsoft SQL Server / Azure SQL

  Re-runnable: it clears the four tables (child → parent) before
  re-inserting, so applying it twice leaves the same rows — no
  duplicate-key errors, no drift.

  IDENTITY_INSERT gives the rows stable, known ids so districts,
  stores and secondary links can reference each other by id and so the
  assertion test can name the edge cases it checks.

  Every §6.2 edge case is represented, and each is called out inline:
    · a salesperson who is PRIMARY of two districts        (BR-6)
    · a district with 0 secondaries and one with 3
    · a salesperson SECONDARY in one district, PRIMARY in another
    · a district with 0 stores
    · every salesperson assigned to at least one district  (BR-3)
    · ~5 districts / 8 salespersons / 15–25 stores
====================================================================*/

-- Clear in reverse dependency order so re-runs start clean.
DELETE FROM dbo.DistrictSecondarySalesperson;
DELETE FROM dbo.Store;
DELETE FROM dbo.District;
DELETE FROM dbo.Salesperson;
GO

-- 8 salespersons. Every one of them lands in ≥1 district below (BR-3).
SET IDENTITY_INSERT dbo.Salesperson ON;
INSERT INTO dbo.Salesperson (Id, Name) VALUES
    (1, 'Anna Bruun'),
    (2, 'Bjørn Dahl'),
    (3, 'Camilla Holm'),
    (4, 'David Krogh'),
    (5, 'Emil Lund'),
    (6, 'Freja Møller'),
    (7, 'Gustav Nielsen'),
    (8, 'Helle Overgaard');
SET IDENTITY_INSERT dbo.Salesperson OFF;
GO

-- 5 districts. Anna (1) is primary of TWO of them — the transition-period
-- case (BR-6): the same salesperson may head more than one district.
SET IDENTITY_INSERT dbo.District ON;
INSERT INTO dbo.District (Id, Name, PrimarySalespersonId) VALUES
    (1, 'North Denmark', 1),   -- Anna primary
    (2, 'South Denmark', 1),   -- Anna primary again (BR-6) — and 0 secondaries below
    (3, 'Copenhagen',    3),   -- Camilla primary — gets 3 secondaries below
    (4, 'Funen',         4),   -- David primary
    (5, 'Zealand',       5);   -- Emil primary — and 0 stores below
SET IDENTITY_INSERT dbo.District OFF;
GO

-- 17 stores across four districts. Zealand (5) deliberately has none
-- (the empty-district edge case). Range stays within the 15–25 target.
SET IDENTITY_INSERT dbo.Store ON;
INSERT INTO dbo.Store (Id, Name, DistrictId) VALUES
    (101, 'Aalborg',      1),
    (102, 'Frederikshavn',1),
    (103, 'Hjørring',     1),
    (104, 'Brønderslev',  1),
    (105, 'Esbjerg',      2),
    (106, 'Kolding',      2),
    (107, 'Vejle',        2),
    (108, 'Sønderborg',   2),
    (109, 'Nørrebro',     3),
    (110, 'Østerbro',     3),
    (111, 'Vesterbro',    3),
    (112, 'Amager',       3),
    (113, 'Valby',        3),
    (114, 'Odense',       4),
    (115, 'Svendborg',    4),
    (116, 'Nyborg',       4),
    (117, 'Middelfart',   4);
SET IDENTITY_INSERT dbo.Store OFF;
GO

-- Secondary links (0..n per district):
--   · Copenhagen (3) has THREE secondaries.
--   · South Denmark (2) and Zealand (5) have ZERO.
--   · Emil (5) is a SECONDARY of North Denmark yet PRIMARY of Zealand —
--     the dual-district case. (Never primary AND secondary of the SAME
--     district: that is BR-7, which the domain aggregate enforces.)
--   · Bjørn (2), Freja (6), Gustav (7) and Helle (8) hold no primary role,
--     so these links are what put them in a district (BR-3).
INSERT INTO dbo.DistrictSecondarySalesperson (DistrictId, SalespersonId) VALUES
    (1, 5),   -- Emil, secondary of North Denmark (primary of Zealand)
    (3, 2),   -- Bjørn,  secondary of Copenhagen
    (3, 6),   -- Freja,  secondary of Copenhagen
    (3, 7),   -- Gustav, secondary of Copenhagen
    (4, 8);   -- Helle,  secondary of Funen
GO
