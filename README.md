# RegistryStrikesBack

RegistryStrikesBack allows a red team operator to export valid .reg files for portions of the Windows Registry via a .NET assembly that should run as a standard user. It can be useful in exfiltrating config files such as to support actions like are described in the "Segmentation Vault" article on the [MDSec Blog](https://www.mdsec.co.uk/knowledge-centre/insights/).

## Note

This is not yet fully implemented, its a best effort and it does not yet support all datatypes and may lead to some unexpected results. However, it did function for the use cases required.

## Usage

```
RegistryStrikesBack.exe <key> [output file path]
```

Export OneDrive Registry Keys to file in .reg format

```
RegistryStrikesBack.exe HKCU\Software\Microsoft\OneDrive C:\ProgramData\OneDriveBusiness.reg
```

Export OneDrive Registry Keys to console in .reg format

```
RegistryStrikesBack.exe HKCU\Software\Microsoft\OneDrive
```

## Author
* **David Middlehurst, MDSec ActiveBreach** - Twitter- [@dtmsecurity](https://twitter.com/dtmsecurity)

## Acknowledgments
* PowerShell Implementation https://franckrichard.blogspot.com/2010/12/generate-reg-regedit-export-to-file.html