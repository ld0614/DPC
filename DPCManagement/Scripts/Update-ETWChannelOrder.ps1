<#
    This script ensures that the ETW channels are in the correct order. It appears that sometimes the operational channel is not the first in the list. While in theory this shouldn't cause and issue,
    The windows event log viewer (Eventvwr.msc) ignores operational events if it is not the first channel in the list, even if all the attributes are correct. As we don't have control of the manifest
    Generator, and the generator hasn't been updated in a decade, using this script re-orders the XML into the correct order without needing to disrupt any other systems
#>

Param
(
    [Parameter(Mandatory)]
    [string]
    $TargetDir
)

$ManifestPath = Join-Path -Path $TargetDir -ChildPath "DPCService.DPC-AOVPN-DPCService.etwManifest.man"
$OriginalContent = Get-Content -Path $ManifestPath
[xml]$Manifest = $OriginalContent

$OldChannels = $Manifest.instrumentationManifest.instrumentation.events.provider.channels
$Tasks = $Manifest.instrumentationManifest.instrumentation.events.provider.tasks

#Locate the current channel information
$OperationalChannel = $Manifest.instrumentationManifest.instrumentation.events.provider.channels.channel | where chid -eq "Operational"
$AdminChannel = $Manifest.instrumentationManifest.instrumentation.events.provider.channels.channel | where chid -eq "Admin"
$DebugChannel = $Manifest.instrumentationManifest.instrumentation.events.provider.channels.channel | where chid -eq "Debug"
$AnalyticChannel = $Manifest.instrumentationManifest.instrumentation.events.provider.channels.channel | where chid -eq "Analytic"

if (($null -eq $OperationalChannel) -or ($null -eq $AdminChannel) -or ($null -eq $DebugChannel) -or ($null -eq $AnalyticChannel))
{
    throw "Something went wrong identifying standard channels"
}
   
#Create a new Element as the existing Element gets messed up when all the children are re-ordered
$Channels = $Manifest.CreateElement("channels")

#Set the channel order correctly
$Channels.AppendChild($OperationalChannel)
$Channels.AppendChild($DebugChannel)
$Channels.AppendChild($AdminChannel)
$Channels.AppendChild($AnalyticChannel)

#Remove the old and busted node
$Manifest.instrumentationManifest.instrumentation.events.provider.RemoveChild($OldChannels)

#Add the new node into the correct place within the XML Heiarachy
$Manifest.instrumentationManifest.instrumentation.events.provider.InsertBefore($Channels, $Tasks)

#Overwrite the original file
$Manifest.Save($ManifestPath)