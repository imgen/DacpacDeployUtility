# DacpacDeployUtility
A command line utility to replace SqlPackage.exe for publishing .dacpac file to database

## Usage
```
DacpacDeployUtility.exe [PathToPublishXmlFile] [PathToDacpacFile]
```

It's as simple as that.

To build the database project and generate the `.dacpac` file, just use `MSBuild` like below
```
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "[PathToDatabaseProject]\XXX.sqlproj" /t:build
```

this should generate a .dacpac in the `bin\Debug` folder, then this utility can be used to publish/deploy to SQL Server instance
