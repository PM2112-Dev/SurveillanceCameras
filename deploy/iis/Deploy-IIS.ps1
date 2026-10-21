#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Triển khai bản publish của src/Web lên IIS (app pool + site), và tuỳ chọn trỏ ngrok service sang site này.

.EXAMPLE
    dotnet publish src\Web\Web.csproj -c Release -o C:\temp\story-publish
    .\deploy\iis\Deploy-IIS.ps1 -PublishDir C:\temp\story-publish

.NOTES
    Thư mục data\, logs\ và file appsettings.Production.json trong thư mục site được giữ nguyên giữa các lần deploy.
#>
param(
    [Parameter(Mandatory)][string]$PublishDir,
    [string]$SiteName = 'StorySystem',
    [int]$Port = 8090,
    [string]$SitePath = "C:\inetpub\$SiteName",
    # Chỉ copy vào site ở lần đầu (khi site chưa có appsettings.Production.json).
    [string]$ProductionSettings,
    # Nếu truyền: sửa upstream của endpoint trong ngrok.yml sang cổng site rồi restart service ngrok.
    [string]$NgrokConfig,
    [string]$NgrokEndpointName,
    # Dưới IIS cửa sổ Chrome đăng nhập không hiện ra màn hình: account N mở cổng debug (base + N) để đăng nhập
    # qua chrome://inspect trên máy này. 0 = tắt.
    [int]$BrowserDebugPortBase = 9222,
    [string]$LogFile
)

$ErrorActionPreference = 'Stop'
if ($LogFile) { Start-Transcript -Path $LogFile -Force | Out-Null }

try {
    Import-Module WebAdministration

    if (-not (Test-Path (Join-Path $PublishDir 'SurveillanceCameras.Web.dll'))) {
        throw "Không thấy SurveillanceCameras.Web.dll trong '$PublishDir' - hãy dotnet publish trước."
    }

    # --- App pool ---
    $poolPath = "IIS:\AppPools\$SiteName"
    if (-not (Test-Path $poolPath)) {
        New-WebAppPool -Name $SiteName | Out-Null
        Write-Host "Đã tạo app pool $SiteName"
    }
    Set-ItemProperty $poolPath -Name managedRuntimeVersion -Value ''
    # Chrome (Playwright) từ chối mở kênh điều khiển nếu tiến trình không có user profile / %LOCALAPPDATA%.
    $appcmd = "$env:windir\System32\inetsrv\appcmd.exe"
    & $appcmd set apppool $SiteName /processModel.loadUserProfile:true /processModel.setProfileEnvironment:true | Out-Host
    # App có background service (Kafka consumer, đồng bộ hằng ngày) nên không để IIS tắt khi rảnh.
    # Dùng appcmd: Set-ItemProperty với tên lồng "processModel.idleTimeout" ném NullReferenceException trên một số bản IIS.
    & $appcmd set apppool $SiteName /processModel.idleTimeout:00:00:00 /startMode:AlwaysRunning | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "appcmd set apppool (idleTimeout/startMode) lỗi (exit $LASTEXITCODE)" }

    if ((Get-WebAppPoolState -Name $SiteName).Value -ne 'Stopped') {
        Stop-WebAppPool -Name $SiteName
        $deadline = (Get-Date).AddSeconds(30)
        while ((Get-WebAppPoolState -Name $SiteName).Value -ne 'Stopped' -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }
    }

    # Chrome đăng nhập còn sót lại sau khi pool dừng sẽ giữ khoá profile, lần mở sau không điều khiển được.
    Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" |
        Where-Object { $_.CommandLine -like "*--user-data-dir=$SitePath\*" } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

    # --- Copy file ---
    New-Item -ItemType Directory -Force -Path $SitePath, "$SitePath\data", "$SitePath\logs" | Out-Null
    robocopy $PublishDir $SitePath /MIR /XD data logs /XF appsettings.Production.json /NFL /NDL /NP /R:5 /W:2 | Out-Host
    if ($LASTEXITCODE -ge 8) { throw "robocopy lỗi (exit $LASTEXITCODE)" }

    $prodTarget = Join-Path $SitePath 'appsettings.Production.json'
    if ($ProductionSettings -and -not (Test-Path $prodTarget)) {
        Copy-Item $ProductionSettings $prodTarget
        Write-Host "Đã copy appsettings.Production.json"
    }
    if (-not (Test-Path $prodTarget)) {
        Write-Warning "Site chưa có appsettings.Production.json (connection string, Jwt:SigningKey)."
    }

    # --- web.config: môi trường + tin header X-Forwarded-* của ngrok + bật stdout log ---
    $webConfigPath = Join-Path $SitePath 'web.config'
    [xml]$webConfig = Get-Content $webConfigPath -Raw
    $aspNetCore = $webConfig.SelectSingleNode('//aspNetCore')
    $aspNetCore.SetAttribute('stdoutLogEnabled', 'true')
    $aspNetCore.SetAttribute('stdoutLogFile', '.\logs\stdout')
    $envVars = $aspNetCore.SelectSingleNode('environmentVariables')
    if (-not $envVars) { $envVars = $aspNetCore.AppendChild($webConfig.CreateElement('environmentVariables')) }
    $environment = @{
        ASPNETCORE_ENVIRONMENT                  = 'Production'
        ASPNETCORE_FORWARDEDHEADERS_ENABLED     = 'true'
        AccountSession__RemoteDebuggingPortBase = "$BrowserDebugPortBase"
    }
    foreach ($pair in $environment.GetEnumerator()) {
        $node = $envVars.SelectSingleNode("environmentVariable[@name='$($pair.Key)']")
        if (-not $node) { $node = $envVars.AppendChild($webConfig.CreateElement('environmentVariable')) }
        $node.SetAttribute('name', $pair.Key)
        $node.SetAttribute('value', $pair.Value)
    }
    $webConfig.Save($webConfigPath)

    # --- Quyền thư mục cho app pool identity ---
    $identity = "IIS AppPool\$SiteName"
    icacls $SitePath /grant "${identity}:(OI)(CI)RX" /Q | Out-Null
    icacls "$SitePath\data" /grant "${identity}:(OI)(CI)M" /Q | Out-Null
    icacls "$SitePath\logs" /grant "${identity}:(OI)(CI)M" /Q | Out-Null

    # --- Site ---
    if (-not (Get-Website -Name $SiteName)) {
        New-Website -Name $SiteName -Port $Port -PhysicalPath $SitePath -ApplicationPool $SiteName | Out-Null
        Write-Host "Đã tạo site $SiteName (http://*:$Port)"
    }
    else {
        Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $SitePath
        Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $SiteName
    }

    Start-WebAppPool -Name $SiteName
    Start-Website -Name $SiteName
    & $appcmd list apppool $SiteName /text:processModel.loadUserProfile | ForEach-Object { Write-Host "loadUserProfile=$_" }
    & $appcmd list apppool $SiteName /text:processModel.setProfileEnvironment | ForEach-Object { Write-Host "setProfileEnvironment=$_" }
    & $appcmd list apppool $SiteName /text:processModel.identityType | ForEach-Object { Write-Host "identityType=$_" }

    # --- Kiểm tra site trả lời (lần đầu còn chạy migration nên chờ lâu hơn) ---
    $ok = $false
    $deadline = (Get-Date).AddSeconds(120)
    while (-not $ok -and (Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest "http://localhost:$Port/" -UseBasicParsing -TimeoutSec 60
            $ok = $response.StatusCode -eq 200
        }
        catch { Start-Sleep -Seconds 3 }
    }
    if (-not $ok) { throw "Site http://localhost:$Port/ không trả 200 - xem $SitePath\logs\stdout_*.log" }
    Write-Host "Site OK: http://localhost:$Port/"

    # --- ngrok ---
    if ($NgrokConfig) {
        Copy-Item $NgrokConfig "$NgrokConfig.bak" -Force
        $yaml = Get-Content $NgrokConfig -Raw
        $yaml = [regex]::Replace($yaml, '(upstream:\s*\r?\n\s*url:\s*)\S+', "`${1}$Port")
        if ($NgrokEndpointName) { $yaml = [regex]::Replace($yaml, '(- name:\s*)\S+', "`${1}$NgrokEndpointName") }
        [IO.File]::WriteAllText($NgrokConfig, $yaml)
        Restart-Service ngrok
        Write-Host "Đã trỏ ngrok sang cổng $Port và restart service (bản cũ: $NgrokConfig.bak)"
    }

    Write-Host 'DEPLOY_OK'
}
catch {
    Write-Host "DEPLOY_FAILED: $_"
    throw
}
finally {
    if ($LogFile) { Stop-Transcript | Out-Null }
}
