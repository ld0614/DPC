## Log Locations

### Install Log
By default the initial logs will be generated as a randomly named temporary file within the installing users TEMP directory, typically %TEMP% if manually installed or C:\Windows\Temp if installed automatically or via the system account

### Operational Logs
DPC uses the Event Logging facilities built into Windows, these logs can be viewed by opening Event Viewer (eventvwr.exe) and navigating to Applications and Services Logs -> DPC -> AOVPN -> DPCService.  By default the Admin and Operational logs should provide high level information and any errors in configuration.  Additional Debug and Analytics can be enabled by navigating to View -> Show Analytic and Debug Logs.  These additional logs will require manual enabling by selecting the event log and selecting enable log.

### Profile Logs
DPC can be configured to export the generated XML profile as well as the profile exported from Windows. This is configured using the SavePath and DebugPath variables and are profile specific.  These logs can be saved to a specified location with specified filenames however will default to 'profile name.xml' in the C:\Windows\Temp directory

### Crash Logs
In the event that DPC encounters an unexpected error it will write an error to the event log, create an memory dump and then close.  This memory dump will be located in C:\Windows\Temp and have a prefix of DPCService.  These files contain a full copy of the DPC memory including profile state and may be requested to assist with troubleshooting.

## Registry Locations

As the service runs as LocalSystem it is not currently possible to dynamically update profiles based on the user context.

### Group Policy

All Group Policy starts at **HKEY_LOCAL_MACHINE\SOFTWARE\Policies\DPC\DPCClient**

To enable direct modification of the registry settings can also be configured at **HKEY_LOCAL_MACHINE\SOFTWARE\DPC\DPCClient**

In the event of a conflict the Group Policy setting will take precedence.

The Advantage of using Group Policy is that ADMX templates are provided for a graphical approach.  These settings are also removed when a group policy is no longer applied.  This will automatically delete any old profiles

ADMX files are included under the install location, this defaults to C:\Program Files\AOVPN Dynamic Profile Creation Service\.  These files can either be placed in the local PolicyDefinitions Folder under C:\Windows\PolicyDefinitions or in the Central Store if configured.  

Once copied into the relevant location Group Policy Management should show a new folder named 'DPC Client' under the Machine Administrative Templates folder.

## Debug Builds

Automated builds from the development branch are build with the debug configuration. While larger and slower than the release configuraiotn this build does automatically include code symobols which enable developers to identify which specific line of source code an exception occured at. This is typically only needed in advanced troubleshooting scenarios.