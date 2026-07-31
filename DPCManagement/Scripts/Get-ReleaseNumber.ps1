param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Full Release", "Release Candidate", "Preview", "Beta")]
    [string]
    $ReleaseType
    [Parameter(Mandatory = $true)]
    [ValidateSet("Breaking Change", "Feature Update", "Patch")]
    [string]
    $NextVersionType
)

[xml]$versionData = get-content "DPCInstaller\ProductVersion.wxi"
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

if ($ReleaseType -ne "Full Release")
{
    $ReleaseNumber = $ReleaseNumberPrefix + $NewVersion
    $ReleaseName = $ReleaseNamePrefix + $NewVersion
}

if ($AllReleases -contains $ReleaseNumber)
{
    throw "Version $ReleaseNumber is already a git tag"
}

#Export values back to the pipeline
Add-Content -Path $env:GITHUB_OUTPUT -Value "releaseNumber=$releaseNumber"
Add-Content -Path $env:GITHUB_OUTPUT -Value "releaseName=$releaseName"
Add-Content -Path $env:GITHUB_OUTPUT -Value "finalReleaseNumber=$NextVersion"
Add-Content -Path $env:GITHUB_OUTPUT -Value "installerNumber=$VersionString"