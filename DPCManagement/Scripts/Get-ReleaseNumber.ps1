param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Full Release", "Release Candidate", "Preview", "Beta")]
    [string]
    $ReleaseType,
    [Parameter(Mandatory = $true)]
    [ValidateSet("Breaking Change", "Feature Update", "Patch")]
    [string]
    $NextVersionType
)

[xml]$versionData = Get-Content "DPCInstaller\ProductVersion.wxi"
$VersionString = $versionData.Include.define.Split("=")[-1]

if ([string]::IsNullOrWhiteSpace($VersionString))
{
    throw "Unable to get Version from ProductVersion.wxi"
}

[Version]$Version = $VersionString

switch ($NextVersionType)
{
    "Breaking Change"
    {
        $NextVersion = New-Object -TypeName version -ArgumentList @(
            $Version.Major + 1
            0
        )
    }
    "Feature Update"
    {
        $NextVersion = New-Object -TypeName version -ArgumentList @(
            $Version.Major
            $Version.Minor + 1
        )
    }
    "Patch"
    {
        $NextVersion = New-Object -TypeName version -ArgumentList @(
            $Version.Major
            $Version.Minor
            $Version.Build + 1
        )
    }
}

switch ($ReleaseType)
{
    "Full Release"
    {
        $ReleaseNumber = "v$Version"
        $ReleaseName = "Version $Version"
        $NextVersion = $Version
    }
    "Release Candidate"
    {
        $ReleaseNumberPrefix = "v$NextVersion-rc"
        $ReleaseNamePrefix = "Version $NextVersion Release Candidate "
    }
    "Preview"
    {
        $ReleaseNumberPrefix = "v$NextVersion-preview"
        $ReleaseNamePrefix = "Version $NextVersion Preview "
    }
    "Beta"
    {
        $ReleaseNumberPrefix = "v$NextVersion-beta"
        $ReleaseNamePrefix = "Version $NextVersion Beta "
    }
    default { throw "Invalid Release Type: $ReleaseType" }
}

$AllReleases = git tag --list

$LatestTag = $AllReleases | Where-Object { $_.StartsWith($ReleaseNumberPrefix) } | Sort-Object -Descending | Select-Object -First 1

if ($null -ne $LatestTag)
{
    #Not a full release and there have previously been other releases of this type so we need to find and increment the type number ie rc1 to rc2
    [int]$LatestVersion = $LatestTag.Replace($ReleaseNumberPrefix, "")
    $NewVersion = $LatestVersion + 1
}
else
{
    #no previous versions found so set number to 1
    $NewVersion = 1
}

$ExistingReleaseExists = $null -ne ($AllReleases | Where-Object { $_ -eq "v$VersionString" })
if ($ExistingReleaseExists -and $ReleaseType -eq "Full Release")
{git
    throw "Version $VersionString is already a git tag. Please update the ProductVersion.wxi file and try again."
}
elseif ($ExistingReleaseExists -and $NextVersionType -eq "Patch")
{
    $VersionOverride = "0.0.$NewVersion"
    Write-Output "Version $VersionString is already a git tag. This is a patch preview release so Installer will be set to $VersionOverride"
}
elseif ($ExistingReleaseExists -and $NextVersionType -eq "Feature Update")
{
    $VersionOverride = "$($Version.Major).$($Version.Minor).$($NewVersion+100)"
    Write-Output "Version $VersionString is already a git tag. This is a feature update preview release so Installer will be set to $VersionOverride"
}
elseif ($ExistingReleaseExists -and $NextVersionType -eq "Breaking Change")
{
    $VersionOverride = "$($Version.Major).$($Version.Minor).$($NewVersion+900)"
    Write-Output "Version $VersionString is already a git tag. This is a major update preview release so Installer will be set to $VersionOverride"
}

if ($ReleaseType -ne "Full Release")
{
    $ReleaseNumber = $ReleaseNumberPrefix + $NewVersion
    $ReleaseName = $ReleaseNamePrefix + $NewVersion
}

if ($AllReleases -contains $ReleaseNumber)
{
    throw "Version $ReleaseNumber is already a git tag"
}

if (-NOT [string]::IsNullOrWhiteSpace($VersionOverride))
{
    $AutoUpgradeWorking = $Version -lt [version]$VersionOverride
    $VersionString = $VersionOverride
}
elseif ($ReleaseType -ne "Full Release")
{
    $AutoUpgradeWorking = $Version -lt $NextVersion
}
else
{
    $AutoUpgradeWorking = $true
}

#Export values back to the pipeline
Add-Content -Path $env:GITHUB_OUTPUT -Value "releaseNumber=$releaseNumber"
Add-Content -Path $env:GITHUB_OUTPUT -Value "releaseName=$releaseName"
Add-Content -Path $env:GITHUB_OUTPUT -Value "finalReleaseNumber=$NextVersion"
Add-Content -Path $env:GITHUB_OUTPUT -Value "installerNumber=$VersionString"
Add-Content -Path $env:GITHUB_OUTPUT -Value "versionOverride=$VersionOverride"
Add-Content -Path $env:GITHUB_OUTPUT -Value "autoUpgradeWorking=$AutoUpgradeWorking"