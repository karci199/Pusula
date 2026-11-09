param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
$FD = [Pusula.Core.Types.FieldDefinition]
"--- 1. Anahtar"
Check 'Konnektör A → konnektor_a' ($FD::KeyFrom('Konnektör A') -eq 'konnektor_a')
Check 'Faz kararlı → faz_kararli' ($FD::KeyFrom('Faz kararlı') -eq 'faz_kararli')
$root = Join-Path $env:TEMP ('pusula-types-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory $root | Out-Null
try {
    $set = [Pusula.Core.Types.TypeStore]::LoadOrCreate($root)
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
    $vm = New-Object Pusula.Wpf.ViewModels.TypeEditor.TypeEditorViewModel($root, $set)
    "--- 2. Düzenleme"
    Check 'türler: Kablo, Jig, Yazılım' ((($vm.Types | ForEach-Object Name) -join ',') -eq 'Kablo,Jig,Yazılım')
    $jig = $vm.Types | Where-Object Name -eq 'Jig'; $vm.Selected = $jig
    Check 'jigin hazır alanları gelir' ($jig.Fields.Count -eq 8)
    $jig.Fields.Clear()
    $f = New-Object Pusula.Wpf.ViewModels.TypeEditor.FormatRow; $f.Pattern = 'JT-####-####'; $f.Label = 'Test jigi'; $jig.Formats.Add($f)
    $f = New-Object Pusula.Wpf.ViewModels.TypeEditor.FormatRow; $f.Pattern = 'JX-####-####'; $f.Label = 'Üretim jigi'; $jig.Formats.Add($f)
    $jig.TryNumber = 'JX-0002-0015'
    Check 'dene: JX uyar' ($jig.TryResult -eq '✓ Üretim jigi') $jig.TryResult
    $jig.TryNumber = 'JZ-1'
    Check 'dene: uymaz' ($jig.TryResult -match 'uymuyor')
    $fr = New-Object Pusula.Wpf.ViewModels.TypeEditor.FieldRow; $fr.Name = 'Malzeme'; $fr.Kind = 'choice'; $fr.Choices = 'Al 6061, Delrin, Paslanmaz'; $jig.Fields.Add($fr)
    $fr = New-Object Pusula.Wpf.ViewModels.TypeEditor.FieldRow; $fr.Name = 'Tork'; $fr.Kind = 'number'; $fr.Unit = 'Nm'; $fr.SubTypes = 'Montaj'; $jig.Fields.Add($fr)
    $st = New-Object Pusula.Wpf.ViewModels.TypeEditor.StatusRow; $st.Name = 'Kalibrasyonda'; $st.Tone = 'waiting'; $st.IsSide = $true; $jig.Statuses.Add($st)
    Check 'var olan durum kilitli' (($jig.Statuses | Where-Object Name -eq 'Talep').IsExisting -and $jig.AreFoldersLocked)
    $vm.AddTypeCommand.Execute($null)
    Check 'yeni tür: anahtar, klasörler açık' ($vm.Selected.IsNew -and $vm.Selected.Source.Key -eq 'yeni_tur' -and $vm.Selected.GeneralFolder -eq 'ENVANTER\Yeni_turler') ($vm.Selected.Source.Key + ' ' + $vm.Selected.GeneralFolder)
    $vm.Selected.Name = 'Ölçüm cihazı'; $vm.Selected.PluralName = 'Ölçüm cihazları'
    $win = New-Object Pusula.Wpf.TypeEditorWindow($vm)
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false
    $win.Show(); $vm.Selected = $jig; Snap $win "$Out\types-window.png"
"--- Tablo hücresi düzenleme kutusu temalı (beyaz değil)"
function FindAll($d, [type]$type) { if ($d -is $type) { $d }; for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($d); $i++) { FindAll ([System.Windows.Media.VisualTreeHelper]::GetChild($d, $i)) $type } }
$grid = @(FindAll $win ([System.Windows.Controls.DataGrid]) | Where-Object { [object]::ReferenceEquals($_.ItemsSource, $jig.Formats) })[0]
$grid.Focus() | Out-Null
$grid.CurrentCell = New-Object System.Windows.Controls.DataGridCellInfo($grid.Items[0], $grid.Columns[0])
$grid.BeginEdit() | Out-Null; Pump; Pump
$box = @(FindAll $grid ([System.Windows.Controls.TextBox]))[0]
$bg = if ($box) { $box.Background.Color } else { $null }
Check 'düzenleme kutusu açıldı' ($box -ne $null)
Check 'arka plan şeffaf (satır rengi görünür, beyaz/siyah kutu yok)' ($bg -ne $null -and $bg.A -eq 0) ([string]$bg)
Check 'altta vurgu çizgisi' ($box.BorderThickness.Bottom -eq 2 -and $box.BorderThickness.Top -eq 0)
Snap $win "$Out\types-edit-cell.png"
$grid.CancelEdit() | Out-Null
    "--- 3. Kaydet"
    $dup = New-Object Pusula.Wpf.ViewModels.TypeEditor.FieldRow; $dup.Name = 'malzeme'; $jig.Fields.Add($dup)
    Check 'aynı alan iki kez → hata' (-not $vm.Save() -and $vm.Error -match 'iki kez') $vm.Error
    $jig.Fields.Remove($dup) | Out-Null
    Check 'kaydedildi' ($vm.Save()) $vm.Error
    $win.Close()
    $re = [Pusula.Core.Types.TypeStore]::LoadOrCreate($root)
    $j = $re.Find('jig')
    Check 'biçimler: JIG, JT, JX' ((($j.NumberFormats | ForEach-Object Pattern) -join ',') -eq 'JIG-####-####,JT-####-####,JX-####-####')
    Check 'alanlar: malzeme (liste), tork (Nm, Montaj)' ($j.Fields.Count -eq 2 -and $j.Fields[0].Key -eq 'malzeme' -and $j.Fields[0].Choices.Count -eq 3 -and $j.Fields[1].Unit -eq 'Nm' -and $j.Fields[1].SubTypes[0] -eq 'Montaj')
    Check 'alt türe göre alan' (@($j.FieldsFor('Mekanik')).Count -eq 1 -and @($j.FieldsFor('Montaj')).Count -eq 2)
    Check 'yeni sıra dışı durum' ($j.FindStatus('Kalibrasyonda') -ne $null -and $j.SideStatuses.Count -eq 3)
    Check 'yeni tür: anahtar ve klasör son addan, stok/proje korunur' ($re.Find('olcum_cihazi').Name -eq 'Ölçüm cihazı' -and $re.Find('olcum_cihazi').GeneralFolder -eq 'ENVANTER\Olcum_cihazlari' -and $re.Find('stock') -ne $null -and $re.Find('project') -ne $null)
} finally { Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue }
""
"SONUC: $ok basarili, $fail hata"
