#
# SetVersionNumber.ps1
#

Function Update-AssemblyVersion
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]
        $AssemblyPath,
        [Parameter(Mandatory)]
        [Version]
        $Version
    )

    $AssemblyInfo = Get-Content -Path $AssemblyPath

    $NewAssemblyInfo = $AssemblyInfo.Clone()
    foreach ($line in $AssemblyInfo)
    {
        if (($line -like "*AssemblyVersion*" -or $line -like "*AssemblyFileVersion*") -and -NOT $line.Trim().StartsWith('//'))
        {
            Write-Host "Found: $line"

            $SplitLine = $Line.Split('"')
            if ($SplitLine.Count -ne 3)
            {
                throw "$line was not in the expected format"
            }

            $VersionSplit = $SplitLine[1].Split('.')

            #Update Major
            $VersionSplit[0] = $Version.Major

            #Update Minor
            $VersionSplit[1] = $Version.Minor

            #Update Build
            $VersionSplit[2] = $Version.Build

            #Update Revision if not dynamic
            if ($VersionSplit.Count -gt 3 -and $VersionSplit[3] -ne "*")
            {
                if ($null -ne $Version.Revision)
                {
                    $VersionSplit[3] = $Version.Revision
                }
                else
                {
                    #if installer doesn't specify a revision then make it dynamic
                    $VersionSplit[3] = "*"
                }
            }

            $NewVersion = $VersionSplit -join "."
            Write-Host "Updated Version to $NewVersion"

            $newLine = $line.Replace($SplitLine[1],$NewVersion)
            Write-Host "New Line is: $newLine"
            $NewAssemblyInfo = $NewAssemblyInfo.Replace($line,$newLine)
        }
    }
    Set-Content -Path $AssemblyPath -Value $NewAssemblyInfo
}

Function Update-ADMXSchemaVersion
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]
        $ADMXFolderPath,
        [Parameter(Mandatory)]
        [Version]
        $Version
    )

    $FullFolderPath = Join-Path -Path $pwd -ChildPath $ADMXFolderPath

    $ADMXPath = Join-Path -Path $FullFolderPath -ChildPath "dpc.admx"
    $Version = "$($Version.Major).$($Version.Minor)"

    [xml]$ADMXFile = Get-Content -Path $ADMXPath

    $ADMXFile.policyDefinitions.revision = $Version
    $ADMXFile.policyDefinitions.resources.minRequiredRevision = $Version

    $ADMXFile.Save($ADMXPath)

    $ADMLPath = Join-Path -Path $FullFolderPath -ChildPath "en-us\dpc.adml"

    [xml]$ADMLFile = Get-Content -Path $ADMLPath

    $ADMLFile.policyDefinitionResources.revision = $Version

    $ADMLFile.Save($ADMLPath)
}

$InstallerVersionPath = "DPCInstaller\ProductVersion.wxi"

[xml]$InstallerContent = Get-Content -Path $InstallerVersionPath

foreach ($Define in $InstallerContent.Include.define)
{
    $SplitDefine = $Define.Split('=')
    if ($SplitDefine[0] -eq "ProductVersion")
    {
        [Version]$InstallerVersion = $SplitDefine[1]
    }
}

Write-Output "Installer Version: $InstallerVersion"

Update-AssemblyVersion -AssemblyPath "DPCService\Properties\AssemblyInfo.cs" -Version $InstallerVersion
Update-AssemblyVersion -AssemblyPath "DPCLibrary\Properties\AssemblyInfo.cs" -Version $InstallerVersion

Update-ADMXSchemaVersion -ADMXFolderPath "DPCInstaller\ADMX" -Version $InstallerVersion