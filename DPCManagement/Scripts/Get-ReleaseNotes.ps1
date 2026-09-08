param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Full Release", "Release Candidate", "Preview", "Beta")]
    [string]
    $ReleaseType,
    [Parameter(Mandatory = $true)]
    [string]
    $Version,
    [Parameter(Mandatory = $true)]
    [string]
    $InstallerVersion,
    [Parameter()]
    [bool]
    $AutoUpgradeWorking=$true
)

$README = Get-Content .\README.md | Out-String

$Sections = $README.Split("## ")

$ReleaseNotes = ""
$vNextReleaseNotes = ""

foreach ($Section in $Sections)
{
    if ($Section.Trim().StartsWith("Version $Version"))
    {
        $ReleaseNotes = $Section.Trim().Replace("Version $Version","").Trim()
        break
    }

    if ($Section.Trim().StartsWith("Version vNext"))
    {
        $vNextReleaseNotes = $Section.Trim().Replace("Version vNext","").Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($ReleaseNotes) -and -NOT [string]::IsNullOrWhiteSpace($vNextReleaseNotes) -and $ReleaseType -ne "Full Release")
{
    $ReleaseNotes = $vNextReleaseNotes
}

if ([string]::IsNullOrWhiteSpace($ReleaseNotes))
{
    throw "Unable to locate release notes for $Version"
}

$ReleaseText = "Release Notes:`n`n$ReleaseNotes`n`n"

if ($InstallerVersion -ne $Version)
{
    $ReleaseText += "Please note that to enable automatic upgrades to the released version, this release has an internal build version of $InstallerVersion.`n`n"
}

if ($ReleaseType -ne "Full Release")
{
    $ReleaseText += "This is a $ReleaseType release and should not be widley deployed in production.`n`n"
}

if (-NOT $AutoUpgradeWorking)
{
    $ReleaseText += "Note: Automatic upgrades will not work for this release. Please manually uninstall existing versions of DPC prior to installing this version.`n`n"
}

$ReleaseText += @"
Check out https://aovpndpc.com/#community for access to the community

Contact DPC@darcy.org.uk if you're interested in Commercial Support
"@

#Export values back to the pipeline
$delimiter = "EOF_$([guid]::NewGuid().ToString())" #Use a unique value to avoid EOF being picked up from the variable itself
Add-Content -Path $env:GITHUB_OUTPUT -Value "releaseText<<$delimiter"
Add-Content -Path $env:GITHUB_OUTPUT -Value $ReleaseText
Add-Content -Path $env:GITHUB_OUTPUT -Value $delimiter