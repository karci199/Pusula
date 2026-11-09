<#
.SYNOPSIS
    Pusula arayüz ve çekirdek senaryolarını çalıştırır.

.DESCRIPTION
    Her senaryo ayrı bir PowerShell sürecinde (STA) çalışır; pencereler ekran dışında açılır.
    Her senaryoya tests\Veri\PusulaDeneme'nin geçici bir kopyası verilir: kullanıcının verisine dokunulmaz,
    senaryolar birbirini etkilemez. %LOCALAPPDATA%\Pusula\ayarlar.json başta yedeklenir, sonda geri yazılır.

.PARAMETER Derle
    Önce çözümü derler (Visual Studio MSBuild, yoksa dotnet build).

.PARAMETER Filtre
    Yalnızca adı bu kalıba uyan senaryolar (ör. 'search', 'inv*').

.PARAMETER Yapilandirma
    Debug ya da Release.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File tests\Calistir.ps1 -Derle
#>
param(
    [switch]$Derle,
    [string]$Filtre = '*',
    [ValidateSet('Debug', 'Release')]
    [string]$Yapilandirma = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$bin = Join-Path $repo ('src\Pusula.Wpf\bin\x64\' + $Yapilandirma + '\net48')
$out = Join-Path $PSScriptRoot 'cikti'
$fixture = Join-Path $PSScriptRoot 'Veri\PusulaDeneme'
$sample = Join-Path $PSScriptRoot 'Veri\Ornek\AB-7000-0002'

if ($Derle)
{
    Get-Process Pusula -ErrorAction SilentlyContinue | Stop-Process -Force
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = $null
    if (Test-Path $vswhere)
    {
        $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    }

    $sln = Join-Path $repo 'Pusula.sln'
    if ($msbuild)
    {
        & $msbuild $sln /p:Configuration=$Yapilandirma /p:Platform=x64 /nologo /v:m
    }
    else
    {
        & dotnet build $sln -c $Yapilandirma
    }

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host 'Derleme başarısız.' -ForegroundColor Red
        exit 1
    }
}

if (-not (Test-Path (Join-Path $bin 'Pusula.exe')))
{
    Write-Host ('Pusula.exe bulunamadı: ' + $bin + ' (önce -Derle ile çalıştırın)') -ForegroundColor Red
    exit 1
}

New-Item -ItemType Directory -Force $out | Out-Null
$settingsFile = Join-Path $env:LOCALAPPDATA 'Pusula\ayarlar.json'
$savedSettings = if (Test-Path $settingsFile) { [IO.File]::ReadAllText($settingsFile) } else { $null }

$scripts = Get-ChildItem (Join-Path $PSScriptRoot 'Senaryolar') -Filter '*.ps1' | Where-Object { $_.BaseName -like $Filtre } | Sort-Object Name
$totalOk = 0
$totalFail = 0
$broken = @()
$watch = [Diagnostics.Stopwatch]::StartNew()
try
{
    foreach ($script in $scripts)
    {
        $data = Join-Path $env:TEMP ('pusula-test-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
        robocopy $fixture $data /E /NFL /NDL /NJH /NJS /NP | Out-Null
        $one = [Diagnostics.Stopwatch]::StartNew()
        try
        {
            # Senaryonun hata çıktısı koşturucuyu durdurmasın (PowerShell 5.1 stderr'i hata kaydına çevirir).
            $ErrorActionPreference = 'Continue'
            $text = & powershell -NoProfile -STA -ExecutionPolicy Bypass -File $script.FullName -Bin $bin -Out $out -Data $data -Sample $sample 2>&1 | Out-String
            $ErrorActionPreference = 'Stop'
            $code = $LASTEXITCODE
        }
        finally
        {
            Remove-Item $data -Recurse -Force -ErrorAction SilentlyContinue
        }

        $ok = ([regex]::Matches($text, '(?m)^\s*OK ')).Count
        $fail = ([regex]::Matches($text, '(?m)^\s*HATA ')).Count
        $crashed = $code -ne 0 -or $text -match 'Exception|ParserError|FullyQualifiedErrorId'
        $totalOk += $ok
        $totalFail += $fail
        $color = if ($fail -gt 0 -or $crashed) { 'Red' } else { 'Green' }
        Write-Host ('{0,-22} OK {1,3}   HATA {2,3}   {3,5:N1} sn' -f $script.BaseName, $ok, $fail, $one.Elapsed.TotalSeconds) -ForegroundColor $color
        foreach ($line in ($text -split "`r?`n" | Where-Object { $_ -match '^\s*HATA ' }))
        {
            Write-Host ('    ' + $line.Trim()) -ForegroundColor Red
        }

        if ($crashed)
        {
            $broken += $script.BaseName
            Write-Host ($text.Trim() -split "`r?`n" | Select-Object -Last 12 | Out-String) -ForegroundColor DarkRed
        }
    }
}
finally
{
    if ($savedSettings -ne $null)
    {
        [IO.File]::WriteAllText($settingsFile, $savedSettings)
    }
}

Write-Host ''
Write-Host ('Toplam: {0} senaryo, {1} OK, {2} HATA, {3} çöken · {4:N0} sn · ekran görüntüleri: {5}' -f $scripts.Count, $totalOk, $totalFail, $broken.Count, $watch.Elapsed.TotalSeconds, $out)
if ($totalFail -gt 0 -or $broken.Count -gt 0)
{
    exit 1
}

exit 0
