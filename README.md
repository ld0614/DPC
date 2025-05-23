# DPC
The new home for DPC. This repo replaces the PowerON Platforms AOVPN DPC Client with a new, open source and free to use version.

# What is DPC?
Microsoft AOVPN is a VPN technology backed into Windows 10 and 11. It enables a (mostly) seamless connection either from boot or from login, enabling users to forget about remote connectivity and concentrate on their jobs.

AOVPN can be managed natively within Windows using an XML configuration which is then 'injected' into the Windows Configuration Service Provider (CSP). This is clunky, error prone and doesn't scale well or enable profile upgrades

Microsoft provide an configuration interface for Intune however this has its own issues including bugs, missing features and license requirements. 

DPC attempts to solve these issues with features including:
- Support for Active Directory GPOs
- Built-in support for configuration changes
- Error checking and profile misconfiguration identification
- Limited FQDN resolution
- Dynamic support for excluding M365 IPs in Force Tunnel
- Monitoring of VPN State
- Old VPN Profile Cleanup
- Documentation for all settings

## Need Commercial Support?

If your organisation in looking for direct access to the DPC development team, prioritised assistance and support then please reach out to dpc@darcy.org.uk and we can discuss your requirements

## Questions?

Hang out and chat with us on the [MS Remote Access UG](https://discord.gg/qzgajr9Dev) Discord server if its a more general question or AOVPN related.

If you've got a specific issue or a feature request please create an issue in Github to enable easier tracking

# DPC Setup/Installation
- Download the latest DPC Installer from [here](../../releases/latest)
- Grab the ADMX files from [here](DPCInstaller/ADMX) and add to AD/Intune
- Copy settings from existing AOVPN profile into the configuration GPO/Profile
- Deploy configuration to end clients
- Install DPC client on all end devices (Simple MSI with no attributes)

# Troubleshooting
Please see the seperate guide [here](Troubleshooting.md)

## Migration from PowerON DPC for AD Users

- Download new ADMX files from [here](DPCInstaller/ADMX)
- Add new ADMX files to Domain Controllers, ADMX Central store and client where Migrate-DPCConfig.ps1 will be run from
- Create new GPO and Link to same Locations as previous GPO
- Run Migrate-DPCConfig.ps1 from [here](DPCManagement/Scripts/Migrate-DPCConfig.ps1)
- Review and Migrate any non-DPC settings in GPO
- Upgrade DPC to Version 5+
- Remove old GPO
- Remove old ADMX Files

Please note that this script will not delete any previously added settings as such it is strongly recommended that an empty GPO is used

## Migration from PowerON DPC for Intune Users

Unfortunately there is no easy way to copy an existing Configuration policy and update it in Intune, as such it is recommended that the new ADMX files are imported and settings are manually copied over.

# Release Notes

## Version 5.0.3
- Improved error logging
- Added Trusted Network detection validation for unicode letters
- Reverted IPv6 support when excluding M365 traffic due to issues connecting after these routes where added

## Version 5.0.2

- Disabled Register DNS on the user tunnel if already configured on the device tunnel
- Updated ADMX wording to clarify a couple of settings
- Added IPv6 support when excluding M365 traffic
- Made Trusted Network detection validation more permissive
- Fixed issue where Override XML would set MTU to be 0

## Version 5.0.1

- Ignore null values in registry settings
- Added support for <EMPTY> value in ADMX settings which have optional values
- Allow RasMan to restart if the Device Tunnel is still operational
- Added additional events for troubleshooting
- Debug installer now installs Debug Symbols for additional information

## Version 5.0.0

- Removed Product Key and Trial Subsystems
- Updated EULA
- Updated License
- Removed references to PowerON Platforms
- Open Sourced Application
- (Breaking Change) Changed Event Log Location
- (Breaking Change) Changed Registry Location
- (Breaking Change) Make Full Monitoring Mandatory
- (Breaking Change) Removed Unsupported Domain Name Information Setting on Machine Tunnel

# Development

With the release of DPC as an open source product additional Contributors are greatly welcomed! 

Please take a look through the information [here](DEVELOPMENT.md) and raise an issue if you've got an idea or would like to get involved

# Special Thanks
DPC would not have been possible without the following people

- My Amazing Wife Christine
- The entire team at PowerON
- Richard Hicks
- Philipp Kuhn
