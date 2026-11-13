# Olay notu

Bir notun YAML'ında `olay:` ve `tarih:` varsa Pusula onu Günlük'e (stokta) ya da Geçmiş'e (jig / kablo) kendiliğinden koyar.

- `olay:` toplanti, karar, numune, sevkiyat, revizyon, ariza, not (başka bir şey de yazılabilir)
- `tarih:` 2026-11-05
- `kisi:` isteğe bağlı
- Başlık: notun ilk `# satırı`

Not, stokun ya da kaydın klasöründe herhangi bir yerde durabilir; düzen için `Notlar\` önerilir. `_` ile başlayan klasörler okunmaz.
Test koşuları `03_Test_Sonuclari\<SİSTEM>\<SN>_T<tur>\test.md` olarak durur (Test.md şablonu).