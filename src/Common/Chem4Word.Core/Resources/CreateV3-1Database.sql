﻿--
-- File generated with SQLiteStudio v3.2.1 on Tue Nov 8 13:10:35 2022
--
-- This will create a database with a V3.1 schema. This will guarantee that all patches can be applied ...
--

-- Table: Gallery
CREATE TABLE Gallery (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, Chemistry BLOB NOT NULL, Name TEXT NOT NULL, Formula TEXT);

-- Table: ChemicalNames
CREATE TABLE ChemicalNames (ChemicalNameId INTEGER NOT NULL, Name TEXT NOT NULL, Namespace TEXT NOT NULL, Tag TEXT NOT NULL, ChemistryID INTEGER NOT NULL, FOREIGN KEY (ChemistryID) REFERENCES Gallery (Id), CONSTRAINT PK_ChemicalNames PRIMARY KEY (ChemicalNameId));

-- Table: ChemistryByTags
CREATE TABLE ChemistryByTags (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, GalleryId INTEGER NOT NULL, TagId INTEGER NOT NULL, FOREIGN KEY (GalleryId) REFERENCES Gallery (Id), FOREIGN KEY (TagId) REFERENCES UserTags (Id));

-- These will be deleted later on by the 3.2.1 patch, but must be seeded to prevent errors while patching

-- Table: UserTags
CREATE TABLE UserTags (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, UserTag TEXT NOT NULL UNIQUE, Lock INTEGER NOT NULL DEFAULT 0);

-- View: GetAllChemistryWithTags
CREATE VIEW GetAllChemistryWithTags AS SELECT Gallery.*, UserTags.UserTag FROM UserTags INNER JOIN (Gallery INNER JOIN ChemistryByTags ON Gallery.Id = ChemistryByTags.GalleryId) ON UserTags.ID = ChemistryByTags.TagId;

-- View: GetUserTags
CREATE VIEW GetUserTags AS SELECT UserTags.UserTag FROM UserTags;

-- View: UserTag
CREATE VIEW UserTag AS SELECT UserTags.UserTag FROM UserTags ORDER BY UserTags.Id;