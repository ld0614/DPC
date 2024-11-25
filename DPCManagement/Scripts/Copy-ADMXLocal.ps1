#
# Copy_ADMXLocal.ps1
#

$RootDir = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent

Copy-Item -Path (Join-Path -Path $RootDir -ChildPath "DPCInstaller\ADMX\*") -Destination (Join-Path -Path $env:SystemRoot -ChildPath "PolicyDefinitions") -Recurse -Verbose -Force

gpedit.msc