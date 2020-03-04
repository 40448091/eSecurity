
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



p=919
q=191
N=p*q
PHI=(p-1)*(q-1)

#find e
CryptoMath.finde(PHI,1,100)
#select 91 from the list 
e=91

#find d
d=CryptoMath.inverse_mod(e,PHI)


M=5
C=(M**e)%N
D=(C**d)%N





