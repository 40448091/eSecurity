import hashlib;
import passlib.hash;
import sys;

def PBKDF2(salt,string):
  if (len(sys.argv)>1):
    string=sys.argv[1]

  if (len(sys.argv)>2):
    salt=sys.argv[2]

  print "salt: " + salt + ", string: " + string
  print "PBKDF2 (SHA1):"+passlib.hash.pbkdf2_sha1.encrypt(string, salt=salt)
  print "PBKDF2 (SHA256):"+passlib.hash.pbkdf2_sha256.encrypt(string, salt=salt)


PBKDF2("ZDzPE45C","changeme")
PBKDF2("ZDzPE45C","123456")
PBKDF2("ZDzPE45C","password")
