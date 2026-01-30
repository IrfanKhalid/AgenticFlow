CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260130205626_InitialCreate') THEN
    CREATE TABLE "JobDetails" (
        "Id" uuid NOT NULL,
        "Title" character varying(1000) NOT NULL,
        "Location" character varying(500) NOT NULL,
        "Description" text,
        "Responsibilities" text,
        "Achievements" text,
        "Requirements" text,
        "Compensation" text NOT NULL,
        "ApplyUrl" text NOT NULL,
        "StartDate" date NOT NULL,
        "Active" boolean NOT NULL,
        CONSTRAINT "PK_JobDetails" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260130205626_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260130205626_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

