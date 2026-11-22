# Test yardımcı aracı sayfası mock'ları: test_araci_sablon.xaml şablonunu her tür için doldurur ve PNG'ye çizer.
# Çalıştırma: powershell -NoProfile -ExecutionPolicy Bypass -STA -File docs\mock\test_araci_mocklar.ps1
# Türler, simgeler, renkler, alanlar ve klasörler TypeSet.CreateDefault ile aynıdır.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$template = [IO.File]::ReadAllText((Join-Path $here 'test_araci_sablon.xaml'))

function Esc([string]$s) { [Security.SecurityElement]::Escape($s) }

function Pair([string]$k, [string]$v)
{
    $val = if ($v -eq '') { '<TextBlock Foreground="{DynamicResource TextTertiaryBrush}" Text="—" />' } else { '<TextBlock TextWrapping="Wrap" Text="' + (Esc $v) + '" />' }
    '<StackPanel Margin="0,0,14,9"><TextBlock Style="{StaticResource Key}" Text="' + (Esc $k) + '" />' + $val + '</StackPanel>'
}

function User([string]$no, [string]$name, [bool]$here)
{
    $tag = if ($here) { '<Border Style="{StaticResource Chip}" Margin="10,0,0,0" Background="{DynamicResource SystemFillColorSuccessBackgroundBrush}"><TextBlock FontSize="11" Foreground="{DynamicResource SystemFillColorSuccessBrush}" Text="Klasörü burada" /></Border>' } else { '' }
    '<StackPanel Orientation="Horizontal" Margin="0,0,0,6"><TextBlock Width="112" FontWeight="SemiBold" VerticalAlignment="Center" Text="' + (Esc $no) + '" /><TextBlock VerticalAlignment="Center" Foreground="{DynamicResource TextSecondaryBrush}" Text="' + (Esc $name) + '" />' + $tag + '</StackPanel>'
}

function FileBtn([string]$folder, [string]$info)
{
    '<Border Style="{StaticResource Btn}" Margin="0,0,8,6"><TextBlock><Run Text="' + (Esc $folder) + '" /><Run Foreground="{DynamicResource TextTertiaryBrush}" Text="  · ' + (Esc $info) + '" /></TextBlock></Border>'
}

# Ton: success, caution, critical, accent (çerçeveli), neutral.
function Chip([string]$text, [string]$tone, [double]$size = 11.5)
{
    switch ($tone)
    {
        'success'  { return '<Border Style="{StaticResource Chip}" Background="{DynamicResource SystemFillColorSuccessBackgroundBrush}"><TextBlock FontSize="' + $size + '" Foreground="{DynamicResource SystemFillColorSuccessBrush}" Text="' + (Esc $text) + '" /></Border>' }
        'caution'  { return '<Border Style="{StaticResource Chip}" Background="{DynamicResource SystemFillColorCautionBackgroundBrush}"><TextBlock FontSize="' + $size + '" Foreground="{DynamicResource SystemFillColorCautionBrush}" Text="' + (Esc $text) + '" /></Border>' }
        'critical' { return '<Border Style="{StaticResource Chip}" Background="{DynamicResource SystemFillColorCriticalBackgroundBrush}"><TextBlock FontSize="' + $size + '" Foreground="{DynamicResource SystemFillColorCriticalBrush}" Text="' + (Esc $text) + '" /></Border>' }
        'accent'   { return '<Border Style="{StaticResource Chip}" Background="Transparent" BorderThickness="1" BorderBrush="{DynamicResource AccentTextFillColorPrimaryBrush}"><TextBlock FontSize="' + $size + '" Foreground="{DynamicResource AccentTextFillColorPrimaryBrush}" Text="' + (Esc $text) + '" /></Border>' }
        default    { return '<Border Style="{StaticResource Chip}"><TextBlock FontSize="' + $size + '" Text="' + (Esc $text) + '" /></Border>' }
    }
}

function Dot([string]$tone)
{
    $brush = switch ($tone) { 'success' { '{DynamicResource SystemFillColorSuccessBrush}' } 'caution' { '{DynamicResource SystemFillColorCautionBrush}' } 'critical' { '{DynamicResource SystemFillColorCriticalBrush}' } 'accent' { '{DynamicResource AccentTextFillColorPrimaryBrush}' } default { '{DynamicResource TextTertiaryBrush}' } }
    '<Ellipse Style="{StaticResource Dot}" Fill="' + $brush + '" />'
}

# Olay: tür çipi + tarih · kişi, başlık, ayrıntı (boş olabilir).
function Ev([string]$kind, [string]$tone, [string]$when, [string]$title, [string]$detail, [bool]$last = $false)
{
    $x = '<Grid Margin="0,0,0,' + $(if ($last) { '0' } else { '14' }) + '">' + (Dot $tone) + '<StackPanel>'
    $x += '<StackPanel Orientation="Horizontal">' + (Chip $kind $tone) + '<TextBlock Style="{StaticResource EvDate}" Text="' + (Esc $when) + '" /></StackPanel>'
    if ($title -ne '') { $x += '<TextBlock Margin="0,5,0,0" Text="' + (Esc $title) + '" />' }
    if ($detail -ne '') { $x += '<TextBlock Margin="0,2,0,0" Style="{StaticResource Dim}" Text="' + (Esc $detail) + '" />' }
    $x + '</StackPanel></Grid>'
}

function NoteBlock([string]$heading, [string[]]$lines)
{
    $x = '<TextBlock FontWeight="SemiBold" Margin="0,0,0,3" Text="' + (Esc $heading) + '" />'
    foreach ($l in $lines) { $x += '<TextBlock Margin="0,0,0,2" TextWrapping="Wrap" Foreground="{DynamicResource TextSecondaryBrush}" Text="' + (Esc $l) + '" />' }
    '<StackPanel Margin="0,0,0,8">' + $x + '</StackPanel>'
}

function Rule([string]$key, [string]$text, [bool]$break = $true)
{
    '<Run FontWeight="SemiBold" Foreground="{DynamicResource TextPrimaryBrush}" Text="' + (Esc $key) + ' " /><Run Text="' + (Esc $text) + '" />' + $(if ($break) { '<LineBreak />' } else { '' })
}

# Fotoğraf kartı: kapak çizimi (XAML), kapak dosya adı, şerit (ad listesi), klasör, numarasız sayısı.
function Photos([string]$cover, [string]$coverFile, [string[]]$strip, [string]$folder, [int]$hidden)
{
    $count = $strip.Count
    $x = '<Border Style="{StaticResource NewCard}"><StackPanel>'
    $x += '<DockPanel Margin="0,0,0,10"><Border DockPanel.Dock="Right" Style="{StaticResource Btn}" Padding="8,1"><TextBlock FontSize="12" Text="Klasör" /></Border>'
    $x += '<TextBlock Style="{StaticResource CardTitle}"><Run Text="Fotoğraflar" /><Run Foreground="{DynamicResource TextTertiaryBrush}" Text="  · ' + $count + '" /></TextBlock></DockPanel>'
    $x += '<Border Height="236" CornerRadius="6" Background="#2E3440" ClipToBounds="True"><Grid>' + $cover
    $x += '<Border HorizontalAlignment="Left" VerticalAlignment="Bottom" Margin="8" Background="#99000000" CornerRadius="4" Padding="7,1"><TextBlock FontSize="11" Foreground="White" Text="' + (Esc $coverFile) + '" /></Border></Grid></Border>'
    $x += '<UniformGrid Rows="1" Columns="5" Margin="0,8,0,0">'
    for ($i = 0; $i -lt $strip.Count; $i++)
    {
        $sel = if ($i -eq 0) { ' BorderBrush="{DynamicResource AccentTextFillColorPrimaryBrush}"' } else { '' }
        $no = if ($i -eq 0) { 'Kapak' } else { [string]$i }
        $x += '<StackPanel Margin="0,0,6,0"><Border Style="{StaticResource Thumb}"' + $sel + '><TextBlock Style="{StaticResource ThumbText}" Text="' + (Esc $strip[$i]) + '" /></Border><TextBlock Style="{StaticResource ThumbNo}" Text="' + $no + '" /></StackPanel>'
    }
    $x += '</UniformGrid>'
    $tail = if ($hidden -gt 0) { ' · numarasız ' + $hidden + ' fotoğraf gösterilmiyor · ' } else { ' · ' }
    $x += '<TextBlock Margin="0,8,0,0" FontSize="11.5" Foreground="{DynamicResource TextTertiaryBrush}"><Run Text="' + (Esc ($folder + $tail)) + '" /><Run Foreground="{DynamicResource AccentTextFillColorPrimaryBrush}" Text="klasörü aç" /></TextBlock>'
    $x + '</StackPanel></Border>'
}

$icons = @{
    cable = 'M1.6,4.8 H4.6 M4.6,3.2 H8.4 V6.4 H4.6 Z M6.5,3.2 V6.4 M8.4,4.8 C12.6,4.8 13.6,7.6 10.8,9.6 C8.2,11.4 9.2,14.2 14.4,14.2'
    jig = 'M13.8,4.6 A3.5,3.5 0 0 1 9.2,8.6 L4.2,13.6 A1.4,1.4 0 0 1 2.4,11.8 L7.4,6.8 A3.5,3.5 0 0 1 11.4,2.2 L9.6,4 L10.2,5.8 L12,6.4 Z'
    software = 'M5.5,4.5 L2,8 L5.5,11.5 M10.5,4.5 L14,8 L10.5,11.5 M9,3.2 L7,12.8'
    equipment = 'M4.5,5.2 H11.5 V10.8 H4.5 Z M1.6,6.8 H4.5 V9.2 H1.6 Z M11.5,6.8 H14.4 V9.2 H11.5 Z M6.8,5.2 V10.8 M9.2,5.2 V10.8'
    other = 'M3.2,8 m-1.15,0 a1.15,1.15 0 1 0 2.3,0 a1.15,1.15 0 1 0 -2.3,0 M8,8 m-1.15,0 a1.15,1.15 0 1 0 2.3,0 a1.15,1.15 0 1 0 -2.3,0 M12.8,8 m-1.15,0 a1.15,1.15 0 1 0 2.3,0 a1.15,1.15 0 1 0 -2.3,0'
}

$photoRule = 'kapak <numara>_kapak, şerit <numara>_1, _2 … (stoktaki kural); numarasızlar görünmez, yalnız sayısı yazılır; kapak yoksa ilk numaralı, o da yoksa türün simgesi.'
$noteRule = 'ana notun gövdesi salt okunur (YAML ve ## Günlük hariç); değiştirmek MD ile.'

$mocks = @(
    @{
        File = 'test_araci_kablaj'; Caption = 'MOCK · TEST YARDIMCI ARACI · KABLAJ (RF) · mavi çerçeveli kartlar yeni'
        Icon = $icons.cable; Color = '#F7A35C'; Number = 'JT-1000-0007'; Name = 'Faz kararlı RF kablo, 1,5 m'
        Parent = 'AB-7000-0003'; Kind = 'Kablaj · RF'; Status = 'Kullanımda'; Since = '02.11.2026 tarihinden beri'
        HeadChips = (Chip '✓ Kalibrasyon 12.11.2027''ye kadar' 'success' 12)
        Media = (Photos '<Path Stroke="#C9CED8" StrokeThickness="7" Data="M 70,180 C 150,40 270,215 340,95 S 410,70 432,128" /><Rectangle Width="36" Height="22" Fill="#E0B050" RadiusX="3" RadiusY="3" HorizontalAlignment="Left" VerticalAlignment="Top" Margin="48,170,0,0" /><Rectangle Width="40" Height="26" Fill="#9AA3B2" RadiusX="3" RadiusY="3" HorizontalAlignment="Left" VerticalAlignment="Top" Margin="414,116,0,0" />' 'JT-1000-0007_kapak.jpg' @('Kapak', 'SMA ucu', 'N ucu', 'Etiket') '02_Fotograflar\' 2)
        Process = (Pair 'Düzey' 'Stok · AB-7000-0003') + (Pair 'Talep' '20.10.2026 · katan') + (Pair 'Yapan' 'Atölye') + (Pair 'Teslim' '01.11.2026')
        UsersCount = '  · 2'; Users = (User 'AB-7000-0003' 'DA400 Döner Anten' $true) + (User 'AB-7000-0002' 'SH2000 Horn' $false)
        FieldsTitle = 'Özellikler · RF · 2 / 2 dolu'; Fields = (Pair 'Boy' '1,5 m') + (Pair 'Frekans' '18000 MHz')
        Files = (FileBtn '01_Belgeler' '2 PDF') + (FileBtn 'Kalibrasyon' '1 PDF · 2 Touchstone')
        NoteFile = '  · JT-1000-0007.md'
        Note = (NoteBlock 'Kullanım' @('SATIMO ölçümlerinde referans kablo. Bükme yarıçapı en az 50 mm; tork 0,9 N·m.')) + (NoteBlock 'Yapılacaklar' @('☐  Yedek SMA adaptörü sipariş et', '☑  Etiket yenilendi  ✅ 05.11.2026'))
        EventsCount = '  · 5'; Rail = '#55F7A35C'
        Filters = (Chip 'Aşama' 'neutral' 12) + (Chip 'Kalibrasyon' 'neutral' 12) + (Chip 'Tamir / bakım' 'neutral' 12) + (Chip 'Konum' 'neutral' 12) + (Chip 'Not' 'neutral' 12)
        Events = (Ev 'Kalibrasyon' 'success' '12.11.2026 · Metroloji lab.' 'Faz / kayıp doğrulaması: geçti' 'Sertifika K-2026-114 · geçerlilik 12.11.2027 · Kalibrasyon\2026-11-12') +
                 (Ev 'Tamir' 'caution' '05.11.2026 · Atölye' 'SMA ucunun konnektörü değişti' 'Tamirden sonra kalibrasyon gerekir; 12.11''de yapıldı.') +
                 (Ev 'Konum' 'neutral' '03.11.2026 · katan' 'Yankısız oda, dolap 2' '') +
                 (Ev 'Kullanımda' 'accent' '02.11.2026 · katan' '' '') +
                 (Ev 'Talep' 'neutral' '20.10.2026 · katan' 'SATIMO referans kablosu' '' $true)
        Rules = (Rule 'Fotoğraf:' ('02_Fotograflar\ klasöründen; ' + $photoRule)) + (Rule 'Not:' $noteRule) +
                (Rule 'Olaylar:' 'Geçmiş kartı Olaylar olur; yeni türler Kalibrasyon (tarih, yapan, sonuç, sertifika no, geçerlilik, klasör) ve Tamir / bakım. Başlıktaki çip son kalibrasyonun geçerliliği: yeşil geçerli, sarı son 30 gün, kırmızı geçmiş.' $false)
    },
    @{
        File = 'test_araci_mekanik_jig'; Caption = 'MOCK · TEST YARDIMCI ARACI · MEKANİK JİG · mavi çerçeveli kartlar yeni'
        Icon = $icons.jig; Color = '#B4A7F5'; Number = 'JT-1000-0014'; Name = 'Döner tabla montaj aparatı'
        Parent = 'AB-7000-0003'; Kind = 'Mekanik jig · SATIMO'; Status = 'Hazırlanıyor'; Since = '30.10.2026 tarihinden beri'
        HeadChips = (Chip 'Çizim rev. B' 'neutral' 12)
        Media = (Photos '<Rectangle Width="300" Height="22" Fill="#9AA3B2" RadiusX="3" RadiusY="3" HorizontalAlignment="Center" VerticalAlignment="Top" Margin="0,170,0,0" /><Rectangle Width="90" Height="80" Fill="#C9CED8" HorizontalAlignment="Center" VerticalAlignment="Top" Margin="0,92,0,0" /><Ellipse Width="70" Height="70" Stroke="#E0B050" StrokeThickness="9" HorizontalAlignment="Center" VerticalAlignment="Top" Margin="0,30,0,0" /><Rectangle Width="16" Height="30" Fill="#7D8594" HorizontalAlignment="Left" VerticalAlignment="Top" Margin="100,192,0,0" /><Rectangle Width="16" Height="30" Fill="#7D8594" HorizontalAlignment="Left" VerticalAlignment="Top" Margin="332,192,0,0" />' 'JT-1000-0014_kapak.jpg' @('Kapak', 'Önden', 'Montajlı', 'Cıvata detayı') '03_Fotograflar\' 1)
        Process = (Pair 'Düzey' 'Stok · AB-7000-0003') + (Pair 'Talep' '28.10.2026 · katan') + (Pair 'Yapan' 'M. Yılmaz') + (Pair 'Teslim' '')
        UsersCount = '  · 1'; Users = (User 'AB-7000-0003' 'DA400 Döner Anten' $true)
        FieldsTitle = 'Özellikler · SATIMO · 6 / 6 dolu'
        Fields = (Pair 'Malzeme' 'Al 6061') + (Pair 'Ölçüler' '400 × 300 × 20 mm') + (Pair 'Yüzey' 'Eloksal') + (Pair 'Bağlantı' '4 × M6') + (Pair 'Tork' '9 Nm') + (Pair 'Çizim revizyonu' 'B')
        Files = (FileBtn '01_Cizimler' '2 STEP · 1 PDF') + (FileBtn '02_Belgeler' '1 PDF')
        NoteFile = '  · JT-1000-0014.md'
        Note = (NoteBlock 'Amaç' @('DA400 antenini SATIMO döner tablasına bağlar; merkez kaçıklığı en fazla 0,5 mm.')) + (NoteBlock 'Yapılacaklar' @('☐  Cıvata deliklerini teknik resimle karşılaştır', '☑  Malzeme Al 6061 seçildi  ✅ 29.10.2026'))
        EventsCount = '  · 4'; Rail = '#55B4A7F5'
        Filters = (Chip 'Aşama' 'neutral' 12) + (Chip 'Revizyon' 'neutral' 12) + (Chip 'Tamir / bakım' 'neutral' 12) + (Chip 'Konum' 'neutral' 12) + (Chip 'Not' 'neutral' 12)
        Events = (Ev 'Revizyon' 'accent' '04.11.2026 · M. Yılmaz' 'Çizim A → B' 'Cıvata delikleri 2 mm kaydırıldı; 01_Cizimler\JT-1000-0014_revB.step') +
                 (Ev 'Hazırlanıyor' 'accent' '30.10.2026 · M. Yılmaz' '' '') +
                 (Ev 'Not' 'success' '29.10.2026 · katan' 'Malzeme Al 6061 seçildi' 'Atölyede var, sipariş gerekmiyor.') +
                 (Ev 'Talep' 'neutral' '28.10.2026 · katan' 'DA400 döner tabla için' '' $true)
        Rules = (Rule 'Fotoğraf:' ('03_Fotograflar\ klasöründen; ' + $photoRule)) + (Rule 'Not:' $noteRule) +
                (Rule 'Olaylar:' 'mekanik jigde kalibrasyon yok; Revizyon (çizim harfi) ve Tamir / bakım olur. Başlıkta son çizim revizyonu çip olarak görünür.' $false)
    },
    @{
        File = 'test_araci_yazilim'; Caption = 'MOCK · TEST YARDIMCI ARACI · YAZILIM · mavi çerçeveli kartlar yeni (fotoğraf yerine Sürümler)'
        Icon = $icons.software; Color = '#4CC38A'; Number = 'JY-7000-0021'; Name = 'SATIMO ölçüm otomasyonu'
        Parent = 'AB-7000-0003'; Kind = 'Yazılım'; Status = 'Kullanımda'; Since = '15.11.2026 tarihinden beri'
        HeadChips = (Chip 'Sürüm 2.3.1' 'success' 12)
        Media = '<Border Style="{StaticResource NewCard}"><StackPanel><DockPanel Margin="0,0,0,10"><Border DockPanel.Dock="Right" Style="{StaticResource Btn}" Padding="8,1"><TextBlock FontSize="12" Text="+  Sürüm" /></Border><TextBlock Style="{StaticResource CardTitle}"><Run Text="Sürümler" /><Run Foreground="{DynamicResource TextTertiaryBrush}" Text="  · 3" /></TextBlock></DockPanel>' +
                 '<Grid><Grid.ColumnDefinitions><ColumnDefinition Width="70" /><ColumnDefinition Width="90" /><ColumnDefinition Width="*" /></Grid.ColumnDefinitions><Grid.RowDefinitions><RowDefinition Height="Auto" /><RowDefinition Height="Auto" /><RowDefinition Height="Auto" /></Grid.RowDefinitions>' +
                 '<TextBlock FontWeight="SemiBold" Foreground="{DynamicResource SystemFillColorSuccessBrush}" Text="2.3.1" /><TextBlock Grid.Column="1" Style="{StaticResource EvDate}" VerticalAlignment="Top" Text="15.11.2026" /><TextBlock Grid.Column="2" Margin="0,0,0,8" TextWrapping="Wrap" Text="Faz hesabında işaret düzeltildi (hata 14.11)" />' +
                 '<TextBlock Grid.Row="1" FontWeight="SemiBold" Text="2.3.0" /><TextBlock Grid.Row="1" Grid.Column="1" Style="{StaticResource EvDate}" VerticalAlignment="Top" Text="02.11.2026" /><TextBlock Grid.Row="1" Grid.Column="2" Margin="0,0,0,8" TextWrapping="Wrap" Text="360° tarama, Touchstone çıktısı" />' +
                 '<TextBlock Grid.Row="2" FontWeight="SemiBold" Text="2.2.0" /><TextBlock Grid.Row="2" Grid.Column="1" Style="{StaticResource EvDate}" VerticalAlignment="Top" Text="10.10.2026" /><TextBlock Grid.Row="2" Grid.Column="2" TextWrapping="Wrap" Text="İlk kullanım sürümü" /></Grid>' +
                 '<TextBlock Margin="0,8,0,0" FontSize="11.5" Foreground="{DynamicResource TextTertiaryBrush}" Text="Sürüm olayları; en üstteki başlıktaki çip ve Özellikler · Sürüm alanı olur." /></StackPanel></Border>'
        Process = (Pair 'Düzey' 'Stok · AB-7000-0003') + (Pair 'Talep' '01.10.2026 · katan') + (Pair 'Yapan' 'A. Kaya') + (Pair 'Teslim' '10.10.2026')
        UsersCount = '  · 2'; Users = (User 'AB-7000-0003' 'DA400 Döner Anten' $true) + (User 'AB-7000-0002' 'SH2000 Horn' $false)
        FieldsTitle = 'Özellikler · 4 / 4 dolu'
        Fields = (Pair 'Sürüm' '2.3.1') + (Pair 'Dil / platform' 'Python') + (Pair 'Konum' '\\sunucu\araclar\satimo') + (Pair 'Bağlı cihaz' 'VNA ZNB20')
        Files = (FileBtn '01_Kaynak' '14 dosya') + (FileBtn '02_Belgeler' '1 PDF · 1 Word')
        NoteFile = '  · JY-7000-0021.md'
        Note = (NoteBlock 'Kullanım' @('ayarlar.json''da VNA adresi; ölçümden önce kalibrasyon dosyası seçilir.')) + (NoteBlock 'Yapılacaklar' @('☐  Kullanıcı kılavuzunu 2.3''e göre güncelle'))
        EventsCount = '  · 5'; Rail = '#554CC38A'
        Filters = (Chip 'Aşama' 'neutral' 12) + (Chip 'Sürüm' 'neutral' 12) + (Chip 'Hata' 'neutral' 12) + (Chip 'Not' 'neutral' 12)
        Events = (Ev 'Sürüm 2.3.1' 'success' '15.11.2026 · A. Kaya' 'Faz hesabında işaret düzeltildi' '01_Kaynak · etiket v2.3.1') +
                 (Ev 'Hata · çözüldü' 'critical' '14.11.2026 · katan' 'Faz 180° ters çıkıyor' 'AB-7000-0003 ölçümünde görüldü; 2.3.1 ile çözüldü.') +
                 (Ev 'Sürüm 2.3.0' 'success' '02.11.2026 · A. Kaya' '360° tarama, Touchstone çıktısı' '') +
                 (Ev 'Kullanımda' 'accent' '10.10.2026 · katan' '' '') +
                 (Ev 'Talep' 'neutral' '01.10.2026 · katan' 'SATIMO ölçümünü otomatikleştir' '' $true)
        Rules = (Rule 'Fotoğraf:' 'yazılımda fotoğraf klasörü yok; yerine Sürümler kartı. Ekran görüntüsü istenirse 02_Belgeler\ içindeki resimler şerit olarak gösterilebilir (soru).') + (Rule 'Not:' $noteRule) +
                (Rule 'Olaylar:' 'Sürüm (no, açıklama) ve Hata (açık / çözüldü; Sorun kaydıyla aynı) eklenir; kalibrasyon ve tamir yok. Son sürüm başlıkta çip.' $false)
    },
    @{
        File = 'test_araci_cihaz'; Caption = 'MOCK · TEST YARDIMCI ARACI · CİHAZ · mavi çerçeveli kartlar yeni'
        Icon = $icons.equipment; Color = '#5FD4C4'; Number = 'JT-1000-0031'; Name = '20 dB zayıflatıcı, 18 GHz'
        Parent = '7000_MILKON'; Kind = 'Cihaz · Zayıflatıcı'; Status = 'Kullanımda'; Since = '15.12.2025 tarihinden beri'
        HeadChips = (Chip '! Kalibrasyon 05.12.2026''da bitiyor' 'caution' 12)
        Media = (Photos '<Rectangle Width="200" Height="56" Fill="#C9CED8" RadiusX="8" RadiusY="8" HorizontalAlignment="Center" VerticalAlignment="Center" /><Rectangle Width="40" Height="30" Fill="#E0B050" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="84,0,0,0" /><Rectangle Width="40" Height="30" Fill="#E0B050" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,84,0" /><TextBlock Text="20 dB" FontSize="20" FontWeight="SemiBold" Foreground="#2E3440" HorizontalAlignment="Center" VerticalAlignment="Center" />' 'JT-1000-0031_kapak.jpg' @('Kapak', 'Etiket', 'Konnektör') '02_Fotograflar\' 0)
        Process = (Pair 'Düzey' 'Proje · 7000_MILKON') + (Pair 'Talep' '01.12.2025 · katan') + (Pair 'Yapan' 'Satın alma') + (Pair 'Teslim' '15.12.2025')
        UsersCount = '  · 2'; Users = (User 'AB-7000-0003' 'DA400 Döner Anten' $false) + (User 'AB-7000-0001' 'Dipol dizi' $false)
        FieldsTitle = 'Özellikler · Zayıflatıcı · 4 / 4 dolu'
        Fields = (Pair 'Marka / model' 'Mini-Circuits BW-S20W2+') + (Pair 'Seri no' '2318-044') + (Pair 'Frekans' '0–18000 MHz') + (Pair 'Kalibrasyon bitişi' '05.12.2026  (son kalibrasyondan)')
        Files = (FileBtn '01_Belgeler' '1 PDF') + (FileBtn 'Kalibrasyon' '2 PDF · 2 Touchstone')
        NoteFile = '  · JT-1000-0031.md'
        Note = (NoteBlock 'Kullanım' @('VNA giriş koruması; en fazla 2 W. Konnektörü tork anahtarıyla sık.'))
        EventsCount = '  · 4'; Rail = '#555FD4C4'
        Filters = (Chip 'Aşama' 'neutral' 12) + (Chip 'Kalibrasyon' 'neutral' 12) + (Chip 'Tamir / bakım' 'neutral' 12) + (Chip 'Konum' 'neutral' 12) + (Chip 'Not' 'neutral' 12)
        Events = (Ev 'Konum' 'neutral' '10.11.2026 · katan' 'VNA tezgâhı' '') +
                 (Ev 'Kalibrasyon' 'success' '05.12.2025 · Metroloji lab.' 'Zayıflama 20,02 dB, uyumlu' 'Sertifika K-2025-311 · geçerlilik 05.12.2026 · Kalibrasyon\2025-12-05') +
                 (Ev 'Kullanımda' 'accent' '15.12.2025 · katan' '' '') +
                 (Ev 'Talep' 'neutral' '01.12.2025 · katan' 'Satın alma, 2 adet' '' $true)
        Rules = (Rule 'Fotoğraf:' ('02_Fotograflar\ klasöründen; ' + $photoRule)) + (Rule 'Not:' $noteRule) +
                (Rule 'Kalibrasyon:' 'son kalibrasyon olayından geçerlilik okunur; mevcut "Kalibrasyon bitişi" alanı elle girilmez, olaydan dolar. Sarı: son 30 gün (bugün 22.11.2026). Ana sayfa Yapılacaklar''da "kalibrasyonu yaklaşan" listesi (soru).' $false)
    },
    @{
        File = 'test_araci_diger'; Caption = 'MOCK · TEST YARDIMCI ARACI · DİĞER · mavi çerçeveli kartlar yeni'
        Icon = $icons.other; Color = '#A0A0A0'; Number = 'JT-1000-0040'; Name = 'Anten tutucu direk (ahşap)'
        Parent = 'AB-7000-0002'; Kind = 'Diğer'; Status = 'Teslim alındı'; Since = '18.11.2026 tarihinden beri'
        HeadChips = ''
        Media = (Photos '<Rectangle Width="18" Height="190" Fill="#B08A5A" HorizontalAlignment="Center" VerticalAlignment="Top" Margin="0,30,0,0" /><Rectangle Width="120" Height="14" Fill="#9AA3B2" HorizontalAlignment="Center" VerticalAlignment="Top" Margin="0,212,0,0" /><Rectangle Width="60" Height="26" Fill="#C9CED8" HorizontalAlignment="Center" VerticalAlignment="Top" Margin="0,22,0,0" />' 'JT-1000-0040_kapak.jpg' @('Kapak', 'Kelepçe') '02_Fotograflar\' 0)
        Process = (Pair 'Düzey' 'Stok · AB-7000-0002') + (Pair 'Talep' '10.11.2026 · katan') + (Pair 'Yapan' 'Marangoz atölyesi') + (Pair 'Teslim' '18.11.2026')
        UsersCount = '  · 1'; Users = (User 'AB-7000-0002' 'SH2000 Horn' $true)
        FieldsTitle = 'Özellikler'
        Fields = '<TextBlock Style="{StaticResource Dim}" Text="Bu türün alanı yok; Türler penceresinden eklenebilir." />'
        Files = (FileBtn '01_Belgeler' 'boş')
        NoteFile = '  · JT-1000-0040.md'
        Note = (NoteBlock 'Kullanım' @('Açık alan ölçümünde anteni 2 m yükseklikte tutar; metal parça yok.'))
        EventsCount = '  · 3'; Rail = '#55A0A0A0'
        Filters = (Chip 'Aşama' 'neutral' 12) + (Chip 'Tamir / bakım' 'neutral' 12) + (Chip 'Konum' 'neutral' 12) + (Chip 'Not' 'neutral' 12)
        Events = (Ev 'Teslim alındı' 'accent' '18.11.2026 · katan' '' '') +
                 (Ev 'Hazırlanıyor' 'accent' '12.11.2026 · katan' '' '') +
                 (Ev 'Talep' 'neutral' '10.11.2026 · katan' 'Açık alan için metal içermeyen direk' '' $true)
        Rules = (Rule 'Fotoğraf:' ('02_Fotograflar\ klasöründen; ' + $photoRule)) + (Rule 'Not:' $noteRule) +
                (Rule 'Olaylar:' 'ortak türler (Aşama, Konum, Not) ve Tamir / bakım; kalibrasyon kaydı eklenirse çip burada da çıkar.' $false)
    }
)

$render = Join-Path $here '..\..\tools\Mock-Ciz.ps1'
foreach ($m in $mocks)
{
    $x = $template
    $map = @{
        '{{CAPTION}}' = (Esc $m.Caption); '{{ICON}}' = $m.Icon; '{{COLOR}}' = $m.Color; '{{NUMBER}}' = (Esc $m.Number); '{{NAME}}' = (Esc $m.Name)
        '{{PARENT}}' = (Esc $m.Parent); '{{KIND}}' = (Esc $m.Kind); '{{STATUS}}' = (Esc $m.Status); '{{SINCE}}' = (Esc $m.Since)
        '{{HEADCHIPS}}' = $m.HeadChips; '{{MEDIA}}' = $m.Media; '{{PROCESS}}' = $m.Process; '{{USERS_COUNT}}' = (Esc $m.UsersCount); '{{USERS}}' = $m.Users
        '{{FIELDS_TITLE}}' = (Esc $m.FieldsTitle); '{{FIELDS}}' = $m.Fields; '{{FILES}}' = $m.Files; '{{NOTE_FILE}}' = (Esc $m.NoteFile); '{{NOTE}}' = $m.Note
        '{{EVENTS_COUNT}}' = (Esc $m.EventsCount); '{{RAIL}}' = $m.Rail; '{{FILTERS}}' = $m.Filters; '{{EVENTS}}' = $m.Events; '{{RULES}}' = $m.Rules
    }
    foreach ($k in $map.Keys) { $x = $x.Replace($k, [string]$map[$k]) }
    $tmp = Join-Path $env:TEMP ($m.File + '.xaml')
    [IO.File]::WriteAllText($tmp, $x, (New-Object Text.UTF8Encoding $true))
    & powershell -NoProfile -ExecutionPolicy Bypass -STA -File $render -Xaml $tmp -Png (Join-Path $here ($m.File + '.png'))
    Remove-Item $tmp
}
