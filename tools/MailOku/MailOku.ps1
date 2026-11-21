<#
    MailOku: .msg (Outlook) ve .eml (Gmail "Mesajı indir", Thunderbird …) dosyasını okur, Pusula'ya nasıl geleceğini gösterir.
    Pusula'nın dışında geçici bir deneme aracıdır; yalnız okur, ağa çıkmaz, Outlook ya da lisans gerekmez.

    Kullanım:
      powershell -NoProfile -ExecutionPolicy Bypass -File tools\MailOku\MailOku.ps1 -Dosya "C:\yol\mail.msg"
      powershell -NoProfile -ExecutionPolicy Bypass -File tools\MailOku\MailOku.ps1 -Dosya "C:\yol\mail.eml" -EkKlasoru "C:\yol\ekler"
      powershell -NoProfile -ExecutionPolicy Bypass -File tools\MailOku\MailOku.ps1 -Ornek     (örnek .msg ve .eml üretip okur)
#>
param(
    [string[]]$Dosya = @(),
    [string]$EkKlasoru = '',
    [switch]$Ornek
)

$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'MailOku.cs')

if ($Ornek) {
    $dir = Join-Path $env:TEMP 'MailOku-ornek'
    New-Item -ItemType Directory -Force $dir | Out-Null

    # Örnek .msg (Outlook alan adlarıyla)
    $m = New-Object MailOku+Mail
    $m.Konu = 'RE: Boynuz anten geçti-kaldı ölçütleri'
    $m.Gonderen.Ad = 'Ayşe Kaya'; $m.Gonderen.Eposta = 'ayse.kaya@firma.com.tr'
    $m.Tarih = [datetime]'2026-11-20 14:12'
    $m.Govde = "Merhaba,`r`n`r`nEkteki tabloya göre geri yansıma için -7 dB sınırında anlaştık.`r`n`r`nSaygılar,`r`nAyşe"
    foreach ($a in @(@('Ali Demirci', 'ali.demirci@firma.com.tr', $false, 1), @('katan', 'katan@firma.com.tr', $false, 1), @('ANTEN-TEST', 'anten-test@firma.com.tr', $true, 2))) {
        $k = New-Object MailOku+Kisi; $k.Ad = $a[0]; $k.Eposta = $a[1]; $k.Ekip = $a[2]; $k.Tur = $a[3]; $m.Alicilar.Add($k)
    }
    $e = New-Object MailOku+Ek; $e.Ad = 'Olcut_tablosu.xlsx'; $e.Veri = [Text.Encoding]::UTF8.GetBytes('ornek ek'); $m.Ekler.Add($e)
    $msg = Join-Path $dir 'ornek.msg'; Remove-Item $msg -ErrorAction SilentlyContinue
    [MailOku+Msg]::OrnekYaz($msg, $m)

    # Örnek .eml (Gmail'in indirdiği biçimde: kodlanmış konu, quoted-printable Türkçe gövde, HTML, base64 ek)
    $ek64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('Frekans;VSWR' + "`r`n" + '8,2;1,72'))
    $konu = '=?UTF-8?B?' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('Ölçüm sonuçları — SN-003')) + '?='
    $eml = @"
Delivered-To: katan@gmail.com
Date: Thu, 19 Nov 2026 14:30:05 +0300
From: "Ali Demirci" <ali.demirci@firma.com.tr>
To: katan <katan@gmail.com>, "Ayşe Kaya" <ayse.kaya@firma.com.tr>
Cc: ANTEN-TEST <anten-test@firma.com.tr>
Subject: $konu
MIME-Version: 1.0
Content-Type: multipart/mixed; boundary="SINIR1"

--SINIR1
Content-Type: multipart/alternative; boundary="SINIR2"

--SINIR2
Content-Type: text/plain; charset="UTF-8"
Content-Transfer-Encoding: quoted-printable

Merhaba,

SN-003 =C3=B6l=C3=A7=C3=BCm=C3=BC ekte; 8,2 GHz'de 1,72 =C3=A7=C4=B1kt=C4=
=B1.

Sayg=C4=B1lar
--SINIR2
Content-Type: text/html; charset="UTF-8"

<div>Merhaba,</div><div>SN-003 ölçümü ekte.</div>
--SINIR2--
--SINIR1
Content-Type: text/csv; name="VSWR_SN003.csv"
Content-Disposition: attachment; filename="VSWR_SN003.csv"
Content-Transfer-Encoding: base64

$ek64
--SINIR1--
"@
    $emlYol = Join-Path $dir 'ornek.eml'; [IO.File]::WriteAllText($emlYol, $eml.Replace("`r`n", "`n").Replace("`n", "`r`n"), (New-Object Text.UTF8Encoding $false))
    $Dosya = @($msg, $emlYol)
    'Örnekler: ' + $dir
}

if ($Dosya.Count -eq 0) { 'Dosya verin: -Dosya "C:\yol\mail.msg" (ya da -Ornek)'; exit 1 }

function Kisi([string]$ad) { '@' + ($ad.Trim() -replace '\s+', '_') }
# Ekip: .msg'de dağıtım listesi bilgisi var; .eml'de yok, tamamı büyük harfli ad (ANTEN-TEST) ekip sayılır.
function EkipMi($k) { $k.Ekip -or ($k.Ad -cmatch '^[A-ZÇĞİÖŞÜ0-9_\-]{3,}$') }
foreach ($yol in $Dosya) {
    $r = [MailOku]::Oku($yol)
    ''
    '=== ' + [IO.Path]::GetFileName($yol)
    '  Konu      : ' + $r.Konu
    '  Tarih     : ' + $(if ($r.Tarih) { ([datetime]$r.Tarih).ToString('dd.MM.yyyy HH:mm') } else { '(yok)' })
    '  Gönderen  : ' + $r.Gonderen.Ad + ' <' + $r.Gonderen.Eposta + '>'
    foreach ($k in $r.Alicilar) { '  Alıcı     : ' + $k.Ad + ' <' + $k.Eposta + '>' + $(if (EkipMi $k) { '  [ekip]' } else { '' }) + $(if ($k.Tur -eq 2) { '  (bilgi)' } else { '' }) }
    foreach ($a in $r.Ekler) { '  Ek        : ' + $a.Ad + ' (' + $a.Veri.Length + ' bayt)' }

    $baslik = ($r.Konu -replace '^\s*((RE|FW|FWD|YNT|İLT|ILT|Ynt|İlt)\s*:\s*)+', '').Trim()
    $kisiler = @(@($r.Gonderen.Ad) + @($r.Alicilar | Where-Object { -not (EkipMi $_) } | ForEach-Object Ad)) | Where-Object { $_ } | Select-Object -Unique
    $ekipler = @($r.Alicilar | Where-Object { EkipMi $_ } | ForEach-Object Ad)
    $paragraf = (($r.Govde -replace "`r", '') -split "`n\s*`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -and $_ -notmatch '^(Merhaba|Selam|Sayg|İyi çalışmalar|Hi|Hello|Regards)' } | Select-Object -First 1)
    '  --- Pusula''ya gelecek'
    '  Başlık    : ' + $baslik
    '  Katılımcı : ' + (($kisiler | ForEach-Object { Kisi $_ }) -join ' ')
    if ($ekipler.Count -gt 0) { '  Ekip      : ' + (($ekipler | ForEach-Object { Kisi $_ }) -join ' ') }
    '  Notlar    : ' + $paragraf

    if ($EkKlasoru -ne '' -and $r.Ekler.Count -gt 0) {
        New-Item -ItemType Directory -Force $EkKlasoru | Out-Null
        foreach ($a in $r.Ekler) { [IO.File]::WriteAllBytes((Join-Path $EkKlasoru $a.Ad), $a.Veri) }
        '  Ekler kaydedildi: ' + $EkKlasoru
    }
}
