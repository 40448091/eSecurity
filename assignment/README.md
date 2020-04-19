# Implications of Quantum Computing on Bitcoin - A Bitcoin-like Test Framework

Advances in Quantum Computing put the cryptographic primitives that protect the Bitcoin Blockchain at risk.

The aim of this project was to build a Bitcoin-like Blockchain Test Framework into which different Cryptographic Providers could be loaded for evaluation and comparison.

The Blockchain itself is built on and extends the work by Daniel van Flyman:

* "Learn Blockchains by Building One", by Daniel van Flymen
  Original Source: 
      [https://github.com/dvf/blockchain/tree/master/csharp/BlockChain](https://github.com/dvf/blockchain/tree/master/csharp/BlockChain)
      [https://github.com/dvf/blockchain](https://github.com/dvf/blockchain)
      
  Project Source: [/master/assignment/blockchain/BlockChainServerNode](https://github.com/40448091/eSecurity/tree/master/assignment/blockchain/BlockChainServerNode)

The extended Blockchain server code allows any public-key cryptographic provider that implements the ICryptoProvider interface to be dynamically loaded. It introduces a Bitcoin-like address, input address validation and multiple input and output addresses similar to the way Bitcoin operates.

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

A Server-Node console was added to allow the user to interact with the server for test purposes:
Project Source: [/master/assignment/blockchain/BlockChainServerNode/BlockChain.Console](https://github.com/40448091/eSecurity/tree/master/assignment/blockchain/BlockChainServerNode/BlockChain.Console)

A Client Console Application was added to allow the user to submit requests to the server via the command line and display results:
Project Source: [/master/assignment/blockchain/BlockChainCLI](https://github.com/40448091/eSecurity/tree/master/assignment/blockchain/BlockChainCLI)
_Additionally the client has a batch-file interpreter so consistent and comparable tests can be performed across cryptographic providers

_NB: The source code can be downloaded and amended as required, though additional cryptographic providers (implementing the ICryptoProvider interface) are dynamically loaded (by placing the compiled .DLL file into the client and server __bin__ directory, and amending the CrytoProvider key in the corresponding application __.config__ files. Client and Server code does not need to be recompiled._

### A precompiled Example Test Environment 

A precompiled Example Test Environment can be found here: [/master/assignment/Example_Test_Environment](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment)

It contains sub-directories for one interactive Client and Two Server Nodes. 

Example Client Test Scripts can be found here: [/master/assignment/Example_Test_Environment/Tests](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment/Tests)

To run a test, navigate to the the [/master/assignment/Example_Test_Environment/Tests](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment/Tests) directory:

1. Amend the following config files to ensure "CryptoProvider" key is set to the same provider:
   * [/master/assignment/Example_Test_Environment/Client/](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment/Client/)_BlockChainClient.exe.config_
   * [/master/assignment/Example_Test_Environment/ServerNode01](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment/ServerNode01)_BlockChainDemo.Console.exe.config_
   * [/master/assignment/Example_Test_Environment/ServerNode02](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment/ServerNode02)_BlockChainDemo.Console.exe.config_
2. Execute the _run_2_Servers.bat_ batch file to launch two interconnected Server-Nodes (loading the specified CryptoProvider)

3. Execute either of the following batch files to launch the appropriate client:
   * _Client_Test_RLWE.bat_ 
   * _Client_Test_ED2551901.bat_ 

4. Resuls are output in a sub-directory of the server-nodes, given the same name as the CryptoProvider being tested:
   * ED25519 [/master/assignment/Example_Test_Environment/ServerNode01/ED25519](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment/ServerNode01/ED25519)
   * RLWE [/master/assignment/Example_Test_Environment/ServerNode01/RLWE](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment/ServerNode01/RLWE)

   - Files in the [Checkpoints](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment/ServerNode01/ED25519/checkpoints) directory contain the Blockchain states (or contents). Use these to see what Blockchain contents before and after tests.
   - Files in the [Logs](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment/ServerNode01/ED25519/logs) directory contain timed log entries showing requests received by the server, and output generated from those requests. 
   _NB: To help with file analysis, special commands can be sent from the client to add "Test xxx" start and end markers into the log

### Test Notes

Notes on the Client and Server console applications can be found in this document [Notes.docx](https://github.com/40448091/eSecurity/tree/master/assignment/Example_Test_Environment/Notes.docx)

