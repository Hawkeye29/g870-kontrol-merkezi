<div align="center">

> **Test kapsamı:** Bu uygulama yalnızca geliştiricinin kullandığı tek bir **Casper Excalibur G870 / 1362-BV70X-C** İ7-13620H RTX 5070 bilgisayarda test edilmiştir. Diğer G870 donanım yapılandırmaları, BIOS/firmware sürümleri ve farklı modellerde uyumluluk doğrulanmamıştır. Aynı model adı, aynı donanım ve davranış anlamına gelmeyebilir.
  <img src="src/Assets/G870.svg" width="104" alt="G870 Kontrol Merkezi logosu">
  <h1>G870 Kontrol Merkezi</h1>
  <p>Excalibur G870 için fan, performans, klavye aydınlatması ve GPU modu kontrolü.</p>
  <p><b>Windows x64</b> · <b>.NET Framework 4.8</b> · <b>Sürüm 1.0.0</b></p>
  <p><a href="../../releases/latest">Uygulamayı indir</a> · <a href="#kurulum">Kurulum</a> · <a href="#özellikler">Özellikler</a> · <a href="../../issues">Sorun bildir</a></p>
</div>

## İndir

**Uygulamayı kullanmak için kod derlemen gerekmiyor.** [Releases](../../releases/latest) sayfasından **G870-Kontrol-Merkezi-1.0.0-windows-x64.zip** dosyasını indir.

GitHub'ın otomatik eklediği **Source code (zip)** ve **Source code (tar.gz)** dosyaları geliştirme içindir; çalışan uygulama paketi değildir.

## Kurulum

1. Uygulama ZIP dosyasının tamamını kalıcı bir klasöre çıkar.
2. `PowerPlans` klasörünü EXE'nin yanında tut.
3. `G870ControlCenter.exe` dosyasını çalıştır ve yönetici iznini onayla.

```text
G870ControlCenter.exe
PowerPlans/
  HighPerformance.pow
  Gaming.pow
  office.pow
BASLA.txt
```

**Gereksinimler:** Windows x64, .NET Framework 4.8 ve RW_GMWMI arayüzünü sunan desteklenen G870 firmware desteği. Yerleşik Windows WmiAcpi sürücüsü kullanılır; Casper Control Center sürücüsü gerekmez. Güç planları pakete dahildir; bunlar için orijinal Control Center uygulamasının kurulu olması gerekmez.

Bu uygulama üreticinin resmî yazılımı değildir. Geliştirme cihazı **G870 / 1362-BV70X-C**; diğer modeller ve firmware sürümleri doğrulanmadı.

## Özellikler

| Bölüm | Yapabildiklerin |
| --- | --- |
| İzleme | CPU/GPU sıcaklığı, fan RPM ve sıcaklık geçmişi |
| Performans | Ofis, Oyun ve Yüksek performans; ilgili Windows güç planını uygulama |
| Fan | BIOS otomatik kontrolü, tam hız ve sekiz noktalı özel eğri |
| Klavye RGB | Genel veya üç bölgeli renk, parlaklık ve efekt seçimi |
| MUX | Desteklenen firmware'de iGPU, Hibrit ve NVIDIA modları |
| Sistem tepsisi | Canlı veriler, hızlı profil seçimi ve uygulamayı açma |
| Başlangıç | Son ayarları yükleme ve Windows ile açılmayı ayrı ayrı seçme |

## Günlük kullanım

- **X düğmesi** uygulamayı tepsiye gizler. Tamamen kapatmak için tepsi panelinde **Çıkış** seç. Çıkışta fan kontrolü BIOS'a döndürülür.
- **Son ayarlarla başlat**, kayıtlı profil ve fan/RGB ayarlarını uygulama açılınca yükler.
- **Windows açılışında uygulamayı başlat**, uygulamayı oturum açıldığında çalıştırır. İki seçenek birbirinden bağımsızdır.
- Uygulamayı tekrar açmak ikinci bir pencere oluşturmaz; açık olan pencereye döner.
- MUX değişikliğinde yeniden başlatma için **Evet/Hayır** onayı istenir. Destek, ekran bağlantıları ve BitLocker durumu değişikliği engelleyebilir.

RGB kutuları uygulamadan son kaydedilen rengi gösterir. Fn tuşları veya başka yazılımlarla yapılan renk değişiklikleri klavyeden geri okunmaz. GPU sensörü mevcut değilse değer yerine çizgi gösterilir.

### Güncelleme

Tepsi panelinden **Çıkış** yap, yeni ZIP'i bir klasöre çıkar ve yeni EXE'yi aç. Windows başlangıcı etkinse uygulama kendi başlangıç kopyasını günceller. Ayarlar `%LOCALAPPDATA%\G870ControlCenterWindows` altında tutulur.

### Windows güvenlik uyarısı

Bu sürüm güvenilir yayıncı sertifikasıyla imzalanmış değildir. Windows Akıllı Uygulama Denetimi çıktıyı engelleyebilir. Visual Studio ile derlemek yayıncı imzası sağlamaz.

## Geliştirme

Visual Studio'da **.NET masaüstü geliştirme** iş yükünü ve **.NET Framework 4.8 hedefleme paketini** kur. `G870KontrolMerkezi.sln` dosyasını aç; başlangıç projesini `G870KontrolMerkezi`, yapılandırmayı **Release / x64** seç.

Developer PowerShell ile:

```powershell
msbuild G870KontrolMerkezi.sln /m /p:Configuration=Release /p:Platform=x64
```

Çıktı `src/bin/Release/` klasöründedir; `PowerPlans` alt klasörü de derlemede kopyalanır.

```powershell
# Donanıma komut göndermeden arayüzü dene
.\src\bin\Release\G870ControlCenter.exe --demo

# Donanımsız testleri çalıştır
.\tests\bin\Release\G870Tests.exe
```

45 donanımsız kontrol yerel olarak geçti. GitHub Actions çözümü derler, testleri çalıştırır ve imzasız çıktıyı artifact olarak saklar. Bu kontroller fiziksel donanım testlerinin yerine geçmez.

## Yardım ve geri bildirim

[Issues](../../issues) üzerinden cihaz modelini, Windows ve uygulama sürümünü, sorunu tekrar etme adımlarını yazabilirsin. Ekran görüntülerinden kişisel bilgilerini kaldır.

[Kurulum ve kaldırma](docs/INSTALL.md) · [Donanım kapsamı](docs/HARDWARE.md) · [Gizlilik](docs/PRIVACY.md)

## Katkılar

Bu proje, OpenAI Codex desteğinden yararlanılarak geliştirilmiştir.