import passlib.hash;


def apr1_encrypt(salt,password):
  print "salt:"+salt+",password:"+password
  print "APR1:"+passlib.hash.apr_md5_crypt.encrypt(password, salt=salt)


apr1_encrypt("wK6l4VDP","changeme")
apr1_encrypt("DmKxp9Ie","123456")
apr1_encrypt("PqKTe0tM","password")
