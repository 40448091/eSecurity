import math
import sys

prime=89
startval=1
a=0
b=7

if (len(sys.argv)>1):
	prime=int(sys.argv[1])
if (len(sys.argv)>2):
	a=int(sys.argv[2])
if (len(sys.argv)>3):
	b=int(sys.argv[3])

def findpoints(prime,a,b):
	found=0
	for x in range(1,2*prime):
		#val=((x*x*x) + a*x+ b) % prime   #Elliptic Curve Formula (old)
		y2=((x**3) + 7) % prime           #Elliptic Curve Formula
		y = math.sqrt(y2)

		if (abs(y-int(y))<0.0001):
			print(x,int(y)),
			found=found+1
		if (found>20): return


print "A: ",a
print "B: ",b
print "Prime number:\t\t",prime
if (a==0):
	print "Elliptic curve is:\t\ty^2=x^3+",b
else:
	print "Elliptic curve is:\t\ty^2=x^3+",a,"x+",b
print "Finding the first 20 points"

print
findpoints(prime,a,b)

