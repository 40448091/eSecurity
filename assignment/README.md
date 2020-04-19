# Implications of Quantum Computing on Bitcoin

Advances in Quantum Computing put the cryptographic primitives that protect the Bitcoin Blockchain at risk.

The aim of this project was to build a Bitcoin-like Blockchain Test Framework into which different Cryptographic Providers could be loaded for evaluation and comparison.

The Blockchain itself is built on and extends the work by Daniel van Flyman:

* "Learn Blockchains by Building One", by Daniel van Flymen
  Original Source: 
      [https://github.com/dvf/blockchain/tree/master/csharp/BlockChain](https://github.com/dvf/blockchain/tree/master/csharp/BlockChain)
      [https://github.com/dvf/blockchain](https://github.com/dvf/blockchain)

The extended Blockchain server code allows any public-key cryptographic provider that implements the ICryptoProvider interface to be dynamically loaded.

Two public-key based crytographic providers are included:

* Elliptic Curve: _presumed to be at risk from Shor's algorithm
  Incorporates "C# .NET Port of the Curve25519 Diffie-Hellman function" by Hans Wolff
  Original Source: [https://github.com/hanswolff/curve25519](https://github.com/hanswolff/curve25519)
  Project Source: [/master/assignment/CryptoProvider/ed25519](https://github.com/40448091/eSecurity/tree/master/assignment/CryptoProvider/ed25519)
  _NB: Bitcoin uses Elliptic Curve secp256k1 to protect addresses

* Ring Learning With Errors: _presumed to be Quantum-Safe
  Incorproates "An implementation of Ring-LWE in C#" created by John G. Underhill (Steppenwolfe65)
  Original Source: [https://github.com/Steppenwolfe65/RingLWE-NET](https://github.com/Steppenwolfe65/RingLWE-NET)
  Project Source: [/master/assignment/CryptoProvider/RingLWE](https://github.com/40448091/eSecurity/tree/master/assignment/CryptoProvider/RingLWE)

Additional public-key cryptographic providers can be added by creating .NET wrapper DLL's that implement the ICryptoProvider interface
  [/master/assignment/CryptoProvider/ICryptoProvider](https://github.com/40448091/eSecurity/tree/master/assignment/CryptoProvider/ICryptoProvider)

## Bitcoin-like Blockchain Test Framework with dynamically loadabled Crytographic Providers 

1. Introduction and Code Structure

This code

2. Test Framework Installation 

```c#
installation 
```

2. Running 

```
details of running the code, and test documentation
``` 
