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
-- Table `Wom`.`Aim`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Wom`.`Aims` (
  `Code` VARBINARY(64) NOT NULL,
  `Description` VARCHAR(2048) DEFAULT NULL,
  `CreationDate` DATETIME NOT NULL,

  PRIMARY KEY (`Code`)
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`Contact`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Wom`.`Contacts` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Email` VARCHAR(1024) NOT NULL COLLATE latin1_general_ci,
  `Name` VARCHAR(256) NOT NULL,
  `Surname` VARCHAR(256) NOT NULL,

  PRIMARY KEY (`ID`)
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`Source`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Wom`.`Sources` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(256) NOT NULL,
  `PublicKey` VARBINARY(1024) NOT NULL,
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
CREATE TABLE IF NOT EXISTS `Wom`.`POS` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(256) NOT NULL,
  `PublicKey` VARBINARY(1024) NOT NULL,
  `CreationDate` DATETIME NOT NULL,
  `URL` VARCHAR(2048) NULL DEFAULT NULL,

  PRIMARY KEY (`ID`)
)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Wom`.`GenerationRequest`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Wom`.`GenerationRequests` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Amount` SMALLINT UNSIGNED NOT NULL,
  `OTCGen` BINARY(16) NOT NULL,
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
CREATE TABLE IF NOT EXISTS `Wom`.`PaymentRequests` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Amount` SMALLINT UNSIGNED NOT NULL,
  `JsonFilter` VARCHAR(2048) NULL DEFAULT NULL,
  `OTCPay` BINARY(16) NOT NULL,
  `URLAckPocket` VARCHAR(2048) NOT NULL,
  `URLAckPOS` VARCHAR(2048) NULL DEFAULT NULL,
  `CreatedAt` DATETIME NOT NULL,
  `Performed` BIT(1) NOT NULL DEFAULT b'0',
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
-- Table `Wom`.`Voucher`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `Wom`.`Vouchers` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Secret` BINARY(16) NOT NULL,
  `AimCode` VARBINARY(64) NOT NULL,
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


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
