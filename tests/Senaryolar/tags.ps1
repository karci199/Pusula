param([string]$Bin, [string]$Out, [string]$Data, [string]$Sample)
. (Join-Path $PSScriptRoot '..\_Ortak.ps1')
# Etiketler: kapsam (tür / alt tür), eski düz etiketlerin dönüşümü, ad düzeltme, kayıtta etiket, notun YAML'ında Obsidian
# "tags:" listesi (çok satırlı eski listenin üstüne yazma, gövdeye dokunmama).
$TD = [Pusula.Core.Types.TagDefinition]

"--- 1. Kapsam"
$set = [Pusula.Core.Types.TypeSet]::CreateDefault()
$names = { param($t, $s) @($set.TagsFor($t, $s) | ForEach-Object { $_.Name }) }
Check 'örnek etiketler var' (@($set.TagList | % Name) -contains 'faz_kararli' -and @($set.TagList | % Name) -contains 'RS422' -and @($set.TagList | % Name) -contains 'SATIMO') (@($set.TagList | % Name) -join ',')
Check 'RF kablo: faz_kararli + her yerde geçenler' ((& $names 'cable' 'RF') -contains 'faz_kararli' -and (& $names 'cable' 'RF') -contains 'SATIMO' -and -not ((& $names 'cable' 'RF') -contains 'RS422'))
Check 'Güç kablosu: faz_kararli ve RS422 yok' (-not ((& $names 'cable' 'Güç') -contains 'faz_kararli') -and -not ((& $names 'cable' 'Güç') -contains 'RS422') -and (& $names 'cable' 'Güç') -contains 'SATIMO')
Check 'Haberleşme: RS422, RS232' ((& $names 'cable' 'Haberleşme') -contains 'RS422' -and (& $names 'cable' 'Haberleşme') -contains 'RS232')
Check 'jigde kablo etiketi yok' (-not ((& $names 'jig' 'Mekanik') -contains 'faz_kararli'))
Check 'ad düzeltme: # ve boşluk' ($TD::NameFrom(' #açık iş ') -eq 'açık_iş' -and $TD::NameFrom('RS422') -eq 'RS422')

"--- 2. Dönüşüm (sürüm 4 → 5)"
$old = [Pusula.Core.Types.TypeSet]::CreateDefault(); $old.TagList.Clear(); $old.Tags.Clear(); $old.Tags.Add('SATIMO'); $old.Tags.Add('Ölçüm odası'); $old.SchemaVersion = 4
$old.Normalize()
Check 'eski düz etiketler her yerde geçerli' (@($old.TagList | Where-Object { $_.Name -eq 'Ölçüm_odası' -and $_.Scopes.Count -eq 0 }).Count -eq 1)
Check 'örnekler eklendi, tekrar yok' (@($old.TagList | Where-Object Name -eq 'SATIMO').Count -eq 1 -and @($old.TagList | % Name) -contains 'faz_kararli' -and $old.SchemaVersion -eq 5)
$mine = [Pusula.Core.Types.TypeSet]::CreateDefault(); $mine.TagList.Clear(); $mine.TagList.Add($TD::For('benim', 'jig')); $mine.SchemaVersion = 4; $mine.Normalize()
Check 'kullanıcının listesi varsa dokunulmaz' (@($mine.TagList).Count -eq 1)

"--- 3. Notun YAML'ında Obsidian tags listesi"
$note = Join-Path $Data 'etiket-deneme.md'
$text = "---`r`nnumara: JT-1`r`ntags:`r`n  - eski`r`n  - ikinci`r`ndurum: x`r`n---`r`n# Başlık`r`n- gövde maddesi`r`n"
[IO.File]::WriteAllText($note, $text, (New-Object Text.UTF8Encoding $true))
$lists = New-Object 'System.Collections.Generic.Dictionary[string,System.Collections.Generic.IList[string]]'
$tl = New-Object 'System.Collections.Generic.List[string]'; $tl.Add('faz_kararli'); $tl.Add('RS422'); $lists['tags'] = $tl
$vals = New-Object 'System.Collections.Generic.Dictionary[string,string]'; $vals['tanim'] = 'Deneme'
$err = [Pusula.Core.Notes.Journal]::SetFrontMatter($note, $vals, $lists)
$after = [IO.File]::ReadAllText($note)
Check 'tags tek satır liste' ($err -eq '' -and $after -match '(?m)^tags: \[faz_kararli, RS422\]\r?$') $after
Check 'eski alt alta liste temizlendi, öteki alanlar yerinde' (-not ($after -match '  - eski') -and $after -match '(?m)^durum: x' -and $after -match '(?m)^numara: JT-1')
Check 'gövde aynı (madde işareti dokunulmadı)' ($after.EndsWith("---`r`n# Başlık`r`n- gövde maddesi`r`n"))
Check 'BOM korundu' (([IO.File]::ReadAllBytes($note))[0] -eq 0xEF)
$lists['tags'] = New-Object 'System.Collections.Generic.List[string]'
[Pusula.Core.Notes.Journal]::SetFrontMatter($note, $vals, $lists) | Out-Null
Check 'boş liste satırı kaldırır' (-not ([IO.File]::ReadAllText($note) -match '(?m)^tags:'))

"--- 4. Kayıtta etiketler"
$types = [Pusula.Core.Types.TypeStore]::LoadOrCreate($Data)
$cable = $types.Find('cable')
$res = [Pusula.Core.Inventory.InventoryService]::Create($Data, $cable, 'General', '', 'JT-0009-0001', 'Etiket kablo', 'RF', '', 'k')
Check 'kayıt açıldı' ($res.Succeeded) $res.Error
$in = New-Object 'System.Collections.Generic.List[string]'; $in.Add('#faz_kararli'); $in.Add('faz_kararli'); $in.Add('yeni etiket')
$e2 = [Pusula.Core.Tracking.RecordEdit]::EditInventory($res.Folder, 'Etiket kablo', 'RF', '', $null, $in)
$rec = [Pusula.Core.Tracking.JsonFile].GetMethod('TryRead').MakeGenericMethod([Pusula.Core.Inventory.InventoryRecord]).Invoke($null, @([Pusula.Core.Inventory.InventoryService]::RecordFile($res.Folder)))
Check 'etiketler kayda yazıldı (# atılır, tekrar yok, boşluk _)' ($e2 -eq '' -and ($rec.Tags -join ',') -eq 'faz_kararli,yeni_etiket') ($rec.Tags -join ',')
$e3 = [Pusula.Core.Tracking.RecordEdit]::EditInventory($res.Folder, 'Etiket kablo 2', 'RF', '', $null, $null)
$rec = [Pusula.Core.Tracking.JsonFile].GetMethod('TryRead').MakeGenericMethod([Pusula.Core.Inventory.InventoryRecord]).Invoke($null, @([Pusula.Core.Inventory.InventoryService]::RecordFile($res.Folder)))
Check 'etiket verilmezse korunur' (($rec.Tags -join ',') -eq 'faz_kararli,yeni_etiket' -and $rec.Name -eq 'Etiket kablo 2')
"SONUÇ OK=$ok HATA=$fail"
