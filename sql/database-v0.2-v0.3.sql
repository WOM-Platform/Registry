USE `Wom`;

ALTER TABLE `Contacts`
    ADD COLUMN `FiscalCode` VARCHAR(64) DEFAULT NULL COLLATE latin1_general_ci AFTER `Email`,
    ADD INDEX `Email_idx` (`Email`),
    ADD UNIQUE INDEX `FiscalCode` (`FiscalCode`)
    ;

ALTER TABLE `Users`
    ADD COLUMN `ContactID` INT UNSIGNED DEFAULT NULL AFTER `PasswordHash`
    ;

-- Fix up missing links directly (works only if Users were created using script so far)
UPDATE Users SET ContactId = ID;

ALTER TABLE `Users`
    CHANGE COLUMN `ContactID` `ContactID` INT UNSIGNED NOT NULL,
    ADD INDEX `fk_User_Contact_idx` (`ContactID` ASC),
    ADD CONSTRAINT `fk_User_Contact`
        FOREIGN KEY `fk_User_Contact_idx` (`ContactID`)
        REFERENCES `Wom`.`Contacts` (`ID`)
        ON DELETE RESTRICT
        ON UPDATE RESTRICT
    ;
