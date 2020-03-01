from Crypto.PublicKey import RSA
from Crypto.Util import asn1
from base64 import b64encode
from Crypto.Cipher import PKCS1_OAEP
#from cryptography.hazmat.primitives import serialization
#from cryptography.hazmat.backends import default_backend
import sys


binPrivKey = ""
binPubKey = ""
msg = ""

#process args
for arg in sys.argv:
  p = arg.split("=")
  if p[0] == "msg":
    msg=p[1]
  elif p[0] == "priv":
    print "loading private key"
    binPrivKey = open(p[1],"r").read()
  elif p[0] == "pub":
    print "loading public key"
    binPubKey = open(p[1],"r").read()


if msg == "":
  msg = "uW6FQth0pKaWc3haoqxbjIA7q2rF+G0Kx3z9ZDPZGU3NmBfzpD9ByU1ZBtbgKC8ATVZzwj15AeteOnbjO3EHQC4A5Nu0xKTWpqpngYRGGmzMGtblW3wBlNQYovDsRUGt+cJK7RD0PKn6PMNqK5EQKCD6394K/gasQ9zA6fKn3f0="


if binPubKey == "" or binPrivKey == "":
  key = RSA.generate(1024)
  binPrivKey = key.exportKey('PEM')
  binPubKey =  key.publickey().exportKey('PEM')


print
print "====Private key==="
print binPrivKey
print
print "====Public key==="
print binPubKey

#load the key strings
privKeyObj = RSA.importKey(binPrivKey)
pubKeyObj =  RSA.importKey(binPubKey)


cipher = PKCS1_OAEP.new(pubKeyObj)
ciphertext = cipher.encrypt(msg)

print
print "====Ciphertext==="
print b64encode(ciphertext)

cipher = PKCS1_OAEP.new(privKeyObj)
message = cipher.decrypt(ciphertext)


print
print "====Decrypted==="
print "Message:",message