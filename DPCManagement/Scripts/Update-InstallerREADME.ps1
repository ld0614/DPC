#
# Update_InstallerREADME.ps1
#

$FilePath = "DPCInstaller\Assets\README.md"
$OutputPath = "DPCInstaller\Assets\README.html"

$ReadMePath = Resolve-Path $FilePath
$OutputPath = Resolve-Path $OutputPath

Convertfrom-Markdown -Path $ReadMePath | Select-Object -ExpandProperty Html | Out-File -FilePath $OutputPath