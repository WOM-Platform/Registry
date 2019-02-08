REM Certificate generation
openssl req -new -x509 -key registry.pem -out registry.cer -nodes -days 365 -subj "/C=IT/ST=PU/L=Urbino/O=University of Urbino"

REM Generate RSA private key (4096 bits)
openssl genrsa -out pos1.pem 4096

REM Generate RSA public key from private
openssl rsa -in pos1.pem -pubout -out pos1.pem
