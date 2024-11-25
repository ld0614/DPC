
#Description
#Creates a DPC compatible Exclusion list in Group Policy based on a local or Internet hosted file

$GPOName = "Demo-DPC"
$FileName = "https://assets.zoom.us/docs/ipranges/ZoomMeetings.txt"
$Comment = "Zoom"
$ClearExistingRoutes = $true
$Tunnel = "MachineTunnel" #MachineTunnel, UserTunnel or UserBackupTunnel

#Check that GPO Exists
Get-GPO -Name $GPOName -ErrorAction Stop | Out-Null

if ($FileName.StartsWith("http://") -or $FileName.StartsWith("https://"))
{
    #File is internet based
    $IPList = (Invoke-WebRequest -Uri $FileName -UseBasicParsing -ErrorAction Stop).Content
}
else
{
    #Assume Local or remote file
    $IPList = Get-Content -Path $FileName -ErrorAction Stop
}

#Transform Data Here if Required, data needs to be in an array of CIDR format and should be IPv4 Only
$IPList = $IPList.Split("`n")

#Remove Duplicates
$IPList = Select-Object -InputObject $IPList -Unique

if ($ClearExistingRoutes)
{
    Write-Output "Clearing Existing Routes"
    #Clear existing Routes
    Remove-GPRegistryValue -Name $GPOName -Key "HKLM\SOFTWARE\Policies\DPC\DPCClient\$Tunnel\RouteListExclude" -ErrorAction SilentlyContinue | Out-Null #Throws error if not already configured
}

$ExistingList = Get-GPRegistryValue -Name $GPOName -Key "HKLM\SOFTWARE\Policies\DPC\DPCClient\$Tunnel\RouteListExclude" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty ValueName #Throws error if not already configured

foreach ($IP in $IPList)
{
    if ($ExistingList -notcontains $IP)
    {
        if (-NOT [string]::IsNullOrWhiteSpace($IP))
        {
            Write-Output "Adding $IP"
            Set-GPRegistryValue -Name $GPOName -Key "HKLM\SOFTWARE\Policies\DPC\DPCClient\$Tunnel\RouteListExclude" -ValueName $IP -Type String -Value $Comment | Out-Null
        }
    }
    else
    {
        Write-Output "$IP already Added"
    }
}

