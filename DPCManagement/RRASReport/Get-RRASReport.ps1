<#
.SYNOPSIS
    Gather RRAS client details from multiple servers, generate HTML and publish to IIS.

.DESCRIPTION
    - Collects active RRAS VPN session details from multiple servers
    - Creates an HTML report with collapsible sections grouped by server
    - Saves report to IIS folder for web access
    - Scheduled task can run this script every 15 minutes

.NOTES
    Requires RemoteAccess module on RRAS servers and WinRM access
#>

# ======== CONFIGURATION ========
# RRAS servers to query
$RRASServers = @("Host1.Domain", "Host2.Domain")   # Replace with actual server names

# IIS Web Folder for publishing report
$IISPath = "C:\inetpub\wwwroot"

# Create IIS folder if missing
if (!(Test-Path $IISPath)) {
    New-Item -ItemType Directory -Path $IISPath | Out-Null
}

# File paths
$HtmlReport = Join-Path $IISPath "RRAS_Report.html"

# ======== SCRIPT START ========
$AllClientDetails = @()
$SummaryReport = @()

foreach ($Server in $RRASServers) {
    Write-Host "`nConnecting to $Server..." -ForegroundColor Cyan
    try {
        $ClientData = Invoke-Command -ComputerName $Server -ScriptBlock {
            Import-Module RemoteAccess
            Get-RemoteAccessConnectionStatistics
        } -ErrorAction Stop

        if ($ClientData) {
            $ClientCount = @($ClientData).Count
            $SummaryReport += [PSCustomObject]@{
                ServerName       = $Server
                ConnectedClients = $ClientCount
            }

            foreach ($Client in $ClientData) {
                # Format values for readability
                $formattedStart = ($Client.ConnectionStartTime).ToString("dd/MM/yyyy HH:mm")
                $duration = New-TimeSpan -Start $Client.ConnectionStartTime -End (Get-Date)
                if ($duration.Days -gt 0) {
                    $durationFormatted = "{0}d {1:hh:mm:ss}" -f $duration.Days, $duration
                }
                else {
                    $durationFormatted = "{0:hh:mm:ss}" -f $duration
                }

                $AllClientDetails += [PSCustomObject]@{
                    ServerName         = $Server
                    UserName           = ($Client.UserName -join ', ')
                    ClientIPAddress    = $Client.ClientIPAddress
                    ClientExternalIP   = $Client.ClientExternalAddress
                    TunnelType         = $Client.TunnelType
                    AuthMethod         = $Client.AuthMethod
                    ConnectionStart    = $formattedStart
                    ConnectionDuration = $durationFormatted
                    BytesInMB          = [math]::Round($Client.TotalBytesIn / 1MB, 2)
                    BytesOutMB         = [math]::Round($Client.TotalBytesOut / 1MB, 2)
                }
            }
        }
        else {
            Write-Host "No active clients on $Server" -ForegroundColor Yellow
            $SummaryReport += [PSCustomObject]@{
                ServerName       = $Server
                ConnectedClients = 0
            }
        }
    }
    catch {
        Write-Host "Error connecting to $Server : $_" -ForegroundColor Red
        $SummaryReport += [PSCustomObject]@{
            ServerName       = $Server
            ConnectedClients = "Error"
        }
    }
}

# ======== BUILD HTML REPORT ========
$style = @"
<style>
    body { font-family: Arial; margin: 20px; background-color: #f9f9f9; }
    h1 { color: #0a41cc; }
    h2 { color: #333; margin-top: 30px; }
    table { border-collapse: collapse; width: 100%; margin-bottom: 15px; }
    th, td { border: 1px solid #ccc; padding: 10px; text-align: left; }
    th {
        background-color: #004080;
        color: white;
        font-weight: bold;
        text-transform: capitalize;
    }
    tr:nth-child(even) { background-color: #f2f2f2; }
	tr:hover { background-color: #d0e4f5; }
    details { margin-bottom: 15px; }
    summary { font-size: 18px; font-weight: bold; cursor: pointer; color: #004080; }
</style>
"@

$searchBox = @"
<style>
#searchContainer {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    background-color: #f9f9f9;
    padding: 10px 20px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    z-index: 9999;
    display: flex;
    align-items: center;
    gap: 10px;
}
#searchBox {
    width: 300px;
    padding: 8px;
    font-size: 16px;
}
#clearBtn {
    padding: 8px 16px;
    font-size: 16px;
    background-color: #0078D7;
    color: white;
    border: none;
    border-radius: 5px;
    cursor: pointer;
}
#clearBtn:hover {
    background-color: #005A9E;
}
body {
    padding-top: 60px; /* space for fixed search bar */
}
mark {
    background-color: yellow;
}
</style>

<div id='searchContainer'>
    <input type='text' id='searchBox' placeholder='Search clients...'>
    <button id='clearBtn'>Clear</button>
</div>

<script>
// Cache original text content on page load
window.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('details table td').forEach(cell => {
        cell.setAttribute('data-original', cell.textContent);
    });
});

document.getElementById('searchBox').addEventListener('keyup', filterTables);
document.getElementById('clearBtn').addEventListener('click', function() {
    document.getElementById('searchBox').value = '';
    filterTables();
});

function filterTables() {
    var filter = document.getElementById('searchBox').value.toLowerCase();
    var detailsSections = document.querySelectorAll('details'); // Only client tables
    detailsSections.forEach(function(section) {
        var tables = section.getElementsByTagName('table');
        Array.prototype.forEach.call(tables, function(table) {
            var rows = table.getElementsByTagName('tr');
            for (var i = 1; i < rows.length; i++) { // skip header row
                var row = rows[i];
                // Use cached original cell text to check filter
                var rowText = Array.from(row.cells)
                    .map(c => c.getAttribute('data-original').toLowerCase())
                    .join(' ');
                if (rowText.indexOf(filter) > -1) {
                    row.style.display = '';
                    highlightRow(row, filter);
                } else {
                    row.style.display = 'none';
                }
            }
        });
    });
}

function highlightRow(row, filter) {
    if (!filter) {
        // Restore original text if no filter
        Array.from(row.cells).forEach(cell => {
            cell.innerHTML = cell.getAttribute('data-original');
        });
        return;
    }
    var regex = new RegExp('(' + filter.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&') + ')', 'gi');
    Array.from(row.cells).forEach(cell => {
        var originalText = cell.getAttribute('data-original');
        cell.innerHTML = originalText.replace(regex, '<mark>`$1</mark>');
    });
}
</script>
"@


$Footer = @"
<footer style='
    position: fixed;
    bottom: 0;
    left: 0;
    width: 100%;
    background-color: #f9f9f9;
    color: #333;
    text-align: center;
    padding: 8px;
    font-size: 14px;
    border-top: 1px solid #ccc;
'>
    &copy; $(Get-Date -Format yyyy) Company - Chris Griffin. All rights reserved.
</footer>
"@

# Page Title
$title = '<title> --- VPN Connection Report --- </title>'

# Auto-refresh every 2 minutes (120 seconds)
$refreshTag = '<meta http-equiv="refresh" content="120">'

# Convert Summary Report with Friendly Headers
$SummaryFriendly = $SummaryReport | Select-Object `
@{Name = 'Server'; Expression = { $_.ServerName } },
@{Name = 'Active Clients'; Expression = { $_.ConnectedClients } }

$SummaryTable = ($SummaryFriendly | ConvertTo-Html -PreContent "<h2>VPN Server Summary</h2>" -Fragment)

# Grouped client details with collapsible sections
$GroupedContent = ""
$Servers = $AllClientDetails | Group-Object ServerName
foreach ($group in $Servers) {
    $serverName = $group.Name
    $clientRows = $group.Group | ConvertTo-Html -Property UserName, ClientIPAddress, ClientExternalIP, TunnelType, AuthMethod, ConnectionStart, ConnectionDuration, BytesInMB, BytesOutMB -Fragment
    # Convert Client Details with Friendly Headers
    $ClientFriendly = $group.Group | Select-Object `
    @{Name = 'User Name'; Expression = { $_.UserName } },
    @{Name = 'IP Address'; Expression = { $_.ClientIPAddress } },
    @{Name = 'External IP Address'; Expression = { $_.ClientExternalIP } },
    @{Name = 'Tunnel Type'; Expression = { $_.TunnelType } },
    @{Name = 'Authentication'; Expression = { $_.AuthMethod } },
    @{Name = 'Start Time'; Expression = { $_.ConnectionStart } },
    @{Name = 'Duration'; Expression = { $_.ConnectionDuration } },
    @{Name = 'Data In (MB)'; Expression = { $_.BytesInMB } },
    @{Name = 'Data Out (MB)'; Expression = { $_.BytesOutMB } }

    $clientRows = $ClientFriendly | ConvertTo-Html -Fragment

    $GroupedContent += "<details open><summary>$serverName (Clients: $($group.Count))</summary>$clientRows</details>"
}

$Body = "<h1>RRAS VPN Connection Report</h1>$searchBox<p>Generated: $(Get-Date -Format 'dd/MM/yyyy HH:mm')</p>$SummaryTable<h2>Client Details</h2>$GroupedContent"
$Body += $Footer
$HtmlContent = ConvertTo-Html -Head "$style $refreshTag $title" -Body $Body

# Save HTML report to IIS path
$HtmlContent | Out-File $HtmlReport -Encoding UTF8

Write-Host "`nReports saved to IIS folder:"
Write-Host "- $HtmlReport "
