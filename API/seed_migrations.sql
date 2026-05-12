-- Clear old stale migration history entries
DELETE FROM "__EFMigrationsHistory";

-- Mark InitialCreate as applied (5 tables already exist in DB, owned by postgres)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260505174436_InitialCreate', '9.0.4');

SELECT * FROM "__EFMigrationsHistory";
