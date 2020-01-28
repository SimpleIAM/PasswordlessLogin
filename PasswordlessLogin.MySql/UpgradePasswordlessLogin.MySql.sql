CREATE TABLE IF NOT EXISTS `__PasswordlessMigrationsHistory` (
    `MigrationId` varchar(95) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    CONSTRAINT `PK___PasswordlessMigrationsHistory` PRIMARY KEY (`MigrationId`)
);


DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__PasswordlessMigrationsHistory` WHERE `MigrationId` = '20200128051726_v0.6.0') THEN

    CREATE TABLE `EventLog` (
        `Id` bigint NOT NULL AUTO_INCREMENT,
        `Time` datetime(6) NOT NULL,
        `Username` varchar(254) CHARACTER SET utf8mb4 NULL,
        `EventType` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `Details` varchar(255) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_EventLog` PRIMARY KEY (`Id`)
    );

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;


DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__PasswordlessMigrationsHistory` WHERE `MigrationId` = '20200128051726_v0.6.0') THEN

    CREATE TABLE `OneTimeCodes` (
        `SentTo` varchar(254) CHARACTER SET utf8mb4 NOT NULL,
        `ClientNonceHash` longtext CHARACTER SET utf8mb4 NULL,
        `ShortCode` longtext CHARACTER SET utf8mb4 NULL,
        `LongCode` longtext CHARACTER SET utf8mb4 NULL,
        `ExpiresUTC` datetime(6) NOT NULL,
        `FailedAttemptCount` int NOT NULL,
        `SentCount` int NOT NULL,
        `RedirectUrl` longtext CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_OneTimeCodes` PRIMARY KEY (`SentTo`)
    );

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;


DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__PasswordlessMigrationsHistory` WHERE `MigrationId` = '20200128051726_v0.6.0') THEN

    CREATE TABLE `PasswordHashes` (
        `SubjectId` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `Hash` longtext CHARACTER SET utf8mb4 NOT NULL,
        `LastChangedUTC` datetime(6) NOT NULL,
        `FailedAttemptCount` int NOT NULL,
        `TempLockUntilUTC` datetime(6) NULL,
        CONSTRAINT `PK_PasswordHashes` PRIMARY KEY (`SubjectId`)
    );

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;


DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__PasswordlessMigrationsHistory` WHERE `MigrationId` = '20200128051726_v0.6.0') THEN

    CREATE TABLE `TrustedBrowsers` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SubjectId` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `BrowserIdHash` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Description` longtext CHARACTER SET utf8mb4 NULL,
        `AddedOn` datetime(6) NOT NULL,
        CONSTRAINT `PK_TrustedBrowsers` PRIMARY KEY (`Id`)
    );

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;


DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__PasswordlessMigrationsHistory` WHERE `MigrationId` = '20200128051726_v0.6.0') THEN

    CREATE TABLE `Users` (
        `SubjectId` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(254) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedUTC` datetime(6) NOT NULL,
        CONSTRAINT `PK_Users` PRIMARY KEY (`SubjectId`)
    );

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;


DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__PasswordlessMigrationsHistory` WHERE `MigrationId` = '20200128051726_v0.6.0') THEN

    CREATE TABLE `Claims` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SubjectId` varchar(36) CHARACTER SET utf8mb4 NOT NULL,
        `Type` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Value` longtext CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_Claims` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Claims_Users_SubjectId` FOREIGN KEY (`SubjectId`) REFERENCES `Users` (`SubjectId`) ON DELETE CASCADE
    );

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;


DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__PasswordlessMigrationsHistory` WHERE `MigrationId` = '20200128051726_v0.6.0') THEN

    CREATE INDEX `IX_Claims_SubjectId` ON `Claims` (`SubjectId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;


DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__PasswordlessMigrationsHistory` WHERE `MigrationId` = '20200128051726_v0.6.0') THEN

    CREATE INDEX `IX_Claims_Type` ON `Claims` (`Type`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;


DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__PasswordlessMigrationsHistory` WHERE `MigrationId` = '20200128051726_v0.6.0') THEN

    CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;


DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__PasswordlessMigrationsHistory` WHERE `MigrationId` = '20200128051726_v0.6.0') THEN

    INSERT INTO `__PasswordlessMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20200128051726_v0.6.0', '3.1.1');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

