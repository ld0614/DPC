
#Description
#Migrates settings from PowerON DPC to DPC Version 5+
param
(
    [Parameter(Mandatory)]
    [String]
    $GPOName
)

#Please run on a Domain Controller or another server with the Group Policy Management RSAT Tools installed
Import-Module -Name GroupPolicy -ErrorAction Stop

#Check that GPO Exists
Get-GPO -Name $GPOName -ErrorAction Stop | Out-Null

Function Copy-GPORegistryKey
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]
        $KeyPath,
        [Parameter(Mandatory)]
        [String]
        $GPOName,
        [Parameter()]
        [String]
        $Indent
    )

    $AllValues = Get-GPRegistryValue -Name $GPOName -Key $KeyPath

    foreach ($Value in $AllValues)
    {
        if ($null -eq $Value.Type)
        {
            #Empty type means that this value is a subkey
            Write-Output "$($Indent)Checking Settings for $($Value.KeyPath.Split("\")[-1])"
            Copy-GPORegistryKey -KeyPath $Value.FullKeyPath -GPOName $GPOName -Indent ($Indent + "`t")
            continue
        }

        Write-Output "$($Indent)Checking $($Value.ValueName)"

        if ($Value.Value.GetType() -ne [int])
        {
            $Data = $Value.Value.Replace("`0","") 
        }
        else
        {
            $Data = $Value.Value
        }

        Set-GPRegistryValue -Name $GPOName -Value $Data -ValueName $Value.ValueName -Type $Value.Type -Key $Value.FullKeyPath | Out-Null
    }
}

Copy-GPORegistryKey -KeyPath "HKEY_LOCAL_MACHINE\Software\Policies\DPC\DPCClient" -GPOName $GPOName

Write-Output "Script Complete"
