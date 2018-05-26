# MicroElements.DevOps
DevOps scripts for CI and CD

## Windows
### Download bootstrap script
Open a new PowerShell window and run the following command.
```
Invoke-WebRequest https://raw.githubusercontent.com/micro-elements/MicroElements.DevOps/master/resources/build.ps1 -OutFile build.ps1
```

### Initialize component
Run target `Init`
```
.\build.ps1 -Target "Init"
```
