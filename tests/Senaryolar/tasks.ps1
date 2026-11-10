param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Yapılacaklar: ana notun "## Yapılacaklar" bölümüne iş ekleme (Obsidian Tasks: @kişi, 📅 son tarih), kutuyla kapatma
# (- [x] ✅ tarih) ve yeniden açma; başka satırlar değişmez; süzgeç Açık / Bana ait / Bitenler.
$TL = [Pusula.Core.Notes.TaskList]; $OT = [Pusula.Core.Notes.OpenTask]
$cal = [char]::ConvertFromUtf32(0x1F4C5); $chk = [string][char]0x2705
$tmp = Join-Path $env:TEMP ('pusula-is-' + [guid]::NewGuid().ToString('N').Substring(0, 6)); New-Item -ItemType Directory $tmp | Out-Null
try {
    "--- 1. Çekirdek"
    $note = Join-Path $tmp 'a.md'
    $body = "---`r`nnumara: X`r`n---`r`n# Başlık`r`nGövde metni.`r`n`r`n---`r`n`r`n## Günlük`r`n`r`n### 2026-11-01 · Not · deneme`r`n"
    [IO.File]::WriteAllText($note, $body, (New-Object Text.UTF8Encoding $false))
    Check 'eklendi' ($TL::Add($note, 'SN02 VNA tekrar', 'Ayşe Demir', [datetime]'2026-11-14') -eq '')
    $text = [IO.File]::ReadAllText($note)
    Check 'bölüm Günlük ayracının üstünde, satır Tasks biçiminde' ($text.Contains("Gövde metni.`r`n`r`n## Yapılacaklar`r`n`r`n- [ ] SN02 VNA tekrar @Ayşe_Demir $cal 2026-11-14`r`n`r`n---`r`n`r`n## Günlük")) $text
    $TL::Add($note, 'İkinci iş', '', $null) | Out-Null
    $text = [IO.File]::ReadAllText($note)
    Check 'ikinci iş listenin sonuna' ($text.Contains("@Ayşe_Demir $cal 2026-11-14`r`n- [ ] İkinci iş`r`n`r`n---"))
    Check 'gövde ve Günlük aynen' ($text.StartsWith("---`r`nnumara: X`r`n---`r`n# Başlık`r`nGövde metni.") -and $text.Contains('### 2026-11-01 · Not · deneme'))
    Check 'BOM eklenmedi' ([IO.File]::ReadAllBytes($note)[0] -ne 0xEF)
    $tasks = @($OT::Read($note))
    Check 'okunur: metin, kişi, son tarih' ($tasks.Count -eq 2 -and $tasks[0].Text -eq 'SN02 VNA tekrar' -and $tasks[0].PersonName -eq 'Ayşe Demir' -and $tasks[0].Due -eq [datetime]'2026-11-14') ($tasks[0].Text + '|' + $tasks[0].PersonName)
    Check 'kapatıldı' ($TL::SetDone($note, $tasks[0].Line, $tasks[0].RawLine, $true, [datetime]'2026-11-10') -eq '')
    $text = [IO.File]::ReadAllText($note)
    Check 'kutu [x], ✅ tarih' ($text.Contains("- [x] SN02 VNA tekrar @Ayşe_Demir $cal 2026-11-14 $chk 2026-11-10"))
    Check 'açıklarda yok, hepsinde bitti' (@($OT::Read($note)).Count -eq 1 -and @($OT::Read($note, $true) | ? { $_.IsDone -and $_.DoneDate -eq [datetime]'2026-11-10' }).Count -eq 1)
    Check 'eski satırla ikinci kez: değişmiş, dokunulmaz' ($TL::SetDone($note, $tasks[0].Line, $tasks[0].RawLine, $false, [datetime]'2026-11-10') -eq 'changed')
    $done = @($OT::Read($note, $true) | ? IsDone)[0]
    $TL::SetDone($note, $done.Line, $done.RawLine, $false, [datetime]'2026-11-10') | Out-Null
    Check 'yeniden açılınca ✅ kalkar' ([IO.File]::ReadAllText($note).Contains("- [ ] SN02 VNA tekrar @Ayşe_Demir $cal 2026-11-14`r`n"))
    $fresh = Join-Path $tmp 'yeni.md'
    $TL::Add($fresh, 'Tek iş', '', $null) | Out-Null
    Check 'not yoksa yalnız bölümle açılır' ([IO.File]::ReadAllText($fresh) -eq "## Yapılacaklar`r`n`r`n- [ ] Tek iş`r`n")
    $plain = Join-Path $tmp 'b.md'; [IO.File]::WriteAllText($plain, "# B`nmetin`n", (New-Object Text.UTF8Encoding $false))
    $TL::Add($plain, 'Sonda', '', $null) | Out-Null
    Check 'Günlük yoksa sona, LF korunur' ([IO.File]::ReadAllText($plain) -eq "# B`nmetin`n`n## Yapılacaklar`n`n- [ ] Sonda`n")

    "--- 2. Stok sayfası"
    [System.Threading.SynchronizationContext]::SetSynchronizationContext((New-Object System.Windows.Threading.DispatcherSynchronizationContext))
    $app = New-Object Pusula.Wpf.App; $app.InitializeComponent(); $app.ShutdownMode = 'OnExplicitShutdown'; [Pusula.Wpf.Themes]::Apply([Pusula.Wpf.Themes]::Find('FluentDark'))
    $win = New-Object Pusula.Wpf.MainWindow
    $win.WindowStartupLocation = 'Manual'; $win.Left = -20000; $win.Top = -20000; $win.ShowActivated = $false; $win.Width = 1240; $win.Height = 1150
    $win.Show(); Pump
    $vm = $win.DataContext; $vm.RootPath = $Data; Wait
    $stock = Join-Path $Data '7000_MILKON\AB-7000-0003'
    $vm.SelectNodeByPath($stock); Wait
    $before = @($vm.Detail.OpenTasks | ? { -not $_.IsDone }).Count
    $vm.NewTaskText = 'Jig Rev AC montajı @' + [Environment]::UserName + ' #SATIMO'; $vm.NewTaskDueText = '12.11.2026'
    Check 'stokta iş eklendi' ($vm.AddTask()); Wait
    $mainNote = Get-ChildItem $stock -Filter 'AB-7000-0003*.md' | Select -First 1
    if (-not $mainNote) { $mainNote = Get-ChildItem $stock -Filter '*.md' | Select -First 1 }
    Check 'ana nota yazıldı' ($mainNote -and [IO.File]::ReadAllText($mainNote.FullName).Contains("- [ ] Jig Rev AC montajı #SATIMO @" + [Environment]::UserName + " $cal 2026-11-12")) ($(if ($mainNote) { $mainNote.Name }))
    $row = @($vm.Detail.OpenTasks | ? { $_.Text -eq 'Jig Rev AC montajı' })[0]
    Check 'listede: kişi, tarih' ($row -ne $null -and $row.PersonName -eq [Environment]::UserName -and $row.DueText -like '12.11*' -and @($vm.Detail.OpenTasks | ? { -not $_.IsDone }).Count -eq $before + 1)
    Check 'başlık açık sayısı' ($vm.Detail.OpenTasksHeader -eq ('Yapılacaklar · ' + ($before + 1))) $vm.Detail.OpenTasksHeader
    $f = $vm.Detail.TaskFilter
    $view = [System.Windows.Data.CollectionViewSource]::GetDefaultView($vm.Detail.OpenTasks)
    $f.Select('mine'); Pump
    Check 'Bana ait: kullanıcının işi' (@($view | ? { $_.Text -eq 'Jig Rev AC montajı' }).Count -eq 1 -and @($view | ? { $_.PersonName -ne [Environment]::UserName }).Count -eq 0)
    $f.Select('open'); Pump
    Snap $win "$Out\tasks-stock.png"
    Check 'kapatıldı' ($vm.SetTaskDone($row, $true)); Wait
    Check 'açıklardan düştü, bitenlerde' (@($view | ? { $_.Text -eq 'Jig Rev AC montajı' }).Count -eq 0 -and @($vm.Detail.OpenTasks | ? { $_.IsDone -and $_.Text -eq 'Jig Rev AC montajı' }).Count -eq 1)
    $f.Select('done'); Pump
    $doneRow = @($view | ? { $_.Text -eq 'Jig Rev AC montajı' })[0]
    Check 'Bitenler: bitti tarihiyle' ($doneRow -ne $null -and $doneRow.DueText -eq ('bitti ' + [datetime]::Today.ToString('dd.MM')))
    Check 'yeniden açıldı' ($vm.SetTaskDone($doneRow, $false)); Wait
    Check 'açıklara döndü' (@($vm.Detail.OpenTasks | ? { -not $_.IsDone -and $_.Text -eq 'Jig Rev AC montajı' }).Count -eq 1)
    $f.Select('open'); Pump
    $vm.NewTaskDueText = '32.13.2026'; $vm.NewTaskText = 'yanlış tarih'
    Check 'geçersiz tarih eklenmez' (-not $vm.AddTask() -and $vm.Status -match 'gg.aa.yyyy')
    $vm.NewTaskDueText = ''; $vm.NewTaskText = ''

    "--- 3. Proje"
    $vm.SelectNodeByPath((Join-Path $Data '7000_MILKON')); Wait
    Check 'projede stok işi numarasıyla' (@($vm.Detail.OpenTasks | ? { $_.Text -eq 'Jig Rev AC montajı' -and $_.Label -like 'AB-7000-0003*' }).Count -eq 1)
    $vm.NewTaskText = 'Haftalık toplantı sunumu'
    Check 'projeye iş eklendi' ($vm.AddTask()); Wait
    Check 'proje notunda' (@($vm.Detail.OpenTasks | ? { $_.Text -eq 'Haftalık toplantı sunumu' }).Count -eq 1)
    Snap $win "$Out\tasks-project.png"
    $win.Close()
} finally { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }