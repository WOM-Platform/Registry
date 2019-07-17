-- MySQL Script generated by MySQL Workbench
-- Wed Dec 13 12:05:23 2017
-- Model: New Model    Version: 1.0
-- MySQL Workbench Forward Engineering

-- -----------------------------------------------------
-- Schema Wom
-- -----------------------------------------------------
USE `Wom`;

INSERT INTO `Wom`.`Contacts` (`Email`, `Name`, `Surname`) VALUES ('example@example.org', 'Sample', 'Sample');

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

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('IM', 12);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('IM', 'en', 'Infrastructure monitoring');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('IM', 'it', 'Monitoraggio infrastrutturale');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('T', 8);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('T', 'en', 'Cultural heritage');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('T', 'it', 'Patrimonio culturale');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('X', 9);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('X', 'en', 'Social cohesion');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('X', 'it', 'Coesione sociale');

INSERT INTO `Wom`.`Aims` (`Code`, `Order`) VALUES ('R', 10);
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('R', 'en', 'Human rights');
INSERT INTO `Wom`.`AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('R', 'it', 'Diritti umani');

INSERT INTO `Wom`.`Sources` (`Name`, `PublicKey`, `PrivateKey`, `CreationDate`, `URL`, `ContactID`) VALUES ('Sample source 1', '-----BEGIN PUBLIC KEY-----
MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA3ofbz4kneWa3Fv1PaYzu
acxNKC+Fokz9eHNMi6O5GK9GepY5O5aS++fhFGJ15HpwHHFv7hipSKFocTJ7+Fob
lBZwtIRPfvWBf+nWG1rqEjfVqVm7ZDrB70URrq049NdBEz3UXWTtAoynwq3kU19I
Nicr+ktYw7JYZpV7vgGrF7+tfDQfwP75ZbAFxbK4wK5dtmNuYGwOGTOJV/NQwjNd
nOu3tik2ef6znyFTpXtLa9dQjHa1hKqxWzxmoMaEOv8/Fbew017D0i5kq0cqnEnW
6sJaozfgBHRplgON3O5d12u2/sy2ome20lZzwWFQDtkHmVupFT3IhM3NyD+Dzl4e
MHvY2yr5FCecYuzB4CG585j23gxC5nNGnKrdWnQk+kEXvt4vZRYPLDXhLfmd959p
QMVIUw0hw4JqTJcjGGhrJzI7C7f4iRuPUoOXh3WW0j4WZBLEONaDzwI7Nm5IFFL1
QA2/o9EPna6iYq/WTnk+vgDr0AGfe2eu6siY5gJ0CDavd1ZMI3PybF9wuLsjzkWV
Cn/nedIE4Nid4toS738+flFMiXbbFyP0p0L2KQvH5O21569CWlOLBnEdDJ8xV1A+
21ILxbD3LDRela0W30fvikokJEZy1/eAZhtDNR3S6BNVpIEACGBml4rbJqZY8EuF
l/68yRYlTREY4L4mj9Vb6H0CAwEAAQ==
-----END PUBLIC KEY-----
', '-----BEGIN RSA PRIVATE KEY-----
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
-----END RSA PRIVATE KEY-----
', '2017-12-20', 'http://example.org', 1);

INSERT INTO `Wom`.`POS` (`Name`, `PublicKey`, `PrivateKey`, `CreationDate`, `URL`) VALUES
('Sample POS 1', '-----BEGIN PUBLIC KEY-----
MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA0v/FRGQrynTC4nmDIwW5
bbEp3UdPrajdnIeCnV9d+KkQLL+/lkWmlZcIWyfEzpSVg/aC7DsABsxSruKVIWVo
L2QjDenbK1CzxU83XH7uR/RV79+uskuIzB0pRKdGq2ljeQlOufagIeOelLfLOApz
63AngpeOawYlZGZre9NL0I+QRPTlqDZjdd7X7AkyexpG4EOuIDG6/jFeQGWYoxtw
aVNuDd6KDZ5MF1d48tTEnH/rCZnNunfioEq9vPewwl5n0JmBSAEWvrxgU67t09hL
0SKGWH6njKHf2gw4N3ALQ0nqqeE5o9RjnHWYItjLrJutLX8AkCGnegO5eZWSz31Q
1S2V7N3cw0qrfEke95jwZw2UmmysqE8D9MyXsZfASU5piO5/Mz6Qfng7T6hkM2Df
mNWV4KVt6k5GYQzGXgNyxZMl6SCPd0zBMQyhPRPJxMhl0mu0odhjx/TN5/eDZP1d
dcQFJWz8l8Uqm0M8aiySTjKvEJrhAILWyLbz1kr3tgXDPcud+57FAdeURwcgIpOu
bZhrclevtmIcu3+ErIy711q4aoAXaR64MJ13vz7wbQek2GFy+omoITR0P+Rz46Rv
TezsXoL+zQvSpUm2o7dCeUGsWO6Q4blF/q4V3yJa9GHiHNJrNQtDFMa/ooOl3wKS
tsNv3HLk7sLtVU2GA0GrUgcCAwEAAQ==
-----END PUBLIC KEY-----
', '-----BEGIN RSA PRIVATE KEY-----
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
-----END RSA PRIVATE KEY-----
', '2019-02-08', 'http://example.org');

INSERT INTO `Wom`.`Users` (`Username`, `PasswordSchema`, `PasswordHash`) VALUES
('Test', 'bcrypt', '$2y$12$vpzvi.7CC2hI3aH.4GxjKeD7PTjfD7GRsdRjpvHgmc3cCkU1tKOxe');

INSERT INTO `Wom`.`UserSourceMap` (`UserID`, `SourceID`) VALUES
(1, 1);

INSERT INTO `Wom`.`UserPOSMap` (`UserID`, `POSID`) VALUES
(1, 1);

INSERT INTO `Wom`.`ChangeLog` (`ID`, `Timestamp`, `Note`) VALUES
('aim-list', '2019-07-04', 'First draft of aims.');
