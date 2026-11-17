param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
# MeDitor (ayrı md düzenleyici): sekmeler, adsız not ve farklı kaydet, klasör kenar çubuğu, [[ önerileri ve bağlantı,
# HTML dışa aktarma, oturum. Pusula'ya bağlı değildir; kendi derlemesinden yüklenir, ayarları geçici dosyaya yazar.
$ErrorActionPreference = 'Stop'
$mbin = [IO.Path]::GetFullPath((Join-Path $Bin '..\..\..\..\..\MeDitor\bin\x64\Debug\net48'))
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
[Reflection.Assembly]::LoadFrom((Join-Path $mbin 'DesignKit.Wpf.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $mbin 'MeDitor.exe')) | Out-Null
$ok = 0; $fail = 0
function Check([string]$name, [bool]$cond, [string]$detail = '') { if ($cond) { $script:ok++; "  OK   $name" } else { $script:fail++; "  HATA $name $detail" } }
function Pump { $f = New-Object System.Windows.Threading.DispatcherFrame; [System.Windows.Threading.Dispatcher]::CurrentDispatcher.BeginInvoke([System.Windows.Threading.DispatcherPriority]::Background, [Action]{ $f.Continue = $false }) | Out-Null; [System.Windows.Threading.Dispatcher]::PushFrame($f) }
function Wait { for ($k = 0; $k -lt 30; $k++) { Pump; Start-Sleep -Milliseconds 20 } }
function W([string]$path, [string]$text) { New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null; [IO.File]::WriteAllText($path, $text, (New-Object Text.UTF8Encoding $false)) }

$env:MEDITOR_AYARLAR = Join-Path $Data 'meditor-ayarlar.json'
$notes = Join-Path $Data 'MeDitorNotlar'
W "$notes\a.md" "# Başlık A`n`nBağlantı: [[b]] ve #proje etiketi.`n`n- [ ] iş`n"
W "$notes\b.md" "# B notu`n"
W "$notes\Alt\c.md" "c"
W "$notes\notlar.txt" "md değil"

[System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
$app = New-Object MeDitor.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'
$win = New-Object MeDitor.MainWindow
$win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1360; $win.Height = 860; $win.WindowState = 'Normal'
$win.Show(); Wait

Check 'boş ekran: sekme yok' ($win.Tabs.Count -eq 0 -and $win.FindName('EmptyPanel').Visibility -eq 'Visible')

"--- sekmeler"
$a = $win.Open("$notes\a.md"); Wait
Check 'dosya sekmede açıldı' ($win.Tabs.Count -eq 1 -and $a.ViewModel.Text -like '# Başlık A*' -and $win.Title -like 'a*MeDitor') $win.Title
$again = $win.Open("$notes\a.md"); Wait
Check 'aynı dosya ikinci sekme açmaz' ($win.Tabs.Count -eq 1 -and $again -eq $a)
$u = $win.Open(''); Wait
Check 'adsız not' ($win.Tabs.Count -eq 2 -and $u.ViewModel.IsUntitled -and $u.ViewModel.FileName -eq 'Adsız' -and $win.Active -eq $u)
$u.ViewModel.Text = "# Yeni fikir`nmetin"
Check 'adsız not kaydedilmez, farklı kaydetle kaydedilir' (-not $u.ViewModel.Save() -and $u.ViewModel.SaveAs("$notes\yeni.md") -and (Test-Path "$notes\yeni.md") -and -not $u.ViewModel.IsUntitled -and $u.ViewModel.FileName -eq 'yeni')
$u.ViewModel.Text = "değişti"
Check 'kirli sekme başlığında nokta' ($u.ViewModel.TabTitle -eq 'yeni •') $u.ViewModel.TabTitle
Check 'Ctrl+S kaydeder' ($u.Save() -and [IO.File]::ReadAllText("$notes\yeni.md") -eq 'değişti' -and -not $u.ViewModel.IsDirty)
$win.HandleKey([System.Windows.Input.Key]::Tab, $true, $false) | Out-Null; Wait
Check 'Ctrl+Tab sonraki sekme' ($win.Active -eq $a)

"--- klasör kenar çubuğu"
$win.LoadFolder($notes); Wait
$names = @($win.FindName('FolderTree').Items | ForEach-Object Name) -join ','
Check 'ağaç: önce klasör, sonra md dosyaları (txt yok)' ($names -eq 'Alt,a,b,yeni') $names
Check 'sekmenin kökü klasör' ($a.ViewModel.Root -eq $notes)
$sug = @($a.ViewModel.Suggestions('b'))
Check '[[ önerisi klasörden' ($sug.Count -ge 1 -and $sug[0].Insert -eq '[[b]]') (@($sug | ForEach-Object Insert) -join ',')
$tags = @($a.TagProvider.Invoke())
Check '#etiket önerisi klasördeki notlardan' (@($tags | ForEach-Object Key) -contains '#proje') (@($tags | ForEach-Object Key) -join ',')
$nav = $win.GetType().GetMethod('Navigate', [Reflection.BindingFlags]'NonPublic,Instance')
Check '[[b]] tıklanınca b açılır' ($nav.Invoke($win, @('b')) -and $win.Active.ViewModel.FileName -eq 'b' -and $win.Tabs.Count -eq 3)
Check 'olmayan bağlantı açılmaz' (-not $nav.Invoke($win, @('yok')))
$win.Select($a); Wait
Check 'seçili sekme ağaçta seçili' ($win.FindName('FolderTree').SelectedItem -ne $null -and $win.FindName('FolderTree').SelectedItem.Name -eq 'a') ([string]$win.FindName('FolderTree').SelectedItem.Name)

"--- dışa aktarma"
$html = [MeDitor.Export]::ToHtml($a.ViewModel, $false)
Check 'HTML: başlık, bağlantı metne döner, görev kutusu' ($html -match '<h1[^>]*>Başlık A</h1>' -and $html -notmatch '\[\[b\]\]' -and $html -match 'type="checkbox"') ($html.Substring(0, [Math]::Min(400, $html.Length)))
$err = [MeDitor.Export]::SaveHtml($a.ViewModel, "$notes\a.html")
Check 'HTML dosyası yazıldı' ($err -eq '' -and (Test-Path "$notes\a.html"))

"--- kapatma ve oturum"
$b = $win.Active
$win.Select($win.Tabs[2]); Wait
$win.CloseTab($win.Tabs[2]); Wait
Check 'değişmemiş sekme sormadan kapanır' ($win.Tabs.Count -eq 2)
$a.ViewModel.Text = $a.ViewModel.Text + "`nek"
$win.CloseTab($a); Wait
Check 'kirli sekme kapanmadan önce sorar' ($win.Tabs.Count -eq 2 -and $win.FindName('CloseDialog').IsOpen)
$win.FindName('CloseDialog').IsOpen = $false; $win.GetType().GetMethod('CancelClose', [Reflection.BindingFlags]'NonPublic,Instance').Invoke($win, @()) | Out-Null
$a.ViewModel.Load(); Wait
$win.SaveSession()
$settings = [MeDitor.App]::Settings
Check 'oturum: açık sekmeler ve klasör saklandı' (@($settings.Session).Count -eq 2 -and $settings.Folder -eq $notes -and (Test-Path $env:MEDITOR_AYARLAR)) (@($settings.Session) -join ' | ')
Check 'son açılanlar' (@($settings.Recent) -contains "$notes\a.md")

# Görüntü: pencere çizilir
$win.Select($a); Wait; $a.RenderPreview(); Wait
$rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap 2040, 1290, 144, 144, ([System.Windows.Media.PixelFormats]::Pbgra32)
$rtb.Render($win)
$enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder; $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
$fs = [IO.File]::Create("$Out\meditor.png"); $enc.Save($fs); $fs.Close()

$win.Close()
Remove-Item Env:MEDITOR_AYARLAR
"SONUÇ OK=$ok HATA=$fail"
