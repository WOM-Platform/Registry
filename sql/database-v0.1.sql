-- MySQL Script generated by MySQL Workbench
-- Wed Dec 13 12:05:23 2017
-- Model: New Model    Version: 1.0
-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='TRADITIONAL,ALLOW_INVALID_DATES';

-- -----------------------------------------------------
-- Schema Wom
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `Wom` DEFAULT CHARACTER SET utf8;
USE `Wom`;

-- -----------------------------------------------------
-- Table `Wom`.`Aims`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`Aims` (
  `Code` VARCHAR(3) NOT NULL COLLATE latin1_general_ci,
  `IconFile` VARCHAR(128) DEFAULT NULL,
  `Order` MEDIUMINT UNSIGNED NOT NULL,

  PRIMARY KEY (`Code`)
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`AimTitles`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`AimTitles` (
  `Code` VARCHAR(3) NOT NULL COLLATE latin1_general_ci,
  `LanguageCode` CHAR(2) NOT NULL COLLATE latin1_general_ci,
  `Title` VARCHAR(256) NOT NULL,

  PRIMARY KEY (`Code`, `LanguageCode`),
  CONSTRAINT `fk_Aim_AimTitle`
    FOREIGN KEY `fk_Aim_AimTitle` (`Code`)
    REFERENCES `Wom`.`Aims` (`Code`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`Contact`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`Contacts` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Email` VARCHAR(320) NOT NULL COLLATE latin1_general_ci,
  `Name` VARCHAR(256) NOT NULL,
  `Surname` VARCHAR(256) NOT NULL,

  PRIMARY KEY (`ID`)
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`Source`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`Sources` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(256) NOT NULL,
  `PublicKey` VARBINARY(1024) NOT NULL,
  `PrivateKey` VARBINARY(4096) NULL,
  `CreationDate` DATETIME NOT NULL,
  `URL` VARCHAR(2048) NULL DEFAULT NULL,
  `ContactID` INT UNSIGNED NOT NULL,

  PRIMARY KEY (`ID`),
  INDEX `fk_Source_Contact_idx` (`ContactID` ASC),
  CONSTRAINT `fk_Source_Contact`
    FOREIGN KEY `fk_Source_Contact_idx` (`ContactID`)
    REFERENCES `Wom`.`Contacts` (`ID`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`POS`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`POS` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(256) NOT NULL,
  `PublicKey` VARBINARY(1024) NOT NULL,
  `PrivateKey` VARBINARY(4096) NULL,
  `CreationDate` DATETIME NOT NULL,
  `URL` VARCHAR(2048) NULL DEFAULT NULL,

  PRIMARY KEY (`ID`)
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`GenerationRequest`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`GenerationRequests` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Amount` SMALLINT UNSIGNED NOT NULL,
  `OTCGen` CHAR(36) NOT NULL COLLATE latin1_general_ci,
  `CreatedAt` DATETIME NOT NULL,
  `Verified` BIT(1) NOT NULL DEFAULT b'0',
  `PerformedAt` DATETIME DEFAULT NULL,
  `Void` BIT(1) NOT NULL DEFAULT b'0',
  `SourceID` INT UNSIGNED NOT NULL,
  `Nonce` VARBINARY(32) NOT NULL,
  `Password` VARCHAR(8) NOT NULL COLLATE latin1_general_ci,

  PRIMARY KEY (`ID`),
  UNIQUE INDEX `OTCGen_idx` (`OTCGen` ASC),
  UNIQUE INDEX `Nonce_idx` (`SourceID` ASC, `Nonce` ASC),
  INDEX `SourceID_idx` (`SourceID` ASC),
  CONSTRAINT `fk_GenerationRequest_Source`
    FOREIGN KEY `SourceID_idx` (`SourceID`)
    REFERENCES `Wom`.`Sources` (`ID`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`PaymentRequest`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`PaymentRequests` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Amount` SMALLINT UNSIGNED NOT NULL,
  `JsonFilter` VARCHAR(2048) NULL DEFAULT NULL,
  `OTCPay` CHAR(36) NOT NULL COLLATE latin1_general_ci,
  `URLAckPocket` VARCHAR(2048) NOT NULL,
  `URLAckPOS` VARCHAR(2048) NULL DEFAULT NULL,
  `CreatedAt` DATETIME NOT NULL,
  `Verified` BIT(1) NOT NULL DEFAULT b'0',
  `Persistent` BIT(1) NOT NULL DEFAULT b'0',
  `Void` BIT(1) NOT NULL DEFAULT b'0',
  `POSID` INT UNSIGNED NOT NULL,
  `Nonce` VARBINARY(32) NOT NULL,
  `Password` VARCHAR(8) NOT NULL COLLATE latin1_general_ci,

  PRIMARY KEY (`ID`),
  UNIQUE INDEX `OTCPay_idx` (`OTCPay` ASC),
  UNIQUE INDEX `Nonce_idx` (`POSID` ASC, `Nonce` ASC),
  INDEX `POSID_idx` (`POSID` ASC),
  CONSTRAINT `fk_PaymentRequest_POS`
    FOREIGN KEY `POSID_idx` (`POSID`)
    REFERENCES `Wom`.`POS` (`ID`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`PaymentConfirmations`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`PaymentConfirmations` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `PaymentRequestID` INT UNSIGNED NOT NULL,
  `PerformedAt` DATETIME NOT NULL,

  PRIMARY KEY (`ID`),
  INDEX `PaymentID_idx` (`PaymentRequestID` ASC),
  CONSTRAINT `fk_PaymentConfirmation_Request`
    FOREIGN KEY `PaymentID_idx` (`PaymentRequestID`)
    REFERENCES `Wom`.`PaymentRequests` (`ID`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`Voucher`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`Vouchers` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Secret` BINARY(16) NOT NULL,
  `AimCode` VARCHAR(3) NOT NULL COLLATE latin1_general_ci,
  `Latitude` DOUBLE NOT NULL,
  `Longitude` DOUBLE NOT NULL,
  `Timestamp` DATETIME NOT NULL,
  `GenerationRequestID` INT UNSIGNED NOT NULL,
  `PaymentRequestID` INT UNSIGNED NULL DEFAULT NULL,
  `Spent` BIT(1) NOT NULL DEFAULT b'0',
  
  PRIMARY KEY (`ID`),
  INDEX `Voucher_AimCode_idx` (`AimCode` ASC),
  CONSTRAINT `fk_Voucher_Aim`
    FOREIGN KEY `Voucher_AimCode_idx`(`AimCode`)
    REFERENCES `Wom`.`Aims` (`Code`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  INDEX `Voucher_GenerationRequest_idx` (`GenerationRequestID` ASC),
  CONSTRAINT `fk_Voucher_GenerationRequest`
    FOREIGN KEY `Voucher_GenerationRequest_idx` (`GenerationRequestID`)
    REFERENCES `Wom`.`GenerationRequests` (`ID`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  INDEX `Voucher_PaymentRequest_idx` (`PaymentRequestID` ASC),
  CONSTRAINT `fk_Voucher_PaymentRequest`
    FOREIGN KEY `Voucher_PaymentRequest_idx` (`PaymentRequestID`)
    REFERENCES `Wom`.`PaymentRequests` (`ID`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`Users`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`Users` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Username` VARCHAR(128) NOT NULL COLLATE latin1_general_ci,
  `PasswordSchema` CHAR(10) NOT NULL,
  `PasswordHash` VARBINARY(128) NOT NULL,

  PRIMARY KEY (`ID`)
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`UserSourceMap`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`UserSourceMap` (
  `UserID` INT UNSIGNED NOT NULL,
  `SourceID` INT UNSIGNED NOT NULL,
  
  PRIMARY KEY (`UserID`, `SourceID`),
  INDEX `UserID_idx` (`UserID`),
  CONSTRAINT `fk_UserSourceMap_User`
    FOREIGN KEY `UserID_idx` (`UserID`)
    REFERENCES `Wom`.`Users` (`ID`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  INDEX `SourceID_idx` (`SourceID`),
  CONSTRAINT `fk_UserSourceMap_Source`
    FOREIGN KEY `SourceID_idx` (`SourceID`)
    REFERENCES `Wom`.`Sources` (`ID`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`UserPOSMap`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`UserPOSMap` (
  `UserID` INT UNSIGNED NOT NULL,
  `POSID` INT UNSIGNED NOT NULL,
  
  PRIMARY KEY (`UserID`, `POSID`),
  INDEX `UserID_idx` (`UserID`),
  CONSTRAINT `fk_UserPOSMap_User`
    FOREIGN KEY `UserID_idx` (`UserID`)
    REFERENCES `Wom`.`Users` (`ID`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  INDEX `POSID_idx` (`POSID`),
  CONSTRAINT `fk_UserPOSMap_POS`
    FOREIGN KEY `POSID_idx` (`POSID`)
    REFERENCES `Wom`.`POS` (`ID`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`ChangeLog`
-- -----------------------------------------------------
CREATE TABLE `Wom`.`ChangeLog` (
  `ID` VARCHAR(64) NOT NULL COLLATE latin1_general_ci,
  `Timestamp` DATETIME NOT NULL,
  `Note` TINYTEXT DEFAULT NULL,

  PRIMARY KEY (`ID`, `Timestamp`)
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Insert starting data
-- -----------------------------------------------------
INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('N', 1);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('N', 'en', 'Natural environment');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('N', 'it', 'Ambiente naturale');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('U', 2);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('U', 'en', 'Urban environment');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('U', 'it', 'Ambiente urbano');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('C', 3);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('C', 'en', 'Culture');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('C', 'it', 'Cultura');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('E', 4);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('E', 'en', 'Education');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('E', 'it', 'Istruzione');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('H', 5);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('H', 'en', 'Health and wellbeing');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('H', 'it', 'Salute e benessere');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('S', 6);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('S', 'en', 'Safety');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('S', 'it', 'Sicurezza');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('I', 7);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('I', 'en', 'Infrastructure and services');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('I', 'it', 'Infrastruttura e servizi');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('T', 8);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('T', 'en', 'Cultural heritage');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('T', 'it', 'Patrimonio culturale');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('X', 9);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('X', 'en', 'Social cohesion');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('X', 'it', 'Coesione sociale');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('R', 10);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('R', 'en', 'Human rights');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('R', 'it', 'Diritti umani');

INSERT INTO `Wom`.`ChangeLog` (`ID`, `Timestamp`, `Note`) VALUES
('aim-list', '2019-07-15', 'First draft of aims.');


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
