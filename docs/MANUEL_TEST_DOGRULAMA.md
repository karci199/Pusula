# Manuel test: Doğrulama · Gereksinimler / Plan / Rapor

Bir stok seçin (örnek: `AB-7000-0002`), alttaki **Doğrulamalar** sekmesini açın. Her adımdan sonra
`03_Testler\<stok>_Test_Gereksinimleri.md` dosyasını Obsidian'da açıp bakabilirsiniz.

Başlamadan önce dosyanın bir kopyasını alın (uygulama silmez, ama karşılaştırmak kolay olur).

## 1. İlk açılış (taşıma)

| # | Yap | Beklenen |
|---|-----|----------|
| 1.1 | Eski düzende (satırda TP / TR yazılı) bir stoku açın | Durum çubuğu: "Plan ve rapor tabloları … kuruldu (kopyalandı…)". Md'de `## Plan` ve `## Rapor` altında tablolar var |
| 1.2 | Gereksinim tablosuna bakın | Hiçbir satır silinmedi; eski TP/TR açıklamaları yerinde |
| 1.3 | Sekmeleri gezin | Gereksinimler · Plan · Rapor; rozetlerde sayılar, yazılar okunur (siyah yok) |

## 2. Kapsama uyarısı

| # | Yap | Beklenen |
|---|-----|----------|
| 2.1 | Gereksinimler sekmesi | Planı olmayan gereksinim varsa sarı uyarı: "N gereksinim hiçbir plan maddesinde yok: …" |
| 2.2 | Plan sekmesi | Raporu olmayan plan maddesi varsa uyarıda listelenir; satırda sarı "rapor yok" |
| 2.3 | Rapor sekmesi | Plana bağlı olmayan rapor varsa uyarıda listelenir; satırda sarı "plan yok" |
| 2.4 | Gereksinimler: "Raporları göster" / Rapor: "Gereksinimleri göster" | Sütun açılır / kapanır; varsayılan kapalı |

## 3. Plan maddesi

| # | Yap | Beklenen |
|---|-----|----------|
| 3.1 | Plan sekmesi → **+ Plan maddesi** | Pencere "Plan maddesi"; TP no sıradaki önerilir (4.4 → 4.5); Sil düğmesi yok |
| 3.2 | Gereksinimler listesinde arama kutusuna "DGÖ" yazın | Liste süzülür; sağda her gereksinimin diğer planları ya da sarı "plan yok" |
| 3.3 | İki gereksinim işaretleyin, yöntem/altyapı/not girin, Kaydet | Satır Plan tablosunda; md'de `## Plan` tablosunda yeni satır, "Gereksinimler" hücresinde "DGÖ-x, DGÖ-y" |
| 3.4 | Aynı TP no ile yeni madde kaydetmeyi deneyin | Pencere kapanmaz, "4.5 zaten var." |
| 3.5 | TP no boş bırakıp Kaydet | "TP no boş olamaz." |
| 3.6 | Gereksinimler sekmesi → yeni plana bağlı gereksinim | Plan sütununda mor çip; durum Bekliyor idiyse **Planlandı**; md'de TP no hücresi doldu |
| 3.7 | Satırdaki kalem ya da satıra çift tık | "Plan maddesi · 4.5" düzenleme penceresi, Sil düğmesi var |
| 3.8 | TP no'yu 4.5 → 4.50 yapıp kaydedin | Bu plana bağlı raporlarda numara 4.50 oldu |

## 4. Rapor maddesi

| # | Yap | Beklenen |
|---|-----|----------|
| 4.1 | Rapor sekmesi → **+ Rapor maddesi** | TR no önerilir; plan listesinde sağda her planın gereksinimleri |
| 4.2 | Bir plan seçin, Sonuç **Kaldı**, sonucun yeri olarak stok içinde bir klasör seçin, Kaydet | Md'de `## Rapor` tablosunda satır; yer göreli (`03_Testler\…`) |
| 4.3 | Gereksinimler sekmesi | O planın bütün gereksinimleri **Kaldı**; "Raporları göster" ile kırmızı çip |
| 4.4 | Raporu **Geçti** yapın | Gereksinimin her planının raporu Geçti ise **Geçti**; raporsuz planı varsa Geçti olmaz |
| 4.5 | Raporu **Hazırlanmadı** yapın | Gereksinim **Planlandı** (Yapılıyor / Değerlendiriliyor gibi süren durumlar korunur) |
| 4.6 | Bir gereksinimi **Kapsam dışı** ya da **Şartlı kabul** yapıp rapor değiştirin | O durum değişmez |

## 5. Gereksinim satırı kartları

| # | Yap | Beklenen |
|---|-----|----------|
| 5.1 | Bir gereksinim satırına tıklayıp açın | Test planı kartında bağlı plan maddeleri (TP no, açıklama, yöntem, altyapı, not); Test raporu kartında planlar üzerinden raporlar |
| 5.2 | Kartta **+** | Planda: bu gereksinim seçili yeni plan. Raporda: bu gereksinimin planları seçili yeni rapor |
| 5.3 | Kartta kalem | O plan / rapor maddesi düzenlenir |
| 5.4 | Rapor kartında **Aç** | Sonucun yeri açılır; yoksa durum çubuğunda uyarı |
| 5.5 | Bağsız gereksinim | Kartlarda "Henüz plan yok" / "Henüz rapor yok" ve ekle düğmesi |

## 6. Silme (dosyaya dikkat)

| # | Yap | Beklenen |
|---|-----|----------|
| 6.1 | Rapor düzenleme penceresinde **Sil** | Onay sorulur: "Rapor maddesi … silinsin mi?" Vazgeç → hiçbir şey değişmez |
| 6.2 | Onaylayın | Yalnız `## Rapor` tablosundaki satır gitti; sonuç klasörü **yerinde**; gereksinim durumu yeniden hesaplandı |
| 6.3 | Bir plan maddesini silin | Yalnız `## Plan` satırı gitti; bağlı raporda sarı "plan yok"; gereksinim Bekliyor |
| 6.4 | Md'nin geri kalanı | Başlıklar, elle yazılmış metinler, `%% pusula:… %%` bölümleri aynı |

## 7. Gereksinim TG no değişimi

| # | Yap | Beklenen |
|---|-----|----------|
| 7.1 | Plana bağlı bir gereksinimin TG no'sunu düzenleyin | Plan tablosunda bağ yeni numaraya geçti; çip sarıya dönmedi |

## 8. Obsidian

| # | Yap | Beklenen |
|---|-----|----------|
| 8.1 | Dosyayı Obsidian'da açın | Üç tablo da düzgün görünür (Gereksinimler, Plan, Rapor) |
| 8.2 | Obsidian'da Plan tablosuna elle satır ekleyip kaydedin, uygulamada stoka yeniden tıklayın | Yeni plan görünür |
| 8.3 | Elle bilinmeyen bir sütun ekleyin, uygulamadan maddeyi kaydedin | Sütun ve değeri korunur |

## Bilinen sınırlar

- Gereksinim durumu yalnız plan / rapor kaydedilince ya da silinince yeniden hesaplanır; elle durum değiştirmek
  bir sonraki plan / rapor kaydına kadar kalır.
- Eski satır başı okunur bölümler (`### TG · başlık`) artık yazılmıyor; var olanlar yerinde duruyor.
- Arama kutusunun silik ipucu yazısı görünmüyor (DesignKit davranışı), arama çalışıyor.
