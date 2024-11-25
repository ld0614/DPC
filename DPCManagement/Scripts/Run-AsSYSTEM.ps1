$PSExecPath = "C:\Program Files\WindowsApps\Microsoft.SysinternalsSuite_2024.6.1.0_x64__8wekyb3d8bbwe\Tools\PsExec.exe"
$VSPath = "C:\Program Files\Microsoft Visual Studio\2022\enterprise"
$MSBuildpath = "$VSPath\MSBuild\Current\Bin\msbuild.exe"
$SolutionRootPath = "C:\source\AOVPN DPC"
$Project = "DPCService"

if(-NOT (Test-path -Path $PSExecPath))
{
    Throw "Unable to Locate PSExec.exe"
}

if (-NOT ([Security.Principal.WindowsIdentity]::GetCurrent().Groups -contains 'S-1-5-32-544'))
{
    Throw "Must run script as Admin"
}

if(-NOT (Test-path -Path $MSBuildpath))
{
    Throw "Unable to Locate msbuild.exe"
}

$Service = Get-Service -Name DPCService -ErrorAction SilentlyContinue
if ($null -ne $Service -and $Service.Status -ne "Stopped")
{
    Write-Warning "DPC Service in State $($Service.Status) Stopping Installed DPC as it will conflict with tests"
    Stop-Service -Name DPCService -ErrorAction Stop
}

. $MSBuildpath "$SolutionRootPath\AOVPNDPC.sln" "/t:$($Project):Rebuild" /property:Configuration=SysAttach /property:Platform=x64 /property:RuntimeIdentifier=win-x64
Start-Process PowerShell -ArgumentList "-NoProfile -WindowStyle Normal -NoExit -Command `". '$PSExecPath' -accepteula -s '$SolutionRootPath\$Project\bin\x64\sysattach\$Project.exe'`""