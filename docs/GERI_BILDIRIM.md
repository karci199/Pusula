# Geri bildirim — gerçek kullanımdan

Kaynak: el yazısı notlar, 09.11.2026 (6 sayfa). Konularına göre toplandı. `?` = netleşmesi gereken.

## 1. Hatalar

- [ ] **"Not ekle" → "the host was not found"** hatası. ?Hangi düğme, kök ağ sürücüsünde mi
- [ ] **NuGet restore hatası** (derlerken). ?Nerede, mesaj ne; iş bilgisayarında internet yok mu
- [ ] **Hover:** proje ve stok aynı anda vurgulanıyor (üstü de vurgulu). Tek satır vurgulansın.
- [ ] **Türler → Biçim tablosu:** hücre F2 ile düzenlenince arka plan beyaz oluyor (koyu temada).
- [ ] **Stok kapak fotoğrafı** hafifçe kesiliyor.
- [ ] Dosya sayımında **Thumbs.db, desktop.ini** vb. sayılmasın; dosya değişiklikleri iyi incelenmeli.

## 2. Hız ve yenileme

- [ ] Stok sayfası ilk açılışta hafif yavaş.
- [ ] **Yenile** en başa atıyor; **son kalınan yere** (seçili düğüm, sekme, kaydırma) dönülsün.
- [ ] ?Diskteki değişiklikler arada taranıp "değişiklik oldu" denilsin mi (otomatik izleme)

## 3. Düzenleme (eksik)

- [ ] Stok **ekleniyor ama silinemiyor / değiştirilemiyor**. Aynısı jig, kablo vb. için.
  (Silme = takipten çıkarma; dosyalar silinmez.)
- [ ] Jig düzenlenemiyor (ad, alt tür, alanlar …).

## 4. Görünüm

- [ ] **Logo / uygulama ikonu** eklensin.
- [ ] **Yeni stok ve yeni proje** pencereleri daha güzel olsun.
- [ ] Sol üstteki **+ düğmesi** daha güzel olsun.
- [ ] Stok sayfasının **üst kısmı** (numara, yol / dizin) daha iyi görünsün.
- [ ] **Sekmeler sürüklenebilsin** (sıra değiştirilebilsin).
- [ ] Stok açılınca **anten hakkında ön bilgi + foto** gelsin (Genel kartı; zaten var, iyileştirilecek).
- [ ] Jig ve kablo **tamamen ayrılsın**. ?"Genel sayfası açıldığı zaman ne ekliyse o gelsin"
- [ ] ?Genel / Kablolar sekmeli yapı daha iyi olabilir mi

## 5. Notlar ve md

- [ ] Uygulamadan eklenen not **basit** kalsın, **md olmasın** (şimdiki gibi).
- [ ] **Alınan notlar md olarak görünsün:** Notlar kısmında md içeriği, **ayrı pencere** gibi açılsın (okuma görünümü).
- [ ] Ana md'yi "Not oluştur" deyince yapıyor. ?Otomatik oluşturulsun mu; ayar olabilir
- [ ] ?Zaman çizelgesine elle eklenen notlar hepsi `<StokNo>.md` içinde mi dursun
- [ ] ?Proje geneli md olsun mu, içinde neler olur
- [ ] **Şablon md'ler** (tür başına: SATIMO tekil ölçüm vb.); değerler uygulamadan girilsin, açıklamalar elle.
- [ ] **Olay ekle** nasıl olsun: "toplantı yapıldı, şu kararlar alındı" gibi hızlı giriş.

## 6. Bağlantılar (@)

- [ ] Not yazarken **@** basınca kablo, jig, stok, proje, **kişi** taransın ve **bağlantı** olarak eklensin
  (her nesnenin md dosyası olmalı).
- [ ] **SATIMO, yankısız oda** gibi ölçüm altyapısı da bağlanabilir nesne olsun.
- [ ] **161 / DGÖ** maddelerine bağlantı (tasarımcıyla konuşulan maddeler); bunun için ayrı arayüz. ?161, DGÖ ne

## 7. Dokümanlar, revizyon, kodlar

- [ ] Uygulama dokümanı **görüyor ama anlamlandırmıyor**. Tanısın:
  doküman türü (261 …), revizyon, stok türü, durum (aktarıldı mı, hazırlanıyor mu).
- [ ] Doküman adı yapısı: `AB-0000-445_261-AA` ?parçaları (AB = birim, 0000-445 = ?, 261 = doküman türü, AA = revizyon)
- [ ] **2–3 harfli önekler** bir anlama eşlenir: AB = birim, AI = modül, KB = aile stoku … → tanım listesi (Ayarlar).
- [ ] **Doküman durumu:** üzerinde çalışılan · bitmiş · revize gereken · aktarım sürecinde.
- [ ] **Revizyon:** ad içinden mi, ayrı klasör mü (`_Revizyonlar`)? İsimlendirme kuralı.
- [ ] **Doküman aktarımlarının takibi** (SAP'ye aktarım?).
- [ ] **P1 = prototip**; REDO verildiyse işlensin. ?REDO ne

## 8. Fotoğraf

- [ ] **İsimlendirme kuralı:** `<StokNo>_F1` mi, görünüme göre mi (`_kapak`, `_on`, `_arka`, `_montaj` …)?
- [ ] **Sıralama:** hangi sırayla görünsün.
- [ ] Şablonda "konulması istenen fotolar" listesi olsun (eksik olan görünsün).
- [ ] png, jpg ve diğer uzantılar desteklensin (şu an: jpg, jpeg, png, bmp, gif, tif, tiff — ?eksik olan hangisi, ör. heic, webp).

## 9. Stok yapısı

- [ ] **Alt / üst stok** nasıl eklensin.
- [ ] Jig, kablo kaydı içinde **alt klasörler** (01_Cizimler …) açılıyor; gerek var mı? (zaten değiştirilebiliyor) → isteğe bağlı yap.
- [ ] Proje altındaki **tüm türler** (stok, jig, kablo …) tek listede görülsün, sıralansın, filtrelensin. Stok ve genel için de.

## 10. Test

- [ ] Test sonuçları nasıl tanınıyor: `SNP1_T1` klasöründen ve **_kosu.md** (KIWI) dosyasından "GEÇTİ" okundu. Kural yazılsın.
- [ ] **Test planlarını** görsün (`02_Test_Planlari`).
- [ ] Test sonucu md dosyası varsa doğrudan ondan okunsun.

## 11. Takip ve iş listesi

- [ ] **Bekleyen işler** listesi (o anlık): "SAP REDO verildi" gibi, ayrıntısız, basit.
- [ ] **İlerleme durumu** (jig hazır mı, yazılım var mı / hazır mı) — büyük ölçüde elle.
- [ ] Jig teslim edildi mi takibi (envanter durumu var; stok ekranında görünürlüğü).
