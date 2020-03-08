class CryptoMath:

  @classmethod
  def gcd(self,a,b):
    while b != 0:
      a, b = b, a % b
    return a

  @classmethod
  def coprime(self,a, b):
    return self.gcd(a, b) == 1

  @classmethod
  def _extended_euclidean_algorithm(self,a, b):
    """
    Returns a three-tuple (gcd, x, y) such that
    a * x + b * y == gcd, where gcd is the greatest
    common divisor of a and b.

    This function implements the extended Euclidean
    algorithm and runs in O(log b) in the worst case.
    """
    s, old_s = 0, 1
    t, old_t = 1, 0
    r, old_r = b, a

    while r != 0:
        quotient = old_r // r
        old_r, r = r, old_r - quotient * r
        old_s, s = s, old_s - quotient * s
        old_t, t = t, old_t - quotient * t

    return old_r, old_s, old_t

  @classmethod
  def finde(self,PHI,min=1,max=0):
    coprimes = []
    if max == 0:
      max=PHI
    for x in range(min,max):
      if self.coprime(x,PHI):
        coprimes.append(x)

    return coprimes


  @classmethod
  def inverse_mod(self,n,p):
    """
    Returns the multiplicative inverse of
    n modulo p.

    This function returns an integer m such that
    (n * m) % p == 1.
    """
    gcd, x, y = self._extended_euclidean_algorithm(n, p)
    assert (n * x + p * y) % p == gcd

    if gcd != 1:
        # Either n is 0, or p is not a prime number.
        raise ValueError(
            '{} has no multiplicative inverse '
            'modulo {}'.format(n, p))
    else:
        return x % p


  @classmethod
  def sieve_for_primes_to(self,n):
    size = n/2
    sieve = [1]*size
    limit = int(n**0.5)
    for i in range(1,limit):
        if sieve[i]:
            val = 2*i+1
            tmp = ((size-1) - i)/val 
            sieve[i+val::val] = [0]*tmp
    return [2] + [i*2+1 for i, v in enumerate(sieve) if v and i>0]


  @classmethod
  def DHGenerator(self,p):
    values=[]
    for x in range(1,p):
      rand = x
      exp = 1 
      next = rand % p
      
      while(next <> 1):
        next = (next * rand) % p
        exp = exp + 1
      
      if (exp == p-1):
        values.append(rand)
    
    return values
   


import random
import base64
import hashlib

class Crypto:

  @classmethod
  def RSA(self):
    print "Primes 1 to 100"
    print CryptoMath.sieve_for_primes_to(100)
    p=input("Select Prime p=")
    q=input("Select Prime q=")
   
    N=p*q
    print "N=p*q={}".format(N)
    
    PHI=(p-1)*(q-1)
    print "PHI=(p-1)(q-1)={}".format(PHI)

    print "Select a value for e from the list"
    print CryptoMath.finde(PHI,1,100)
    e=input("Select e=")

    d=CryptoMath.inverse_mod(e,PHI)
    print "finding d=inverse_mod(e,PHI)={}".format(d)

    M=input("M=")
    C=(M**e)%N
    print "Cipher C={}".format(C)
    
    D=(C**d)%N
    print "Decipher D={}".format(D)


  @classmethod
  def DH(self):
    print CryptoMath.sieve_for_primes_to(5500)
    p=input("Select Prime (N) p=")
    print "Recommended Generator values (though this can be higher than p):"
    print CryptoMath.DHGenerator(p)
    g=input("Select Generator g=")
    x=input("enter (Random) value for x:")

    print "Bob:-"
    print "  Selects random value x={}".format(x)
    A=(g**x) % p
    print "  Calculates A=(g**x)%p=({}**{}) % {} = {}".format(g,x,p,A)
    print "  then sends A to Alice"

    print "Alice:-"
    y=input("enter (Random) value for y:")
    print "  Selects random value y={}".format(y)
    B=(g**y) % p
    print "  Calculates B=(g**y)%p=({}**{}) % {} = {}".format(g,y,p,B)
    print "  then sends B to Bob"

    print ""

    print "Alice Calculates:"
    keyA = (A**y) % p
    print "  keyA = (A**y) % p = ({}**{} % {}) = {}".format(A,y,p,keyA)
    print "  KeyA = {}".format(hashlib.sha256(str(keyA)).hexdigest())

    print "" 

    print "Bob Calculates:"
    keyB = (B**x) % p
    print "  keyB = (B**x) % p = ({}**{} % {}) = {}".format(B,x,p,keyB)
    print "  KeyB = {}".format(hashlib.sha256(str(keyB)).hexdigest())


    
      



'''
from eSecurity import CryptoMath
from eSecurity import Crypto

Crypto.RSA()
'''



