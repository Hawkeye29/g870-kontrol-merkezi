# Kurulum ve kaldırma

Uygulamayı ilk kez çalıştırmadan önce orijinal sürücünün kurulu olduğunu kontrol edin. Aynı anda OEM arayüzünden donanım komutu göndermeyin. Pencerenin X düğmesi uygulamayı tepsiye gizler; tamamen kapatmak için tepsi simgesine sağ tık → Çıkış seçin.

## Sistem değişiklikleri

Profiller `%LOCALAPPDATA%\G870ControlCenterWindows` altında saklanır. Windows başlangıcı seçeneği açıldığında EXE, bu klasörün `App` alt klasörüne kopyalanır; kullanıcı SID'sini içeren `G870ControlCenter-<SID>` adlı zamanlanmış görev oluşturulur. Görev o kullanıcı oturum açınca en yüksek kullanılabilir yetkiyle uygulamayı başlatır. Seçenek kapatılınca görev silinir. Donanım ayarları yalnızca ilgili Uygula/Mod/MUX eylemleriyle veya kullanıcı açılışta geri yüklemeyi etkinleştirmişse gönderilir.

## Kaldırma

1. Uygulamadaki Windows başlangıcı seçeneğini kapatın; hata olmadığını kontrol edin.
2. Tepsi menüsünden Çıkış yapın; fanların BIOS otomatiğine döndüğünü kontrol edin.
3. Uygulama klasörünü silin. Kişisel profilleri de silmek istiyorsanız `%LOCALAPPDATA%\G870ControlCenterWindows` klasörünü silin.
4. Başlangıç kaydı uygulama üzerinden silinemediyse Windows Görev Zamanlayıcı'da yalnızca bu uygulamaya ait `G870ControlCenter-<kendi kullanıcı SID'niz>` görevini kaldırın.

OEM sürücüsünü veya diğer üretici görevlerini kaldırmayın. Uygulama BIOS/EFI yüklemez. MUX modu kaldırma işlemiyle geri alınmaz; gerekiyorsa önce istenen GPU modunu seçip yeniden başlatın.

Uygulama ZIP dosyasının tamamını çıkarın ve EXE yanındaki PowerPlans klasörünü koruyun. Güç planları için orijinal Control Center gerekmez. Donanım erişimi için OEM sürücüsü gerekir. Windows başlangıcı kurulumu üç plan dosyasını da uygulamayla kopyalar.
