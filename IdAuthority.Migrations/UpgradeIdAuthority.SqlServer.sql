IF OBJECT_ID(N'__EFMigrationsHistory') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20180420155726_Initial')
BEGIN
    CREATE TABLE [OneTimePasswords] (
        [Email] nvarchar(254) NOT NULL,
        [ExpiresUTC] datetime2 NOT NULL,
        [LinkCode] nvarchar(36) NULL,
        [OTP] nvarchar(8) NULL,
        [RedirectUrl] nvarchar(2048) NULL,
        CONSTRAINT [PK_OneTimePasswords] PRIMARY KEY ([Email])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20180420155726_Initial')
BEGIN
    CREATE TABLE [PasswordHashes] (
        [SubjectId] nvarchar(36) NOT NULL,
        [FailedAuthenticationCount] int NOT NULL,
        [Hash] nvarchar(max) NOT NULL,
        [LastChangedUTC] datetime2 NOT NULL,
        [TempLockUntilUTC] datetime2 NOT NULL,
        CONSTRAINT [PK_PasswordHashes] PRIMARY KEY ([SubjectId])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20180420155726_Initial')
BEGIN
    CREATE TABLE [Subjects] (
        [SubjectId] nvarchar(36) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        CONSTRAINT [PK_Subjects] PRIMARY KEY ([SubjectId])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20180420155726_Initial')
BEGIN
    CREATE UNIQUE INDEX [IX_Subjects_Email] ON [Subjects] ([Email]);
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20180420155726_Initial')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20180420155726_Initial', N'2.0.2-rtm-10011');
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20180430161327_RenameOTP')
BEGIN
    DROP TABLE [OneTimePasswords];
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20180430161327_RenameOTP')
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'PasswordHashes') AND [c].[name] = N'TempLockUntilUTC');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [PasswordHashes] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [PasswordHashes] ALTER COLUMN [TempLockUntilUTC] datetime2 NULL;
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20180430161327_RenameOTP')
BEGIN
    CREATE TABLE [OneTimeCodes] (
        [Email] nvarchar(254) NOT NULL,
        [ExpiresUTC] datetime2 NOT NULL,
        [LinkCode] nvarchar(36) NULL,
        [OTC] nvarchar(8) NULL,
        [RedirectUrl] nvarchar(2048) NULL,
        CONSTRAINT [PK_OneTimeCodes] PRIMARY KEY ([Email])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20180430161327_RenameOTP')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20180430161327_RenameOTP', N'2.0.2-rtm-10011');
END;

GO

