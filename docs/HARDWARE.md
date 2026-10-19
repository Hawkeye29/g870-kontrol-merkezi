# Donanım ve doğrulama kapsamı

> **Test kapsamı:** Bu uygulama yalnızca geliştiricinin kullandığı tek bir **Casper Excalibur G870 / 1362-BV70X-C** bilgisayarda test edilmiştir. Diğer G870 donanım yapılandırmaları, BIOS/firmware sürümleri ve farklı modellerde uyumluluk doğrulanmamıştır. Aynı model adı, aynı donanım ve davranış anlamına gelmeyebilir.

Hedef cihaz: G870 / 1362-BV70X-C. Aynı model adını taşıyan farklı firmware sürümlerinde uyumluluk doğrulanmadı. WMI taşıması `root\wmi` altındaki `RW_GMWMI.BufferBytes` üzerinden 32 bayt paket kullanır.

Sıcaklık/RPM, fan ve performans kontrollerinin çalıştığı geliştirme cihazında kullanıcı tarafından bildirildi. Tüm firmware ve fiziksel MUX geçişleri için test kapsamı bulunmuyor.

Üstteki mod seçimi, üreticinin Office/Gaming/HighPerformance Windows güç planlarını GUID ile uygular. Eksik planlar EXE yanındaki PowerPlans klasöründeki .pow dosyalarından alınır. Windows'tan yapılan güç planı değişikliği bilgi satırına yansır; fan/RGB ayarlarını otomatik değiştirmez.

RGB kaydı, uygulamadan gönderilen son ayardır; canlı fiziksel renk geri okuması değildir. Başka yazılım veya Fn tuşlarıyla değişen renk otomatik okunmaz. OEM RGB firmware yolundaki beklemeler, kullanıcı alanındaki zaman aşımıyla iptal edilemeyebilir.

Fan gözetimi ayrı kullanıcı sürecidir. Sensör hatası ve 80°C sınırında BIOS otomatiğine dönüş denenir. Sistem veya gözetim süreci kilitlenirse geri dönüş garanti edilemez.

MUX desteği firmware'e bağlıdır. Kullanıcı yeniden başlatmayı onaylarsa başarılı MUX komutundan sonra Windows yeniden başlatılır. BitLocker denetimi isteği engelleyebilir; uygulama şifrelemeyi kapatmaz. Yeniden başlatma hataları yerel `mux-restart.log` dosyasına yazılır.

Uygulama ZIP dosyasının tamamını çıkarın ve EXE yanındaki PowerPlans klasörünü koruyun. Güç planları için orijinal Control Center gerekmez. Donanım erişimi yerleşik Windows WmiAcpi sürücüsünden sağlanır. Windows başlangıcı kurulumu üç plan dosyasını da uygulamayla kopyalar.

19 Ekim 2026 cihaz tanılaması: RW_GMWMI örneği ACPI/PNP0C14/0x1_0 üzerinde aktif; eşleşen aygıtın servisi WmiAcpi. WmiAcpi çalışırken OEM ControlCenter sürücüsü durmuş durumdaydı. Uygulama OEM DLL veya SYS çağırmaz. OEM yazılımı tamamen kaldırılmış temiz Windows kurulumu ayrıca test edilmedi.

