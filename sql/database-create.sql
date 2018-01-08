-- MySQL Script generated by MySQL Workbench
-- Wed Dec 13 12:05:23 2017
-- Model: New Model    Version: 1.0
-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='TRADITIONAL,ALLOW_INVALID_DATES';

-- -----------------------------------------------------
-- Schema VoucherPiattaforma
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Schema VoucherPiattaforma
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `VoucherPiattaforma` DEFAULT CHARACTER SET utf8 ;
USE `VoucherPiattaforma` ;

-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`Aim`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`Aims` (
  `ID` INT NOT NULL AUTO_INCREMENT,
  `Description` VARCHAR(105) NOT NULL,
  `ContactID` INT NOT NULL,
  `Type` VARCHAR(45) NOT NULL,
  `CreationDate` TIMESTAMP NOT NULL,
  PRIMARY KEY (`ID`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`Contact`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`Contacts` (
  `ID` INT NOT NULL AUTO_INCREMENT,
  `Email` VARCHAR(45) NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `Surname` VARCHAR(45) NOT NULL,
  `Phone` VARCHAR(100) NULL DEFAULT NULL,
  PRIMARY KEY (`ID`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`Source`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`Sources` (
  `ID` INT NOT NULL AUTO_INCREMENT,
  `Key` VARCHAR(1024) NOT NULL,
  `CreationDate` TIMESTAMP NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `Description` VARCHAR(255) NULL DEFAULT NULL,
  `URL` VARCHAR(255) NULL DEFAULT NULL,
  `AimID` INT NOT NULL,
  `ContactID` INT NOT NULL,
  PRIMARY KEY (`ID`),
  INDEX `fk_Source_Aim1_idx` (`AimID` ASC),
  INDEX `fk_Source_Contact1_idx` (`ContactID` ASC),
  CONSTRAINT `fk_Source_Aim1`
    FOREIGN KEY (`AimID`)
    REFERENCES `VoucherPiattaforma`.`Aims` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Source_Contact1`
    FOREIGN KEY (`ContactID`)
    REFERENCES `VoucherPiattaforma`.`Contacts` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`POS`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`POS` (
  `ID` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  `Key` VARCHAR(1024) NOT NULL,
  `Description` VARCHAR(1024) NULL DEFAULT NULL,
  `CreationDate` TIMESTAMP NOT NULL,
  `URL` VARCHAR(1024) NULL DEFAULT NULL,
  PRIMARY KEY (`ID`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`PaymentRequest`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`PaymentRequests` (
  `ID` INT NOT NULL AUTO_INCREMENT,
  `Amount` INT NOT NULL,
  `JsonFilter` VARCHAR(2048) NULL DEFAULT NULL,
  `OTCPay` VARCHAR(255) NOT NULL,
  `URLAckPocket` VARCHAR(1024) NOT NULL,
  `URLAckPOS` VARCHAR(1024) NULL DEFAULT NULL,
  `CreatedAt` TIMESTAMP NOT NULL,
  `State` VARCHAR(45) NOT NULL,
  `POSID` INT NOT NULL,
  PRIMARY KEY (`ID`),
  INDEX `fk_PaymentRequest_POS1_idx` (`POSID` ASC),
  CONSTRAINT `fk_PaymentRequest_POS1`
    FOREIGN KEY (`POSID`)
    REFERENCES `VoucherPiattaforma`.`POS` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`GenerationRequest`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`GenerationRequests` (
  `ID` INT NOT NULL AUTO_INCREMENT,
  `Amount` INT NOT NULL,
  `OTC` VARCHAR(45) NOT NULL,
  `CreatedAt` TIMESTAMP NOT NULL,
  `State` VARCHAR(45) NOT NULL,
  `SourceID` INT NOT NULL,
  PRIMARY KEY (`ID`),
  INDEX `fk_GenerationRequest_Source1_idx` (`SourceID` ASC),
  CONSTRAINT `fk_GenerationRequest_Source1`
    FOREIGN KEY (`SourceID`)
    REFERENCES `VoucherPiattaforma`.`Sources` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`Voucher`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`Vouchers` (
  `ID` BINARY(16) NOT NULL,
  `Latitude` DOUBLE NOT NULL,
  `Longitude` DOUBLE NOT NULL,
  `Timestamp` TIMESTAMP NOT NULL,
  `OTC` VARCHAR(255) NOT NULL,
  `Type` VARCHAR(45) NOT NULL,
  `Nonce` BINARY(16) NOT NULL,
  `SourceID` INT NOT NULL,
  `PaymentRequestID` INT NULL DEFAULT NULL,
  `GenerationRequestID` INT NOT NULL,
  PRIMARY KEY (`ID`),
  INDEX `fk_Voucher_Source1_idx` (`SourceID` ASC),
  INDEX `fk_Voucher_PaymentRequest1_idx` (`PaymentRequestID` ASC),
  INDEX `fk_Voucher_GenerationRequest1_idx` (`GenerationRequestID` ASC),
  CONSTRAINT `fk_Voucher_Source1`
    FOREIGN KEY (`SourceID`)
    REFERENCES `VoucherPiattaforma`.`Sources` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Voucher_PaymentRequest1`
    FOREIGN KEY (`PaymentRequestID`)
    REFERENCES `VoucherPiattaforma`.`PaymentRequests` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Voucher_GenerationRequest1`
    FOREIGN KEY (`GenerationRequestID`)
    REFERENCES `VoucherPiattaforma`.`GenerationRequests` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
