-- This script may need to be run manually in SQLite Manager to fix these tables

-- ChemicalNames
ALTER TABLE ChemicalNames RENAME TO ChemicalNames_Temp;
CREATE TABLE ChemicalNames (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Namespace TEXT NOT NULL, Tag TEXT NOT NULL, ChemistryID INTEGER NOT NULL, FOREIGN KEY (ChemistryID) REFERENCES Gallery (Id), CONSTRAINT PK_ChemicalNames);
INSERT INTO ChemicalNames (Name, Namespace, Tag, ChemistryID) SELECT Name, Namespace, Tag, ChemistryID FROM ChemicalNames_Temp;
DROP TABLE ChemicalNames_Temp;

-- ChemicalFormulae
ALTER TABLE ChemicalFormulae RENAME TO ChemicalFormulae_Temp;
CREATE TABLE ChemicalFormulae (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Namespace TEXT NOT NULL, Tag TEXT NOT NULL, ChemistryID INTEGER NOT NULL, FOREIGN KEY (ChemistryID) REFERENCES Gallery (Id), CONSTRAINT PK_ChemicalFormulae);
INSERT INTO ChemicalFormulae (Name, Namespace, Tag, ChemistryID) SELECT Name, Namespace, Tag, ChemistryID FROM ChemicalFormulae_Temp;
DROP TABLE ChemicalFormulae_Temp;

-- ChemicalCaptions
ALTER TABLE ChemicalCaptions RENAME TO ChemicalCaptions_Temp;
CREATE TABLE ChemicalCaptions (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Namespace TEXT NOT NULL, Tag TEXT NOT NULL, ChemistryID INTEGER NOT NULL, FOREIGN KEY (ChemistryID) REFERENCES Gallery (Id), CONSTRAINT PK_ChemicalCaptions);
INSERT INTO ChemicalCaptions (Name, Namespace, Tag, ChemistryID) SELECT Name, Namespace, Tag, ChemistryID FROM ChemicalCaptions_Temp;
DROP TABLE ChemicalCaptions_Temp;

