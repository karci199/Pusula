param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Aynı kodla ikinci proje, başka projede aynı numarayla stok açılmaz; kodda "_" olmaz.
$PS = [Pusula.Core.Tracking.ProjectService]
$none = [string[]]@()

$r = $PS::CreateProject($Data, '7000', 'DENEME', $none)
Check 'aynı kodla proje açılmaz' (-not $r.Succeeded -and $r.Error -eq 'NumberInUse' -and $r.Folder -like '*7000_MILKON') ([string]$r.Error + ' ' + $r.Folder)
Check 'klasör oluşmadı' (-not (Test-Path (Join-Path $Data '7000_DENEME')))

$r = $PS::CreateProject($Data, '71_00', 'X', $none)
Check 'kodda _ olmaz' (-not $r.Succeeded -and $r.Error -eq 'InvalidCharacters') ([string]$r.Error)

$r = $PS::CreateProject($Data, '7100', 'KARAKUS', $none)
Check 'yeni kod açılır' ($r.Succeeded) ([string]$r.Error)

$r = $PS::CreateStock($r.Folder, 'AB-7000-0001', 'kopya', '', $none, '')
Check 'başka projede aynı stok no açılmaz' (-not $r.Succeeded -and $r.Error -eq 'NumberInUse') ([string]$r.Error + ' ' + $r.Folder)

$r = $PS::CreateStock((Join-Path $Data '7100_KARAKUS'), 'AB-7100-0001', 'yeni', '', $none, '')
Check 'yeni stok no açılır' ($r.Succeeded) ([string]$r.Error)
"SONUÇ OK=$ok HATA=$fail"
