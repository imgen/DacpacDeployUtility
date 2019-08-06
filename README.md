# DacpacDeployUtility
A command line utility to replace `SqlPackage.exe` for publishing .dacpac file to database. 

# Why not `SqlPackage.exe`
The problem with `SqlPackage.exe` is sometimes it will keep throwing `StackOverflow` exception while publishing/deployment can be done successfully using `Visual Studio`, suggesting `Visual Studio` is using something different to do the publishing. With this tool, `StackOverflow` exception can be avoided completely. 

## Why not `Visual Studio`
Using `Visual Studio` to open `.sqlproj` and do the publishing/deployment is painfully slow and constantly freeze up, also it is a manual labour, this is true for both `VS2017` and `VS2019`.  

## Usage
```
DacpacDeployUtility.exe [PathToPublishXmlFile] [PathToDacpacFile]
```

It's as simple as that.

To build the database project and generate the `.dacpac` file, just use `MSBuild` like below
```
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "[PathToDatabaseProject]\XXX.sqlproj" /t:build
```

this should generate the `.dacpac` file output in the `bin\Debug` folder, then this utility can be used to publish/deploy to `SQL Server` instance
