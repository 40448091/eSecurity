import OpenSSL 
from cryptography import x509
from cryptography.hazmat.backends import default_backend

str="fred.pfx"
passwords=["ankle","battery","password","bill","apple","apples","orange"]

for password in passwords:
	try:
		pfx = open(str, 'rb').read()
		
		p12 = OpenSSL.crypto.load_pkcs12(pfx, password)
		print "Found: ",password


		privkey=OpenSSL.crypto.dump_privatekey(OpenSSL.crypto.FILETYPE_PEM, p12.get_privatekey())

		cert=OpenSSL.crypto.dump_certificate(OpenSSL.crypto.FILETYPE_PEM, p12.get_certificate())

		cert = x509.load_pem_x509_certificate(cert, default_backend())


		print " Issuer: ",cert.issuer
		print " Subect: ",cert.subject
		print " Serial number: ",cert.serial_number
		print " Hash: ",cert.signature_hash_algorithm.name
		print privkey
		print certificate


	except:

		print "Not working: ",password

