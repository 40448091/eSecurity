'''
import base64

data = "abc123!?$*&()'-=@~"

# Standard Base64 Encoding
encodedBytes = base64.b64encode(data.encode("utf-8"))
encodedStr = str(encodedBytes, "utf-8")

print(encodedStr)


----------

ata = "abc123!?$*&()'-=@~"

# URL and Filename Safe Base64 Encoding
urlSafeEncodedBytes = base64.urlsafe_b64encode(data.encode("utf-8"))
urlSafeEncodedStr = str(urlSafeEncodedBytes, "utf-8")

print(urlSafeEncodedStr)

'''


from Crypto.Cipher import DES
import base64
import hashlib
import sys
import binascii
import Padding

password='hello123'
textIn='hello'
mode='e' #encrypt


print sys.argv
print len(sys.argv)

if (len(sys.argv)>3):
  mode=sys.argv[1]
  textIn=sys.argv[2]
  password=sys.argv[3]
else:
  mode=raw_input('Enter mode (e for encrypt, d for decrypt):')
  textIn=raw_input('Enter text:')
  password=raw_input('Enter password:')

print(mode,textIn,password)

def encrypt(textIn,key, mode):
  encobj = DES.new(key,mode)
  return(encobj.encrypt(textIn))

def decrypt(textOut,key, mode):
  encobj = DES.new(key,mode)
  return(encobj.decrypt(textOut))

key = hashlib.sha256(password).digest()[:8]

if mode=='e':
  textIn = Padding.appendPadding(textIn,blocksize=Padding.DES_blocksize,mode='CMS')
  print "After padding (CMS): "+binascii.hexlify(bytearray(textIn))
  ciphertext = encrypt(textIn,key,DES.MODE_ECB)
  b64EncodedCipher = base64.encodestring(ciphertext)
  print "Cipher (ECB) b64: " + b64EncodedCipher 
else:
  b64decoded=base64.decodestring(textIn)
  textIn = binascii.unhexlify(b64decoded)
  plaintext = decrypt(textIn,key,DES.MODE_ECB)
  plaintext = Padding.removePadding(plaintext,mode='CMS')
  print " decrypt: "+plaintext


