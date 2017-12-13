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
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`Aim` (
  `ID` INT NOT NULL,
  `Description` VARCHAR(105) NOT NULL,
  `ID_Contact` INT NOT NULL,
  `Type` VARCHAR(45) NOT NULL,
  `CreationDate` TIMESTAMP NOT NULL,
  PRIMARY KEY (`ID`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`Contact`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`Contact` (
  `ID` INT NOT NULL AUTO_INCREMENT,
  `Email` VARCHAR(45) NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `Surname` VARCHAR(45) NOT NULL,
  `Phone` INT(15) NULL,
  PRIMARY KEY (`ID`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`Source`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`Source` (
  `ID` INT NOT NULL AUTO_INCREMENT,
  `Key` VARCHAR(45) NOT NULL,
  `CreationDate` TIMESTAMP NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `Descriprion` VARCHAR(105) NOT NULL,
  `URLSource` VARCHAR(85) NOT NULL,
  `Aim_ID` INT NOT NULL,
  `Contact_ID` INT NOT NULL,
  PRIMARY KEY (`ID`),
  INDEX `fk_Source_Aim1_idx` (`Aim_ID` ASC),
  INDEX `fk_Source_Contact1_idx` (`Contact_ID` ASC),
  CONSTRAINT `fk_Source_Aim1`
    FOREIGN KEY (`Aim_ID`)
    REFERENCES `VoucherPiattaforma`.`Aim` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Source_Contact1`
    FOREIGN KEY (`Contact_ID`)
    REFERENCES `VoucherPiattaforma`.`Contact` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`POS`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`POS` (
  `ID` INT NOT NULL,
  `Key` VARCHAR(45) NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `Description` VARCHAR(105) NOT NULL,
  `CreationDate` TIMESTAMP NOT NULL,
  `URLPOS` VARCHAR(85) NOT NULL,
  PRIMARY KEY (`ID`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`PaymentRequest`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`PaymentRequest` (
  `ID` INT NOT NULL,
  `ID_POS` INT NOT NULL,
  `URLAckPocket` VARCHAR(85) NOT NULL,
  `Amount` INT NOT NULL,
  `OTCPay` VARCHAR(85) NOT NULL,
  `URLAckPOS` VARCHAR(85) NOT NULL,
  `JsonFilter` VARCHAR(450) NULL,
  `CreatedAt` TIMESTAMP NOT NULL,
  `State` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`ID`),
  INDEX `fk_PaymentRequest_POS1_idx` (`ID_POS` ASC),
  CONSTRAINT `fk_PaymentRequest_POS1`
    FOREIGN KEY (`ID_POS`)
    REFERENCES `VoucherPiattaforma`.`POS` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`GenerationRequest`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`GenerationRequest` (
  `ID` INT NOT NULL,
  `Amount` INT NOT NULL,
  `OTC` VARCHAR(45) NOT NULL,
  `CreatedAt` TIMESTAMP NOT NULL,
  `State` VARCHAR(45) NOT NULL,
  `Source_ID` INT NOT NULL,
  PRIMARY KEY (`ID`),
  INDEX `fk_GenerationRequest_Source1_idx` (`Source_ID` ASC),
  CONSTRAINT `fk_GenerationRequest_Source1`
    FOREIGN KEY (`Source_ID`)
    REFERENCES `VoucherPiattaforma`.`Source` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`Voucher`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`Voucher` (
  `ID` INT NOT NULL,
  `Latitude` VARCHAR(15) NOT NULL,
  `Longitude` VARCHAR(15) NOT NULL,
  `CreatedAt` TIMESTAMP NOT NULL,
  `Type` VARCHAR(45) NOT NULL,
  `ID_Source` INT NOT NULL,
  `OTC` VARCHAR(100) NOT NULL,
  `ID_PaymentRequest` INT NULL DEFAULT NULL,
  `Nonce` VARCHAR(45) NOT NULL,
  `ID_GenerationRequest` INT NOT NULL,
  PRIMARY KEY (`ID`),
  INDEX `fk_Voucher_Source1_idx` (`ID_Source` ASC),
  INDEX `fk_Voucher_PaymentRequest1_idx` (`ID_PaymentRequest` ASC),
  INDEX `fk_Voucher_GenerationRequest1_idx` (`ID_GenerationRequest` ASC),
  CONSTRAINT `fk_Voucher_Source1`
    FOREIGN KEY (`ID_Source`)
    REFERENCES `VoucherPiattaforma`.`Source` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Voucher_PaymentRequest1`
    FOREIGN KEY (`ID_PaymentRequest`)
    REFERENCES `VoucherPiattaforma`.`PaymentRequest` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_Voucher_GenerationRequest1`
    FOREIGN KEY (`ID_GenerationRequest`)
    REFERENCES `VoucherPiattaforma`.`GenerationRequest` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `VoucherPiattaforma`.`VoucherArchive`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `VoucherPiattaforma`.`VoucherArchive` (
  `ID` INT NOT NULL,
  `Latitude` VARCHAR(15) NOT NULL,
  `Longitude` VARCHAR(15) NOT NULL,
  `CreatedAt` TIMESTAMP NOT NULL,
  `Type` VARCHAR(45) NOT NULL,
  `ID_Source` INT NOT NULL,
  `OTC` VARCHAR(100) NOT NULL,
  `ID_PaymentRequest` INT NULL DEFAULT NULL,
  `Nonce` VARCHAR(45) NOT NULL,
  `ID_GenerationRequest` INT NOT NULL,
  PRIMARY KEY (`ID`),
  INDEX `fk_VoucherArchive_Source1_idx` (`ID_Source` ASC),
  INDEX `fk_VoucherArchive_PaymentRequest1_idx` (`ID_PaymentRequest` ASC),
  INDEX `fk_VoucherArchive_GenerationRequest1_idx` (`ID_GenerationRequest` ASC),
  CONSTRAINT `fk_VoucherArchive_Source1`
    FOREIGN KEY (`ID_Source`)
    REFERENCES `VoucherPiattaforma`.`Source` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_VoucherArchive_PaymentRequest1`
    FOREIGN KEY (`ID_PaymentRequest`)
    REFERENCES `VoucherPiattaforma`.`PaymentRequest` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `fk_VoucherArchive_GenerationRequest1`
    FOREIGN KEY (`ID_GenerationRequest`)
    REFERENCES `VoucherPiattaforma`.`GenerationRequest` (`ID`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
