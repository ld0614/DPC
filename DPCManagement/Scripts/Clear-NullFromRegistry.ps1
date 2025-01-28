$RegList = @("HKLM:\SOFTWARE\Policies\DPC\DPCClient","HKLM:\SOFTWARE\DPC\DPCClient")

Function Clear-NullChars
{
    param(
        [string]
        $RegKey
    )
    Write-Output "Checking $RegKey"
    $RegKey = $RegKey.Replace("HKEY_LOCAL_MACHINE","HKLM:")

    $Item = Get-Item -Path $RegKey

    foreach ($Value in $Item.Property)
    {
        Write-Output "Checking $Value"
        $Data = Get-ItemPropertyValue -Path $RegKey -Name $Value
        if ($Data.GetType() -eq [int]) {continue}
        $SanitisedData = $Data.Replace("`0","")
        if ($Data.Length -ne $SanitisedData.Length) #Null is not compared but does change the length of the string
        {
            Write-Warning "Null Found, Fixing..."
            Set-ItemProperty -Path $RegKey -Name $Value -Value $SanitisedData
        }
    }

    $SubKeys = Get-ChildItem -Path $RegKey
    foreach ($Key in $SubKeys)
    {
        Clear-NullChars -RegKey $Key.Name
    }
}

foreach ($RegKey in $RegList)
{
    Clear-NullChars -RegKey $RegKey
}