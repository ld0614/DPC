
#Description
#Gathers as much data about the DPC Agent and System State as possible to assist with troubleshooting DPC related issues

Param (
    [Parameter()]
    [string]
    $FileName="DPCLog",
    [Switch]
    $DebugLogs
)

$SavePath = Join-Path -Path $env:TEMP -ChildPath "DPCLog"

Function Format-XML([string]$xmlContent)
{
    $xml = New-Object -TypeName System.Xml.XmlDocument
    $xml.LoadXml($xmlContent)
    $sw = New-Object System.IO.StringWriter
    $xmlWriter = New-Object System.Xml.XmlTextwriter($sw)
    $xmlWriter.Formatting = [System.XML.Formatting]::Indented
    $xml.WriteContentTo($xmlWriter)
    return $sw.ToString()
}

try
{
    Start-Transcript -Path (Join-Path -Path $env:TEMP -ChildPath "DPCLog.log")

    if ((Test-Path -Path $SavePath) -and -not [string]::IsNullOrWhiteSpace($SavePath))
    {
        Write-Output "Remove old Log Temp Path"
        Remove-Item -Path $SavePath -Recurse -Force
    }

    New-Item -Path $SavePath -ItemType Directory | Out-Null

    #Create Directories to avoid errors with saving
    New-Item -Path (Join-Path -Path $SavePath -ChildPath "Settings") -ItemType Directory | Out-Null
    New-Item -Path (Join-Path -Path $SavePath -ChildPath "RasMan") -ItemType Directory | Out-Null
    New-Item -Path (Join-Path -Path $SavePath -ChildPath "Connections") -ItemType Directory | Out-Null
    New-Item -Path (Join-Path -Path $SavePath -ChildPath "EventLogs") -ItemType Directory | Out-Null
    New-Item -Path (Join-Path -Path $SavePath -ChildPath "PBKFiles") -ItemType Directory | Out-Null
    New-Item -Path (Join-Path -Path $SavePath -ChildPath "Profiles") -ItemType Directory | Out-Null
    New-Item -Path (Join-Path -Path $SavePath -ChildPath "SystemInfo") -ItemType Directory | Out-Null
    New-Item -Path (Join-Path -Path $SavePath -ChildPath "Certificates") -ItemType Directory | Out-Null

    Start-Transcript -Path (Join-Path -Path $SavePath -ChildPath "DPCLog.log")

    if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
    {
        throw "Script must be run as an administrator"
    }

    $ZipPath = Join-Path -Path $env:TEMP -ChildPath "$FileName.zip"

    Write-Output "Getting Hostname"
    $env:COMPUTERNAME | Out-File (Join-Path -Path $SavePath -ChildPath "SystemInfo\HostName.txt")

    Write-Output "Getting DPC Service Details"
    sc.exe qc DPCService | Out-File (Join-Path -Path $SavePath -ChildPath "SystemInfo\ServiceDetails.txt")

    Write-Output "Getting Operating System Information"
    Get-CimInstance -ClassName Win32_OperatingSystem | Format-List -Property * | Out-File (Join-Path -Path $SavePath -ChildPath "SystemInfo\OSInfo.txt")

    Write-Output "Getting System Information"
    systeminfo | Out-File (Join-Path -Path $SavePath -ChildPath "SystemInfo\SysInfo.txt")

    Write-Output "Getting DsRegCmd Status Information"
    dsregcmd.exe /status | Out-File (Join-Path -Path $SavePath -ChildPath "SystemInfo\DsRegCmdStatus.txt")

    Write-Output "Getting OS Version"
    cmd /c ver | Out-File (Join-Path -Path $SavePath -ChildPath "SystemInfo\WindowsVersion.txt")

    Write-Output "Getting RasMan Configuration Settings"
    reg export HKLM\SYSTEM\CurrentControlSet\Services\RasMan\Config (Join-Path -Path $SavePath -ChildPath "RasMan\RasManConfig.txt")

    Write-Output "Getting RasMan Device Tunnel Settings"
    reg export HKLM\SYSTEM\CurrentControlSet\Services\RasMan\DeviceTunnel (Join-Path -Path $SavePath -ChildPath "RasMan\RasManDeviceTunnel.txt")

    Write-Output "Getting RasMan Parameters Settings"
    reg export HKLM\SYSTEM\CurrentControlSet\Services\RasMan\Parameters (Join-Path -Path $SavePath -ChildPath "RasMan\RasManParams.txt")

    Write-Output "Getting GPO Settings"
    if ((Test-Path -Path "HKLM:\SOFTWARE\Policies\DPC\DPCClient"))
    {
        reg export HKLM\SOFTWARE\Policies\DPC\DPCClient (Join-Path -Path $SavePath -ChildPath "Settings\Policy.txt")
    }
    else
    {
        Write-Warning "No GPO Settings have been applied"
    }

    Write-Output "Getting DPC Internal Settings"

    if ((Test-Path -Path "HKLM:\SOFTWARE\DPC\DPCClient"))
    {
        reg export HKLM\SOFTWARE\DPC\DPCClient (Join-Path -Path $SavePath -ChildPath "Settings\Internal.txt")
    }
    else
    {
        Write-Warning "No Internal Settings have been applied"
    }

    Write-Output "Getting Machine Certificate Details"
    $LocalCerts = Get-ChildItem -Path Cert:\LocalMachine\My
    $LocalCerts | Select-Object -Property Thumbprint, Subject, EnhancedKeyUsageList, NotAfter, NotBefore, HasPrivateKey, Issuer, FriendlyName, SerialNumber | ConvertTo-Csv -NoTypeInformation | Out-File (Join-Path -Path $SavePath -ChildPath "Certificates\LocalMachine.csv")

    Write-Output "Getting Current User Certificate Details"
    $UserCerts = Get-ChildItem -Path Cert:\CurrentUser\My
    $UserCerts | Select-Object -Property Thumbprint, Subject, EnhancedKeyUsageList, NotAfter, NotBefore, HasPrivateKey, Issuer, FriendlyName, SerialNumber | ConvertTo-Csv -NoTypeInformation | Out-File (Join-Path -Path $SavePath -ChildPath "Certificates\LocalUser.csv")

    Write-Output "Getting Existing User Connections"
    Get-VpnConnection | ConvertTo-Json -Depth 99 | Out-File (Join-Path -Path $SavePath -ChildPath "Connections\UserConnections.json")

    $Connections = Get-VpnConnection
    foreach ($Connection in $Connections)
    {
        Format-XML -xmlContent $Connection.CimInstanceProperties["VpnConfigurationXml"].Value | Out-File (Join-Path -Path $SavePath -ChildPath "Connections\$($Connection.Name)-VpnConfigurationXml.xml")
    }

    Write-Output "Getting Existing Device Connections"
    Get-VpnConnection -AllUserConnection | ConvertTo-Json -Depth 99 | Out-File (Join-Path -Path $SavePath -ChildPath "Connections\DeviceConnections.json")

    $Connections = Get-VpnConnection -AllUserConnection
    foreach ($Connection in $Connections)
    {
        Format-XML -xmlContent $Connection.CimInstanceProperties["VpnConfigurationXml"].Value | Out-File (Join-Path -Path $SavePath -ChildPath "Connections\$($Connection.Name)-VpnConfigurationXml.xml")
    }

    if (Test-Path -Path (Join-Path -Path $env:ALLUSERSPROFILE -ChildPath "Microsoft\Network\Connections\Pbk"))
    {
        Write-Output "Getting Device PBK Files"
        New-Item -Path (Join-Path -Path $SavePath -ChildPath "PBKFiles\ProgramData") -ItemType Directory | Out-Null
        Copy-Item -Path (Join-Path -Path $env:ALLUSERSPROFILE -ChildPath "Microsoft\Network\Connections\Pbk") -Destination (Join-Path -Path $SavePath -ChildPath "PBKFiles\ProgramData") -Recurse
    }

    $Drive = $env:HOMEDRIVE

    if ([string]::IsNullOrWhiteSpace($Drive))
    {
        Write-Warning "Unable to locate HomeDrive, defaulting to C:"
        $Drive = "C:"
    }

    if (-Not $Drive.Contains(":"))
    {
        $Drive = $Drive + ":"
    }

    $UserDir = $Env:USERPROFILE

    $UserDirectories = Split-Path -Path $UserDir -Parent
    foreach ($UserDirectory in $UserDirectories)
    {
        $UserPBKLocation = Join-Path -Path $UserDirectory.fullName -ChildPath "AppData\Roaming\Microsoft\Network\Connections\Pbk"
        $UserName = $UserDirectory.Name
        if (Test-Path -Path $UserPBKLocation)
        {
            Write-Output "Getting $UserName PBK Files"
            New-Item -Path (Join-Path -Path $SavePath -ChildPath "PBKFiles\$UserName") -ItemType Directory | Out-Null
            Copy-Item -Path $UserPBKLocation -Destination (Join-Path -Path $SavePath -ChildPath "PBKFiles\$UserName") -Recurse
        }
    }

    Write-Output "Overwriting Profile Save Settings"
    if ((Test-Path -Path "HKLM:\SOFTWARE\Policies\DPC\DPCClient\MachineTunnel"))
    {
        Write-Output "Setting Machine Tunnel Output Locations"
        New-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\MachineTunnel -Name DebugPath -Value (Join-Path -Path $SavePath -ChildPath "Profiles\MachineTunnel-Debug.xml") -Force | Out-Null
        New-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\MachineTunnel -Name SavePath -Value (Join-Path -Path $SavePath -ChildPath "Profiles\MachineTunnel-Export.xml") -Force | Out-Null
    }

    if ((Test-Path -Path "HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserTunnel"))
    {
        Write-Output "Setting User Tunnel Output Locations"
        New-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserTunnel -Name DebugPath -Value (Join-Path -Path $SavePath -ChildPath "Profiles\UserTunnel-Debug.xml") -Force | Out-Null
        New-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserTunnel -Name SavePath -Value (Join-Path -Path $SavePath -ChildPath "Profiles\UserTunnel-Export.xml") -Force | Out-Null
    }

    if ((Test-Path -Path "HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserBackupTunnel"))
    {
        Write-Output "Setting User Backup Tunnel Output Locations"
        New-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserBackupTunnel -Name DebugPath -Value (Join-Path -Path $SavePath -ChildPath "Profiles\UserBackupTunnel-Debug.xml") -Force | Out-Null
        New-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserBackupTunnel -Name SavePath -Value (Join-Path -Path $SavePath -ChildPath "Profiles\UserBackupTunnel-Export.xml") -Force | Out-Null
    }

    if ($DebugLogs)
    {
        Write-Output "Enabling Debug Logs"
        wevtutil clear-log "DPC-AOVPN-DPCService/Debug"
        wevtutil clear-log "DPC-AOVPN-DPCService/Analytic"

        wevtutil set-log "DPC-AOVPN-DPCService/Debug" /enabled /quiet
        wevtutil set-log "DPC-AOVPN-DPCService/Analytic" /enabled /quiet

        Write-Output "Setting Refresh Interval to 1 Minute"
        New-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient -Name RefreshPeriod -Value 1 -Force | Out-Null
    }

    Write-Output "Restarting Agent"
    Restart-Service DPCService

    if (-NOT $DebugLogs)
    {
        Write-Output "Waiting 60 seconds for DPC Service to initialize"
        Start-Sleep -Seconds 60
    }
    else
    {
        Write-Output "Waiting 5 minutes for DPC Service to initialize and complete profile refresh"
        Start-Sleep -Seconds 300
    }

    Write-Output "Exporting Event Logs"
    wevtutil export-log "DPC-AOVPN-DPCService/Admin" (Join-Path -Path $SavePath -ChildPath "EventLogs\Admin.evtx")
    wevtutil export-log "DPC-AOVPN-DPCService/Operational" (Join-Path -Path $SavePath -ChildPath "EventLogs\Operational.evtx")
    if ($DebugLogs)
    {
        wevtutil export-log "DPC-AOVPN-DPCService/Debug" (Join-Path -Path $SavePath -ChildPath "EventLogs\Debug.evtx")
        wevtutil export-log "DPC-AOVPN-DPCService/Analytic" (Join-Path -Path $SavePath -ChildPath "EventLogs\Analytic.evtx")
    }

    Write-Output "Checking for Memory Dumps"
    $MemoryDumpList = @()
    $MemoryDumpList += Get-ChildItem -Path (Join-Path -Path $env:SystemDrive -ChildPath "Windows\Temp") -Filter DPCService-*.dmp
    if ($MemoryDumpList.Count -gt 0)
    {
        Write-Output "Copying Memory Dumps"

        New-Item -Path (Join-Path -Path $SavePath -ChildPath "MemoryDumps") -ItemType Directory | Out-Null

        foreach ($MemoryDump in $MemoryDumpList)
        {
            Copy-Item -Path $MemoryDump.FullName -Destination (Join-Path -Path $SavePath -ChildPath "MemoryDumps\$($MemoryDump.Name)")
        }
    }
    else
    {
        Write-Output "No Memory Dumps Found"
    }

    Write-Output "Resettings Profile Save Settings"
    if ((Test-Path -Path "HKLM:\SOFTWARE\Policies\DPC\DPCClient\MachineTunnel"))
    {
        Write-Output "Reseting Machine Tunnel Output Locations"
        Remove-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\MachineTunnel -Name DebugPath -Force | Out-Null
        Remove-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\MachineTunnel -Name SavePath -Force | Out-Null
    }
    if ((Test-Path -Path "HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserTunnel"))
    {
        Write-Output "Reseting User Tunnel Output Locations"
        Remove-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserTunnel -Name DebugPath -Force | Out-Null
        Remove-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserTunnel -Name SavePath -Force | Out-Null
    }
    if ((Test-Path -Path "HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserBackupTunnel"))
    {
        Write-Output "Reseting User Backup Tunnel Output Locations"
        Remove-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserBackupTunnel -Name DebugPath -Force | Out-Null
        Remove-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient\UserBackupTunnel -Name SavePath -Force | Out-Null
    }

    if ($DebugLogs)
    {
        Write-Output "Resetting Profile Refresh Interval"
        Remove-ItemProperty -Path HKLM:\SOFTWARE\Policies\DPC\DPCClient -Name RefreshPeriod -Force | Out-Null

        Write-Output "Disabling Debug Logs"
        wevtutil set-log "DPC-AOVPN-DPCService/Debug" /enabled:false /quiet
        wevtutil set-log "DPC-AOVPN-DPCService/Analytic" /enabled:false /quiet

        wevtutil clear-log "DPC-AOVPN-DPCService/Debug"
        wevtutil clear-log "DPC-AOVPN-DPCService/Analytic"
    }

    Write-Output "Updating GPO Settings (GPUpdate /target:Computer /Force) This may take some time..."
    gpupdate /target:Computer /force

    Write-Output "Restarting Agent"
    Restart-Service DPCService

    Write-Output "Compressing Results"
    Stop-Transcript
    Compress-Archive -Path $SavePath -DestinationPath $ZipPath -Force

    Write-Output "Log file located at $ZipPath"

    Write-Output "Please provide $ZipPath to the DPC Development Team through your preferred file sharing process."
}
finally
{
    Stop-Transcript
}