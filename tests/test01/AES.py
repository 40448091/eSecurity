from Crypto.Cipher import AES
import hashlib
import sys
import binascii
import Padding

val='hello'
password='hello'
plaintext=val

print sys.argv
print len(sys.argv)

if (len(sys.argv)>2):
  val=sys.argv[1]

if (len(sys.argv)>2):
  password=sys.argv[2]

def encrypt(plaintext,key, mode):
  encobj = DES.new(key,mode)
  return(encobj.encrypt(plaintext))

def decrypt(ciphertext,key, mode):
  encobj = DES.new(key,mode)
  return(encobj.decrypt(ciphertext))

key = hashlib.sha256(password).digest()

plaintext = Padding.appendPadding(plaintext,blocksize=Padding.AES_blocksize,mode='CMS')
print "After padding (CMS): "+binascii.hexlify(bytearray(plaintext))
ciphertext = encrypt(plaintext,key,AES.MODE_ECB)
print "Cipher (ECB): "+binascii.hexlify(bytearray(ciphertext))
plaintext = decrypt(ciphertext,key,AES.MODE_ECB)
plaintext = Padding.removePadding(plaintext,mode='CMS')
print " decrypt: "+plaintext
plaintext=val
