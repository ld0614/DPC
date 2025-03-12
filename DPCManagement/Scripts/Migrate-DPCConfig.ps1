
#Description
#Migrates settings from PowerON DPC to DPC Version 5+
param
(
    [Parameter(Mandatory)]
    [String]
    $PreviousGPOName,
    [Parameter(Mandatory)]
    [String]
    $NewGPOName
)

#Please run on a Domain Controller or another server with the Group Policy Management RSAT Tools installed
Import-Module -Name GroupPolicy -ErrorAction Stop

#Check that GPO Exists
Get-GPO -Name $PreviousGPOName -ErrorAction Stop | Out-Null
Get-GPO -Name $NewGPOName -ErrorAction Stop | Out-Null

Function Copy-GPORegistryKey
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]
        $KeyPath,
        [Parameter(Mandatory)]
        [String]
        $PreviousGPOName,
        [Parameter(Mandatory)]
        [String]
        $NewGPOName,
        [Parameter()]
        [String]
        $Indent
    )

    $AllValues = Get-GPRegistryValue -Name $PreviousGPOName -Key $KeyPath

    foreach ($Value in $AllValues)
    {
        if ($null -eq $Value.Type)
        {
            #Empty type means that this value is a subkey
            Write-Output "$($Indent)Copying Settings for $($Value.KeyPath.Split("\")[-1])"
            Copy-GPORegistryKey -KeyPath $Value.FullKeyPath -PreviousGPOName $PreviousGPOName -NewGPOName $NewGPOName -Indent ($Indent + "`t")
            continue
        }

        if ($Value.ValueName -eq "ProductKey")
        {
            Write-Output "Skipping ProductKey as this is no longer needed"
            continue
        }

        if ($Value.ValueName -eq "MonitorVPN")
        {
            Write-Output "Skipping MonitorVPN as this is now always on"
            continue
        }

        Write-Output "$($Indent)Copying over $($Value.ValueName)"

        if ($Value.Value.GetType() -ne [int])
        {
            $Data = $Value.Value.Replace("`0","") 
        }
        else
        {
            $Data = $Value.Value
        }

        Set-GPRegistryValue -Name $NewGPOName -Value $Data -ValueName $Value.ValueName -Type $Value.Type -Key $Value.FullKeyPath.Replace("PowerONPlatforms\AOVPNDPCClient","DPC\DPCClient") | Out-Null
    }
}

Copy-GPORegistryKey -KeyPath "HKEY_LOCAL_MACHINE\Software\Policies\PowerONPlatforms\AOVPNDPCClient" -PreviousGPOName $PreviousGPOName -NewGPOName $NewGPOName

Write-Output "Script Complete"
