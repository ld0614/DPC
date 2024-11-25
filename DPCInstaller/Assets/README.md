# README

## Introduction

Always On VPN Dynamic Profile Configurator (**DPC**) is a client side agent for dynamically creating and managing AOVPN profiles.  It is made up of a Windows Client Service and is configured either using Registry Settings or via Group Policy Administrative Templates.  

DPC aims to make the management of AOVPN solutions easier by enabling updates to configuration without requiring regular deployments and in some cases by removing administrator interaction.

## Requirements

- Windows 10 x64 Enterprise Version 1803 or later
- .NET 4.6.2
- Configured AOVPN solution
- Supported Active Directory Group Policy if using for management

## Installing DPC

- Run the AOVPNDPC.msi installer on a client device.  This will typically install to **C:\Program Files\AOVPN Dynamic Profile Configurator**

- Copy PolicyDefinitions folder into either the local PolicyDefinitions folder at **C:\Windows\PolicyDefinitions** or into the **Domain Central Store** if in use.  

- Create a new group policy with desired settings, all settings are located under **Computer Configuration -> Policies -> Administrative Templates -> AOVPN Dynamic Profile Creation Client**

- Assign test client to relevant Organizational Unit or Group with access to Group Policy

- Run ```gpupdate /force``` on client to ensure that all settings have been replicated to the device

- Start / Restart **DPC Service** to pick up the latest changes

After the service has started (contingent on the Product Key being available), changes will be automatically picked up every 60 minutes by default.  This setting can be configured as part of device configuration.  Immediate changes can be forced by restarting the service

The DPC Service reports status using the Windows Event Logging System (ETW). These logs can be viewed by using the built in tool eventvwr.msc (Event Viewer).  Once the tool has been opened the logs are located under **Applications and Services Logs -> DPC -> AOVPN -> DPCService**.  

**Note:** It is currently only possible to deploy User Tunnels using a computer configuration.  This continues to work at a user level, it simply means that any user who logs into the device will be able to see the VPN Connection

## Version Numbering
The DPC Service application operates on the Semantic Versioning system of MAJOR.MINOR.PATCH an example would be version 1.5.3

**Major Update** - Major updates may introduce breaking changes for existing users.  Care should be taken to read any provided release notes and testing is highly recommended prior to rolling out this new version to all users

**Minor Update** - These updates provide increased functionality while maintaining backwards compatibility.  To take advantage of the new functionality group policy ADMX files will likely need to be updated. 

**Patch Update** - These updates typically fix issues identified with existing functionality and are unlikely to provide a significant changes in client behavior.

In general it is always advised that updates are tested prior to rollout to ensure that the process goes smoothly in a verity of circumstances and is compatible with your specific settings.  

## Support Policy

UPDATE ME

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

ADMX files are included under the install location, this defaults to C:\Program Files\AOVPN Dynamic Profile Configurator\.  These files can either be placed in the local PolicyDefinitions Folder under C:\Windows\PolicyDefinitions or in the Central Store if configured.  

Once copied into the relevant location Group Policy Management should show a new folder named AOVPN Dynamic Profile Creation Client under the Machine Administrative Templates folder.

### Intune Support

There is currently no native Intune support for the DPC tool however you can attempt to upload the ADMX files using the instructions https://docs.microsoft.com/en-us/windows/client-management/mdm/win32-and-centennial-app-policy-configuration
