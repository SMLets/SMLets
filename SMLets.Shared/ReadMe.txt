SCSM Cmdlets
To install the cmdlets:
1) Unless you are building SMLets as part of a development effort you can 
   simply install SMLets using the most recent release pack from the CodePlex 
   site.  You can install using the provided .msi.
2) If you are building and need to be able to deploy SCSM you can follow 
   the instructions below:

To build the module, use the build.ps1 script in the current 
directory. Just run the build.ps1 script which should compile the module.
To import the module, you can copy the module files to your module
directory, or type the following (from the directory where you built 
the module):

PS> import-module $PWD
