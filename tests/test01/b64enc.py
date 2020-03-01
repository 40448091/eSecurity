def base64encode(message):
  b64char=("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-")
  msg = message
  bits = ""
  for c in msg:
    bits+="{0:b}".format(ord(c)).zfill(8)  #zero pad char to 8 bits
 
  encoded = ""
  while len(bits) > 0:
    chBits=bits[0:6]
    bits=bits[6:]

    if len(chBits)<6:
        chBits.ljust(6,"0") #zero pad
        
    encoded += b64char[int(chBits,2)]

  encoded += "=="[0:4-(len(encoded)%4)]

  return encoded


