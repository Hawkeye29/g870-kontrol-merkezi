# Donanım ve doğrulama kapsamı

Hedef cihaz: G870 / 1362-BV70X-C. Aynı model adını taşıyan farklı firmware sürümlerinde uyumluluk doğrulanmadı. WMI taşıması `root\wmi` altındaki `RW_GMWMI.BufferBytes` üzerinden 32 bayt paket kullanır.

Sıcaklık/RPM, fan ve performans kontrollerinin çalıştığı geliştirme cihazında kullanıcı tarafından bildirildi. Tüm firmware ve fiziksel MUX geçişleri için test kapsamı bulunmuyor.

Üstteki mod seçimi, üreticinin Office/Gaming/HighPerformance Windows güç planlarını GUID ile uygular. Eksik planlar EXE yanındaki PowerPlans klasöründeki .pow dosyalarından alınır. Windows'tan yapılan güç planı değişikliği bilgi satırına yansır; fan/RGB ayarlarını otomatik değiştirmez.

RGB kaydı, uygulamadan gönderilen son ayardır; canlı fiziksel renk geri okuması değildir. Başka yazılım veya Fn tuşlarıyla değişen renk otomatik okunmaz. OEM RGB firmware yolundaki beklemeler, kullanıcı alanındaki zaman aşımıyla iptal edilemeyebilir.

Fan gözetimi ayrı kullanıcı sürecidir. Sensör hatası ve 80°C sınırında BIOS otomatiğine dönüş denenir. Sistem veya gözetim süreci kilitlenirse geri dönüş garanti edilemez.

MUX desteği firmware'e bağlıdır. Kullanıcı yeniden başlatmayı onaylarsa başarılı MUX komutundan sonra Windows yeniden başlatılır. BitLocker denetimi isteği engelleyebilir; uygulama şifrelemeyi kapatmaz. Yeniden başlatma hataları yerel `mux-restart.log` dosyasına yazılır.

Uygulama ZIP dosyasının tamamını çıkarın ve EXE yanındaki PowerPlans klasörünü koruyun. Güç planları için orijinal Control Center gerekmez. Donanım erişimi için OEM sürücüsü gerekir. Windows başlangıcı kurulumu üç plan dosyasını da uygulamayla kopyalar.
