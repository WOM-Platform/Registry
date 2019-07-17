-- -----------------------------------------------------
-- Template that registers a new source and POS combo
-- with user login
-- -----------------------------------------------------

USE `Wom`;

INSERT INTO `Wom`.`Contacts` (`Email`, `Name`, `Surname`) VALUES ('EMAIL', 'NAME', 'SURNAME');

SET @contact_id = LAST_INSERT_ID();

INSERT INTO `Wom`.`Sources` (`Name`, `PublicKey`, `PrivateKey`, `CreationDate`, `URL`, `ContactID`) VALUES ('SOURCE NAME', '-----BEGIN PUBLIC KEY-----
-----END PUBLIC KEY-----
', '-----BEGIN RSA PRIVATE KEY-----
-----END RSA PRIVATE KEY-----
', CURDATE(), 'URL', @contact_id);

SET @source_id = LAST_INSERT_ID();

INSERT INTO `Wom`.`POS` (`Name`, `PublicKey`, `PrivateKey`, `CreationDate`, `URL`) VALUES
('POS NAME', '-----BEGIN PUBLIC KEY-----
-----END PUBLIC KEY-----
', '-----BEGIN RSA PRIVATE KEY-----
-----END RSA PRIVATE KEY-----
', CURDATE(), 'URL');

SET @pos_id = LAST_INSERT_ID();

INSERT INTO `Wom`.`Users` (`Username`, `PasswordSchema`, `PasswordHash`) VALUES
('USERNAME', 'bcrypt', 'HASH');

SET @user_id = LAST_INSERT_ID();

INSERT INTO `Wom`.`UserSourceMap` (`UserID`, `SourceID`) VALUES
(@user_id, @source_id);

INSERT INTO `Wom`.`UserPOSMap` (`UserID`, `POSID`) VALUES
(@user_id, @pos_id);
