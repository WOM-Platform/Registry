-- MySQL Script generated by MySQL Workbench
-- Wed Dec 13 12:05:23 2017
-- Model: New Model    Version: 1.0
-- MySQL Workbench Forward Engineering

-- -----------------------------------------------------
-- Schema VoucherPiattaforma
-- -----------------------------------------------------
USE `VoucherPiattaforma` ;

INSERT INTO `voucherpiattaforma`.`contacts` (`Email`, `Name`, `Surname`, `Phone`) VALUES ('example@example.org', 'Test User', 'Test User', '123');

INSERT INTO `voucherpiattaforma`.`aims` (`Description`, `ID_Contact`, `Type`, `CreationDate`) VALUES ('Test aim', '1', 'Test', '2017-12-20');

INSERT INTO `voucherpiattaforma`.`sources` (`Key`, `CreationDate`, `Name`, `Descriprion`, `URLSource`, `Aim_ID`, `Contact_ID`) VALUES ('Sample key', '2017-12-20', 'Sample source', 'Sample source', 'http://example.org', '1', '1');
