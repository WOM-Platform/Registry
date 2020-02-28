ALTER TABLE `Sources` CHANGE `PublicKey` `PublicKey` VARCHAR(1024) CHARACTER SET latin1 COLLATE latin1_bin NOT NULL;
ALTER TABLE `Sources` CHANGE `PrivateKey` `PrivateKey` VARCHAR(4096) CHARACTER SET latin1 COLLATE latin1_bin NULL DEFAULT NULL; 
ALTER TABLE `POS` CHANGE `PublicKey` `PublicKey` VARCHAR(1024) CHARACTER SET latin1 COLLATE latin1_bin NOT NULL; 
ALTER TABLE `POS` CHANGE `PrivateKey` `PrivateKey` VARCHAR(4096) CHARACTER SET latin1 COLLATE latin1_bin NULL DEFAULT NULL; 

INSERT INTO `Aims` (`Code`, `IconFile`, `Order`) VALUES ('0', NULL, '99999'); 
INSERT INTO `AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('0', 'en', 'Demo'); 
INSERT INTO `AimTitles` (`Code`, `LanguageCode`, `Title`) VALUES ('0', 'it', 'Demo'); 

INSERT INTO `Sources` (`Name`, `PublicKey`, `PrivateKey`, `CreationDate`, `URL`, `ContactID`) VALUES ('Demo', '-----BEGIN PUBLIC KEY-----
MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA5caoDmZ0Ae4jdaVwZpSa
Pnk/+KsB/vzMlSU7F5lnnh9N0XSZqsQXttWr7I+NDE5RUvRQ6OMwCksFGGZbqegH
fR5EP3BsgQPSDi+NbOT2dDlMU/LOXt7vSi5YmnTeeAlIeKD+AE6aSckhYGen4wxS
FgQz1zhLZiXHA1fL1zDu41oroMm6jY5GPoEZKvsjH46qxgkcOdo3P+lFTlf7KgYL
hjfAPrWdi626JHb6pdI5oKFJti93R+ZkArCYtWtQib41fpiS5FD1sZVEOpEf/eXT
kbVRbeXGK5X/j6G1w41Rv2nE8bOXpQ4hq7MlDE8k1RsJ5gKDFUfLwCTyZ0yD+D5B
YLjE6vdLNjjTm937lrgcQYLUwZiHXr4hA6YYkBYmXrK4TxbLr5cACAgJWy2sVkNO
mcZYoRBvxRy5BHQ8ZKNLP31/tse5If6l6VPQxyVoikAUwtAfA6Uc+hTu+zb+84E1
vCKEFx7KzPouylVrnEAzSm+KXelrPi4xrEj3C+XB0dMpf8SzuVChMzy368ygsmzi
66pR0QcpcxYCvMFv7yBlVbHaGc2KF6P7pf0on5YZidtGsAOGuzTHz9km5Ts0pbAH
22XfDk8hPQDvkE8H61p22gUvhNWbuwIi3uQ1rYBbv4G+N+gmn/kwlc2MK6qjyh7H
7nroY5Y00/D6y+avgsz6ZB8CAwEAAQ==
-----END PUBLIC KEY-----
'
, '-----BEGIN RSA PRIVATE KEY-----
MIIJKQIBAAKCAgEA5caoDmZ0Ae4jdaVwZpSaPnk/+KsB/vzMlSU7F5lnnh9N0XSZ
qsQXttWr7I+NDE5RUvRQ6OMwCksFGGZbqegHfR5EP3BsgQPSDi+NbOT2dDlMU/LO
Xt7vSi5YmnTeeAlIeKD+AE6aSckhYGen4wxSFgQz1zhLZiXHA1fL1zDu41oroMm6
jY5GPoEZKvsjH46qxgkcOdo3P+lFTlf7KgYLhjfAPrWdi626JHb6pdI5oKFJti93
R+ZkArCYtWtQib41fpiS5FD1sZVEOpEf/eXTkbVRbeXGK5X/j6G1w41Rv2nE8bOX
pQ4hq7MlDE8k1RsJ5gKDFUfLwCTyZ0yD+D5BYLjE6vdLNjjTm937lrgcQYLUwZiH
Xr4hA6YYkBYmXrK4TxbLr5cACAgJWy2sVkNOmcZYoRBvxRy5BHQ8ZKNLP31/tse5
If6l6VPQxyVoikAUwtAfA6Uc+hTu+zb+84E1vCKEFx7KzPouylVrnEAzSm+KXelr
Pi4xrEj3C+XB0dMpf8SzuVChMzy368ygsmzi66pR0QcpcxYCvMFv7yBlVbHaGc2K
F6P7pf0on5YZidtGsAOGuzTHz9km5Ts0pbAH22XfDk8hPQDvkE8H61p22gUvhNWb
uwIi3uQ1rYBbv4G+N+gmn/kwlc2MK6qjyh7H7nroY5Y00/D6y+avgsz6ZB8CAwEA
AQKCAgBSozm5xBsgvpa+LvvXyMYYhd79/fK/1Ad39GXmPOPJOJTyKa9CfTfSJ9Kn
R5GAxYH2Baw7tcmHSifQ+K8q0iovU31UG4jKdadMNsn0SfxhHGJZJtAVyrPgx/5R
vopsPVp1F+GCFsLimpWIaH825y12gZhrZPGpERkcGK1U/WJNHhbmwuZ3Fp4oyKFW
le+x38uHYatnGxYXxDuKy5WnLXljkhVv+D1rCNYTWz8V+B+Fp7ws67FIiJGbbgvd
43SZtXDj0NeziXJzpa1eKueIlumaU82vap30+wNoks4eQGsQHmgYTXDSidyONeWz
IZurkQfkUy71rlaXCjKr+BbWZv13r0eWPQx4r1JSZG8w3P/I0BrYPZQgjynfCOdp
b/mCbmzAPWgaw9xHR3LuXyTTUVIbjXB0UOxM2dVfe0pG30KLv7hu/mixbLJGcr8d
KIAIwkIU8zO5mxm1v+dkWaeNXIXfhDDV6jRfI0Z2ru8inE3bNX51jv7Aou6jlW6F
kTNS2b7zIgumf75+peSDrIndsYTvOdPDrMwKW9YYJm3qPj8F5BgyFfpgp2TUhibu
z1ZMIyqtaVr/X9ia2nSBE06ijNxa8zLUKSVisZQOfAYblI53+Nc/7i6YCFy2v9co
tPR4hkmekevNrNURaXa6bL9ErWMV2uLy90UJqvnUrEusOmN/wQKCAQEA8y2bJ36t
Paj7RFelREpL8Hh4vUT54Pmcd90SvOqCDnom6rcCelWbUGIJOJenpd2ymenOUdNG
7ByJNfMlUBap1f5qYo/QX/CfEmsOR1DyUZ9YMRmgJsF53SIDXp74DiMgVBgc0+Wt
C2VVlhz9VVPdY7sSAXXIpGnVBSAConilQIAW13JWQGc1nSo3xW/MYoyAlWe0OlOK
NxiwHqfYhj6+O62iGpXMAvopSzc++hxyfZLpFRA4qotnVPXjGz8FZ27aZ6tvY7Vk
+BbeVRasG501Rd2BbOiyMqnnrY6G+bpBS0hjoHDbXoqby/vxguL+4JfV8zPIYxNg
sbYs3YKCrdkNEQKCAQEA8eQmSwXtzDEEUfBZU9ZqF7J0QOUjP3GVVSHws60Y1d2u
EXH4/TjNsJHcOG3pyRwLm29Y41DQaIA6VEWeMTzZk+jMaykEOq3eyHN5OYfzUKKm
wcUPORHq26m7P+1ihaddM4lNTgUbgVTZlv34T8/xbia333vRJUGy23ZLf3KTonpZ
bYS4jFEIuCF8fm5Tz51gsKd+so3uLCIHSVPX/v6kdhX7mKo8iMTh0xNc2TDRfY9z
YuQoRhImfXblD+jgZGoV1GjvUKGm/xAsgyKri/yAoVoK0pG7QCkwQDY7GIzMDtvJ
E4dJQbVPXq+0Tof5Wkylv6HcfspKChM0z7G4LQkeLwKCAQEArHSCXN70Yw8Mqqnx
dV2vPylgjvF0uDys30BzwnAYrcWpBbml0zYUwEvWOEEszm9L8uUhwVuxJ08Ra6Y0
pvh7l1wm/CD7aJ6PYRN0+9SHFKWJeMCwl+uLzewKMbdROU1l5t12zDtMnhFOQffr
HPEtx0VqfoiWMysuu8S4uZoPr49nI4Fdc6z+E6hWBvnDG3yCz/HMmbSXB33FoOft
oT6r0EzR8kKbfN0GHeZfDibQdweRrrNjTGcyb8k1NyRZY8H0t6KXi0GgCmTFZdh/
U9IIbrSozaC0h0OBHs8+H6ocFhSPOr/ugryPwni5DNaIZKSpQSFPEhwy7bTBWpHs
tPeB0QKCAQASaBUN4m6c+iHlBlAV1BwQn5C/G3CaNE9zwfhqA8L/CzZit3SF+FuW
kxLZ7Gs81XiApHF0IsMpIJDPtth50LKR0cY0ZVOgD3kDcd4IpbK7MRVVa2RkKFvh
yUGpdKvplbm+4TTTugnExqskFUFe+Wjaw/F7/RUGK8CreI34LcTUOVEyx0Wvz77F
HC84A7c29jfUWXqHpcs46oH5b3rhOYlUPwn9LP8cPTcd87w9/rwCPPc/0DLMWjc5
luGJW2Qv3+63UiDb1uE5SYbJl8rMBTPYk1x9d39zO28jg7ztelFQ1CKx7LqDWIOo
peAnlatA1tJKCcwYjdYifCGpqFFmg+obAoIBAQDGQiGHWP7YKz+h8ZE70STPCLCE
bqIh7ha4e3C6WdwZ+h+3EtSdgWMghdGdW8GSWxYl72c8x73giEA/675cemtpMmol
cP1I5FHZ2JQ9i78wKfiX/ts5FTooyUcV6e8p/Kl3jrt+V188fPs/38YX2EMj+c4K
0sTSdpGvy+QVipOGpgGHmJGO3/sUKirQAMkFgBksR/ApaNyDg4t4OPmLGkCJMFvi
XxHJ7lCy4BXDcqUPB2FyMXPkXfpC996Odbg7vsUS5AzlBNhhC9OxnZEHqljlWXuF
gowmdqQAqyoOFjSmYD8DGrjBGPOu4fdcW2IvsY+OpMOgHhgwjX1mTMSm9dl6
-----END RSA PRIVATE KEY-----
', NOW(), 'https://wom.social', '1');

INSERT INTO `POS` (`ID`, `Name`, `PublicKey`, `PrivateKey`, `CreationDate`, `URL`) VALUES (NULL, 'Demo POS', '-----BEGIN PUBLIC KEY-----
MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA5caoDmZ0Ae4jdaVwZpSa
Pnk/+KsB/vzMlSU7F5lnnh9N0XSZqsQXttWr7I+NDE5RUvRQ6OMwCksFGGZbqegH
fR5EP3BsgQPSDi+NbOT2dDlMU/LOXt7vSi5YmnTeeAlIeKD+AE6aSckhYGen4wxS
FgQz1zhLZiXHA1fL1zDu41oroMm6jY5GPoEZKvsjH46qxgkcOdo3P+lFTlf7KgYL
hjfAPrWdi626JHb6pdI5oKFJti93R+ZkArCYtWtQib41fpiS5FD1sZVEOpEf/eXT
kbVRbeXGK5X/j6G1w41Rv2nE8bOXpQ4hq7MlDE8k1RsJ5gKDFUfLwCTyZ0yD+D5B
YLjE6vdLNjjTm937lrgcQYLUwZiHXr4hA6YYkBYmXrK4TxbLr5cACAgJWy2sVkNO
mcZYoRBvxRy5BHQ8ZKNLP31/tse5If6l6VPQxyVoikAUwtAfA6Uc+hTu+zb+84E1
vCKEFx7KzPouylVrnEAzSm+KXelrPi4xrEj3C+XB0dMpf8SzuVChMzy368ygsmzi
66pR0QcpcxYCvMFv7yBlVbHaGc2KF6P7pf0on5YZidtGsAOGuzTHz9km5Ts0pbAH
22XfDk8hPQDvkE8H61p22gUvhNWbuwIi3uQ1rYBbv4G+N+gmn/kwlc2MK6qjyh7H
7nroY5Y00/D6y+avgsz6ZB8CAwEAAQ==
-----END PUBLIC KEY-----
', '-----BEGIN RSA PRIVATE KEY-----
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
-----END RSA PRIVATE KEY-----
', 'https://wom.social');

/* Remove POS reference */
ALTER TABLE Wom.PaymentRequests DROP FOREIGN KEY fk_PaymentRequest_POS;
ALTER TABLE `PaymentRequests` ADD `NewPosId` VARCHAR(24) CHARACTER SET latin1 COLLATE latin1_bin NOT NULL AFTER `POSID`; 
UPDATE PaymentRequests SET NewPosId = '' + POSID;

ALTER TABLE `PaymentRequests` DROP INDEX `POSID_idx`;
ALTER TABLE `PaymentRequests` DROP INDEX `Nonce_idx`;
ALTER TABLE `PaymentRequests` DROP `POSID`;
ALTER TABLE `PaymentRequests` CHANGE `NewPosId` `PosId` VARCHAR(24) CHARACTER SET latin1 COLLATE latin1_bin NOT NULL;
ALTER TABLE `Wom`.`PaymentRequests`
    ADD UNIQUE `Nonce_idx` (`PosId`, `Nonce`)
      ADD KEY `POSID_idx` (`PosId`)
;
