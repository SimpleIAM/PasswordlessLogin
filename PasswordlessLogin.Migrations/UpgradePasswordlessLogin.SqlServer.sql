IF OBJECT_ID(N'[auth].[__PasswordlessMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'auth') IS NULL EXEC(N'CREATE SCHEMA [auth];');
    CREATE TABLE [auth].[__PasswordlessMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___PasswordlessMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20180815171813_v0.3.0')
BEGIN
    IF SCHEMA_ID(N'auth') IS NULL EXEC(N'CREATE SCHEMA [auth];');
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20180815171813_v0.3.0')
BEGIN
    CREATE TABLE [auth].[AuthorizedDevices] (
        [Id] int NOT NULL IDENTITY,
        [SubjectId] nvarchar(36) NOT NULL,
        [DeviceIdHash] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [AddedOn] datetime2 NOT NULL,
        CONSTRAINT [PK_AuthorizedDevices] PRIMARY KEY ([Id])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20180815171813_v0.3.0')
BEGIN
    CREATE TABLE [auth].[OneTimeCodes] (
        [SentTo] nvarchar(254) NOT NULL,
        [ClientNonceHash] nvarchar(max) NULL,
        [ShortCode] nvarchar(max) NULL,
        [LongCode] nvarchar(max) NULL,
        [ExpiresUTC] datetime2 NOT NULL,
        [FailedAttemptCount] int NOT NULL,
        [SentCount] int NOT NULL,
        [RedirectUrl] nvarchar(2048) NULL,
        CONSTRAINT [PK_OneTimeCodes] PRIMARY KEY ([SentTo])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20180815171813_v0.3.0')
BEGIN
    CREATE TABLE [auth].[PasswordHashes] (
        [SubjectId] nvarchar(36) NOT NULL,
        [Hash] nvarchar(max) NOT NULL,
        [LastChangedUTC] datetime2 NOT NULL,
        [FailedAttemptCount] int NOT NULL,
        [TempLockUntilUTC] datetime2 NULL,
        CONSTRAINT [PK_PasswordHashes] PRIMARY KEY ([SubjectId])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20180815171813_v0.3.0')
BEGIN
    CREATE TABLE [auth].[Users] (
        [SubjectId] nvarchar(36) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([SubjectId])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20180815171813_v0.3.0')
BEGIN
    CREATE TABLE [auth].[Claims] (
        [Id] int NOT NULL IDENTITY,
        [SubjectId] nvarchar(36) NOT NULL,
        [Type] nvarchar(255) NOT NULL,
        [Value] nvarchar(4000) NOT NULL,
        CONSTRAINT [PK_Claims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Claims_Users_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [auth].[Users] ([SubjectId]) ON DELETE CASCADE
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20180815171813_v0.3.0')
BEGIN
    CREATE INDEX [IX_Claims_SubjectId] ON [auth].[Claims] ([SubjectId]);
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20180815171813_v0.3.0')
BEGIN
    CREATE INDEX [IX_Claims_Type] ON [auth].[Claims] ([Type]);
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20180815171813_v0.3.0')
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [auth].[Users] ([Email]);
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20180815171813_v0.3.0')
BEGIN
    INSERT INTO [auth].[__PasswordlessMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20180815171813_v0.3.0', N'2.1.4-rtm-31024');
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20181214010145_v0.4.0')
BEGIN
    CREATE TABLE [auth].[EventLog] (
        [Id] bigint NOT NULL IDENTITY,
        [Time] datetime2 NOT NULL,
        [Username] nvarchar(254) NOT NULL,
        [EventType] nvarchar(30) NOT NULL,
        [Details] nvarchar(255) NULL,
        CONSTRAINT [PK_EventLog] PRIMARY KEY ([Id])
    );
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20181214010145_v0.4.0')
BEGIN
    INSERT INTO [auth].[__PasswordlessMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20181214010145_v0.4.0', N'2.1.4-rtm-31024');
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20190424185144_v0.5.0')
BEGIN
    ALTER TABLE [auth].[Users] ADD [CreatedUTC] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20190424185144_v0.5.0')
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[auth].[EventLog]') AND [c].[name] = N'Username');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [auth].[EventLog] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [auth].[EventLog] ALTER COLUMN [Username] nvarchar(254) NULL;
END;

GO

IF NOT EXISTS(SELECT * FROM [auth].[__PasswordlessMigrationsHistory] WHERE [MigrationId] = N'20190424185144_v0.5.0')
BEGIN
    INSERT INTO [auth].[__PasswordlessMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20190424185144_v0.5.0', N'2.1.4-rtm-31024');
END;

GO

