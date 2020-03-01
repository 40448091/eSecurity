#https://docs.python.org/3/library/hmac.html

import hashlib
import hmac
import base64
secret = b'qwerty123'
message = b'Hello'
signing = hmac.new(secret, message, hashlib.md5)
print message
h=""
for i in bytearray(signing.digest()):
   h+=hex(i)
print h 
print base64.b64encode(bytearray(signing.digest()))

#Type:		HMAC-MD5
#Message:	Hello
#Password:	qwerty123
#Hex:		c3a2fa8f20dee654a32c30e666cec48e
#Base64:	7376b67daf1fdb475e7bae786b7d9cdf47baeba71e738f1e
