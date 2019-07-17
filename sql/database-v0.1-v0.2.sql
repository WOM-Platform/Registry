USE `Wom`;

DROP TABLE IF EXISTS `Wom`.`ChangeLog`;

CREATE TABLE `Wom`.`ChangeLog` (
  `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `Code` VARCHAR(64) NOT NULL COLLATE latin1_general_ci,
  `Timestamp` DATETIME NOT NULL,
  `Note` TINYTEXT DEFAULT NULL,

  PRIMARY KEY (`ID`),
  INDEX `ChangeLog_Code_idx` (`Code` ASC),
  INDEX `ChangeLog_Timestamp_idx` (`Timestamp` ASC),
  INDEX `ChangeLog_Latest_idx` (`Code` ASC, `Timestamp` DESC)
)
ENGINE = InnoDB;

INSERT INTO `Wom`.`ChangeLog` (`Code`, `Timestamp`, `Note`) VALUES
('aim-list', NOW(), 'First draft of aims.');
