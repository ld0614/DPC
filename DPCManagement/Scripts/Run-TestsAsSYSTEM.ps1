$PSExecPath = "C:\Program Files\WindowsApps\Microsoft.SysinternalsSuite_2024.7.0.0_x64__8wekyb3d8bbwe\Tools\PsExec.exe"
$VSPath = "C:\Program Files\Microsoft Visual Studio\2022\Community"
$VSTestPath = "$VSPath\Common7\IDE\Extensions\TestPlatform\vstest.console.exe"
$MSBuildpath = "$VSPath\MSBuild\Current\Bin\msbuild.exe"
$SolutionRootPath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$SleepBlockingProtection = $false

if(-NOT (Test-path -Path $PSExecPath))
{
    Throw "Unable to Locate PSExec.exe"
}

if (-NOT ([Security.Principal.WindowsIdentity]::GetCurrent().Groups -contains 'S-1-5-32-544'))
{
    Throw "Must run script as Admin"
}

if(-NOT (Test-path -Path $VSTestPath))
{
    Throw "Unable to Locate vstest.console.exe"
}

if(-NOT (Test-path -Path $MSBuildpath))
{
    Throw "Unable to Locate msbuild.exe"
}

if(-NOT (Test-path -Path $SolutionRootPath))
{
    Throw "Unable to Locate Solution Root Path"
}

$Service = Get-Service -Name DPCService -ErrorAction SilentlyContinue
if ($null -ne $Service -and $Service.Status -ne "Stopped")
{
    Write-Warning "DPC Service in State $($Service.Status) Stopping Installed DPC as it will conflict with test profiles"
    Stop-Service -Name DPCService -ErrorAction Stop
}

#On Windows 10 exclude Windows 11 and vice versa
$OSVersion = (Get-WMIObject win32_operatingsystem).Caption
if ($OSVersion -like "*Windows 11*")
{
    $ExcludeVersionCategory = "Windows10"
}
else
{
    $ExcludeVersionCategory = "Windows11"
}

if (-NOT [Environment]::GetEnvironmentVariables("Machine").Fakes_Use_V2_DataCollector)
{
    [Environment]::SetEnvironmentVariable("Fakes_Use_V2_DataCollector", "true", "Machine")
    Throw "Fakes_Use_V2_DataCollector configured, please restart PowerShell ISE"
}

if ($SleepBlockingProtection)
{
    Write-Output "Launching Sleep Blocking, close to re-enable sleep"
    Start-Process PowerShell -ArgumentList "-NoProfile -WindowStyle Normal -NoExit -Command `"Clear-Host; Write-Warning 'Your PC will not go to sleep whilst this window is open...'; Do {[void][System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms'); [System.Windows.Forms.SendKeys]::SendWait(`"{PRTSC}`"); Start-Sleep -Seconds 2 } While (`$true) `""
}

Write-Output "Clearing previous Test Builds"
Remove-Item -Path "$SolutionRootPath\*Tests\bin\" -Recurse

. $MSBuildpath "$SolutionRootPath\AOVPNDPC.sln" "/t:restore;rebuild" /property:Configuration=Debug /property:Platform=x64 /property:RuntimeIdentifier=win-x64
#. $PSExecPath -accepteula -s $VSTestPath "$SolutionRootPath\*Tests\bin\x64\*\*Tests.dll" /TestCaseFilter:"TestCategory=TrafficFilters" #--Tests:BasicUserProfileWithTrafficFilters
#. $PSExecPath -accepteula -s $VSTestPath "$SolutionRootPath\*Tests\bin\x64\*\*Tests.dll" /TestCaseFilter:"TestCategory=OverrideProfile" #--Tests:BasicUserProfileWithTrafficFilters
#. $PSExecPath -accepteula -s $VSTestPath "$SolutionRootPath\*Tests\bin\x64\*\*Tests.dll" /TestCaseFilter:"TestCategory!=TrafficFilters" #--Tests:BasicUserProfileWithTrafficFilters
#. $PSExecPath -accepteula -s $VSTestPath "$SolutionRootPath\*Tests\bin\x64\*\*Tests.dll" /TestCaseFilter:"TestCategory=MachineTunnel" --Tests:OverrideMachineProfile
#. $PSExecPath -accepteula -s $VSTestPath "$SolutionRootPath\*Tests\bin\x64\*\*Tests.dll" --Tests:BasicUserProfileWithWildcardDNSSuffixList
#. $PSExecPath -accepteula -s $VSTestPath "$SolutionRootPath\*Tests\bin\x64\*\*Tests.dll" --Tests:BasicMachineDebugSave
#. $PSExecPath -accepteula -s $VSTestPath "$SolutionRootPath\*Tests\bin\x64\*\*Tests.dll" --Tests:UpdateMachineEnableThenDisableEKU
#. $PSExecPath -accepteula -s $VSTestPath "$SolutionRootPath\*Tests\bin\x64\*\*Tests.dll" /TestCaseFilter:"TestCategory!=$ExcludeVersionCategory & TestCategory!=MachineTunnel" #All User Tests
#. $PSExecPath -accepteula -s $VSTestPath "$SolutionRootPath\*Tests\bin\x64\*\*Tests.dll" /TestCaseFilter:"TestCategory!=$ExcludeVersionCategory & TestCategory=MachineTunnel" #All Machine Tests

#All Tests for version
. $PSExecPath -accepteula -s $VSTestPath "$SolutionRootPath\*Tests\bin\x64\*\*Tests.dll" /TestCaseFilter:"TestCategory!=$ExcludeVersionCategory"