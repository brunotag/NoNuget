# NoNuget

We have a library (a folder) in the cloud which contains our dependancies.
This library is mirrored on the local dev machines (like a OneDrive folder).

It contains

- all the versions of the Xero libraries (pushed there by teamcity) in a directory tree like:
  - internal/
    - /[dll name without extension]
      - /branch
        - /version
          - /net4.0
          - /net4.5	

- all the versions of the external libraries (pushed there maybe from NuGet) in a directory tree like:
  - external/
    - /[dll name without extension]
        - /version
          - /net4.0
          - /net4.5	


Hypothesis => not good (see the (b) implication)

For each visual studio solution we have a solution level dependencies version config file which, for each dependency, describes which version has to be used.
This implies that: 
a) All the projects of that solution MUST use the same version for each dependency (LEGIT).
b) A project is part of at most only one solution (NOT GOOD)
- STOP, not good-


so, Hypothesis version 2 (good):

For each branch of each GitHub repository we have a branch level dependencies version config file which, for each dependency, describes which version has to be used.
This implies that: 
a) All the projects of that solution MUST use the same version for each dependency (LEGIT).
b) A project is part of at most only one GitHub repository (GOOD)
c) Visual studio is not aware of the dependency configuration (LEGIT)

So we can carry on.

The dependencies config file structure could be something like (the author is familiar with XML but any other descriptive language could be used):

<dependencies>
  <dependency id="The.Third.Party.Dll.File.Name.dll" type="external" version="3.14" net="net4.0"/>
  <dependency id="The.Xero.Owned.Dll.File.Name.dll" type="internal" branch="master" version="42" net="net4.5"/>
</dependencies>

The format of the config file should be decided chosing the easiest one to be merged in diffMerge, which is the tool we use to manual merge.


Then, maybe when Visual Studio loads a solution contained in that GitHub repository, the config file is read and the HintPath for each reference in each csproj file is changed accordingly to the global config.
Also, all other references will be forcibly removed, in order to enforce the use of that repository (exceptions: a list of standard .NET dlls. This list could be made of the GAC of a gold master (virtual) machine which has a particular .NET version installed on it. This list could be a readonly file stored in that same OneDrive folder where the lib folder is)

=> we need some kind of visual studio extension that takes place when a solution is loaded.
