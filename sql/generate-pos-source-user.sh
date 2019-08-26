#! /bin/bash

if [ -e output.sql ]
then
    echo "File output.sql exists."
    exit
fi

echo "This script generates a SQL script that generates a user and an associated Source and POS."

read -p 'Contact e-mail: ' emailvar
read -p 'Contact name: ' namevar
read -p 'Contact surname: ' surnamevar

read -p 'Username: ' usernamevar
read -sp 'Password: ' passwordvar
echo ""

read -p 'Source name: ' sourcenamevar
read -p 'Source URL: ' sourceurlvar
read -p 'POS name: ' posnamevar
read -p 'POS URL: ' posurlvar

echo "Generating RSA keys..."
openssl genrsa -out tmp-source.pem 4096
openssl genrsa -out tmp-pos.pem 4096

echo "-- Creating Source and POS for user $namevar $surnamevar ($emailvar)" > output.sql

echo "USE \`Wom\`;" >> output.sql

echo "INSERT INTO \`Wom\`.\`Contacts\` (\`Email\`, \`Name\`, \`Surname\`) VALUES ('$emailvar', '$namevar', '$surnamevar');" >> output.sql
echo "SET @contact_id = LAST_INSERT_ID();" >> output.sql

printf "INSERT INTO \`Wom\`.\`Sources\` (\`Name\`, \`PublicKey\`, \`PrivateKey\`, \`CreationDate\`, \`URL\`, \`ContactID\`) VALUES ('$sourcenamevar', '" >> output.sql
openssl rsa -in tmp-source.pem -pubout >> output.sql
printf "', '" >> output.sql
cat tmp-source.pem >> output.sql
echo "', CURDATE(), '$sourceurlvar', @contact_id);" >> output.sql

echo "SET @source_id = LAST_INSERT_ID();" >> output.sql

printf "INSERT INTO \`Wom\`.\`POS\` (\`Name\`, \`PublicKey\`, \`PrivateKey\`, \`CreationDate\`, \`URL\`) VALUES ('$posnamevar', '" >> output.sql
openssl rsa -in tmp-pos.pem -pubout >> output.sql
printf "', '" >> output.sql
cat tmp-pos.pem >> output.sql
echo "', CURDATE(), '$posurlvar');" >> output.sql

echo "SET @pos_id = LAST_INSERT_ID();" >> output.sql

printf "INSERT INTO \`Wom\`.\`Users\` (\`Username\`, \`PasswordSchema\`, \`PasswordHash\`, \`ContactID\`) VALUES ('$usernamevar', 'bcrypt', '" >> output.sql
htpasswd -bnBC 12 "" $passwordvar | tr -d ':\n' >> output.sql
echo "', @contact_id);" >> output.sql

echo "SET @user_id = LAST_INSERT_ID();" >> output.sql

echo "INSERT INTO \`Wom\`.\`UserSourceMap\` (\`UserID\`, \`SourceID\`) VALUES (@user_id, @source_id);" >> output.sql

echo "INSERT INTO \`Wom\`.\`UserPOSMap\` (\`UserID\`, \`POSID\`) VALUES (@user_id, @pos_id);" >> output.sql

echo "Cleaning up..."

rm tmp-source.pem
rm tmp-pos.pem

echo "All done."
