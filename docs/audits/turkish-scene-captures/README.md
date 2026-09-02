# Türkçe ürün dili + yeni sahne — yakalama paketi (2026-09-02)

iPhone 16 Pro simülatörü, son koddan; gerçek rotalar (`qaniva://` derin bağlantılar) ve
gerçek Unity düğmelerine basan e2e sürücüsü. Mock yok. Geliştirme sürümü izleri:
açılışta kısa Metro bandı, sürücü koşularında "E2E run n" başlığı.

| Dosya | Ekran / durum | Not |
| --- | --- | --- |
| 01-onboarding-1.png, 02-onboarding-4.png | Tanıtım sayfaları | Türkçe başlık/gövde/adımlar (derin bağlantı sayfa ilerletmesi bu kayıtta tetiklenmedi; iki dosya aynı sayfayı gösterir) |
| 03-home.png | Ana Sayfa | Devam et / Vakalar / İlerlemen; sekmeler Ana Sayfa · Vakalar · İlerleme · Ayarlar |
| 04-cases.png | Vakalar | Türkçe vaka başlıkları ve rozetler (Acil Tıp, En iyi N puan) |
| 05-stemi-briefing.png, 06-anaphylaxis-briefing.png | Vaka Özeti | Türkçe Rolünüz/Ortam/Kaynaklar/Triyaj notu + Göreviniz |
| 07-simulation-default.png | Simülasyon (varsayılan) | **İkinci geçiş sahnesi:** ayak ucundan, ortalanmış ve yükseltilmiş kamera; kollar yanda yatakta, eller gevşek, yarı oturur hasta (CC0 Quaternius tabanlı, kaplı önlük mesh'i, saç); izleyiciye dönük monitör (EKG dalga şeridi + NABIZ yeşil / SpO2 camgöbeği / TA kırmızı / SS sarı); **kenar raylı kategori sekmeleri** (sol: Hasta · Muayene, sağ: İstemler · Tedavi · Diğer) ve altta yüzen Vaka günlüğü / Çık |
| 07b-simulation-treat-panel.png | Tedavi paneli açık | Sekme kendi kenarından panel açar; satırlar başlık + ikincil satır + satır içi durum (Henüz uygun değil); Kapat ile kapanır |
| 08-ecg-viewer.png | 12 derivasyonlu EKG | Türkçe başlık ve yer tutucu uyarısı |
| 09-stemi-results-top.png | Sonuçlar (özet) | Vaka tamamlandı · 88 · Kritik/Zamanlama/Verimlilik/Tedavi/Karar · Kritik kararlar |
| 10-stemi-results-donewell.png | Sonuçlar (iyi yapılanlar) | Türkçe ölçüt satırları, "Zamanında 03:45 · 5/5 puan" |
| 11-stemi-results-references.png | Sonuçlar (kaynaklar) | Kaynaklar + kanıt defteri |
| 12-progress.png … 15-disclaimer.png | İlerleme / Ayarlar / Hakkında / Eğitim Amaçlı Kullanım | Tamamı Türkçe |
| video/stemi-simulation-tr.mp4 | STEMI koşusu | Kenar raylı arayüzle Türkçe simülasyon → paneller → EKG → tamamlanma → Sonuçlar |

Tüm kareler paketlenmeden önce tek tek incelendi.
