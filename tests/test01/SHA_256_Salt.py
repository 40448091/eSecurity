import passlib.hash;

def SHA_Encrypt(salt,string):
  print "string: " + string + ", salt: "+ salt
  print "SHA1:"+passlib.hash.sha1_crypt.encrypt(string, salt=salt)
  print "SHA256:"+passlib.hash.sha256_crypt.encrypt(string, salt=salt)
  print "SHA512:"+passlib.hash.sha512_crypt.encrypt(string, salt=salt)

salt="8sFt66rZ"

SHA_Encrypt(salt,"hello")
SHA_Encrypt(salt,"changeme")
SHA_Encrypt(salt,"123456")
SHA_Encrypt(salt,"password")

