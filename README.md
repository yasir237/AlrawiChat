# 🚀 Alrawi Chat - Yerel Ağ Mesajlaşma Uygulaması

Alrawi Chat, aynı yerel ağ (WiFi) üzerinde bulunan kullanıcıların birbirleriyle anlık olarak mesajlaşmasını sağlayan, C# ve Windows Forms kullanılarak geliştirilmiş bir masaüstü uygulamasıdır. Uygulama, merkezi bir sunucu ve bu sunucuya bağlanan istemciler (clients) mimarisi üzerine kurulmuştur. Temel iletişim altyapısı TCP/IP soketleri ile sağlanmakta, eş zamanlılık ve kullanıcı arayüzü yanıt verebilirliği ise System.Threading kütüphanesi kullanılarak yönetilmektedir.

## ✨ Temel Özellikler

- **Sunucu & İstemci Modları**: Uygulama hem sunucu (server) hem de istemci (client) olarak başlatılabilir.
- **Yerel Ağ Keşfi**: Sunucu, yerel IP adresini göstererek istemcilerin kolayca bağlanmasını sağlar.
- **Anlık Mesajlaşma**: Sunucuya bağlı tüm istemciler arasında gerçek zamanlı metin tabanlı iletişim.
- **Kullanıcı Adı Desteği**: Bağlanan her kullanıcı, sistemdeki makine adıyla veya belirlediği özel bir isimle temsil edilir.
- **Dinamik Kullanıcı Arayüzü**: Bağlantı durumuna ve role (sunucu/istemci) göre değişen, modern ve kullanıcı dostu arayüz.
- **Çoklu İstemci Desteği**: Sunucu, birden fazla istemci bağlantısını eş zamanlı olarak yönetebilir.
- **Bağlantı Yönetimi**: İstemcilerin bağlanma ve ayrılma durumları takip edilir ve tüm kullanıcılara bildirilir.

## 🔧 Teknik Derinlik: Mimari ve Teknolojiler
Bu projenin kalbinde, modern ağ tabanlı uygulamaların temel taşlarını oluşturan iki kritik konsept yatmaktadır: Eş zamanlı operasyonlar için İş Parçacıkları (Threads) ve ağ üzerinde veri iletişimi için Soketler (Sockets). Ayrıca, bu projenin iletişim omurgasını oluşturan TCP protokolünün neden tercih edildiğini anlamak için TCP ve UDP arasındaki temel farklara da değineceğiz.

## 🧵 Eş Zamanlılık Yönetimi: İş Parçacıkları (System.Threading.Thread)
### İş Parçacığı (Thread) Nedir?

Bir işletim sisteminde çalışan her uygulama bir süreç (process) olarak temsil edilir. Her süreç, kendi bellek alanına ve sistem kaynaklarına sahip izole bir çalışma ortamıdır. Bir iş parçacığı ise, bir süreç içerisinde yürütülebilen en küçük bağımsız kod yürütme birimidir. Geleneksel olarak, bir süreç tek bir iş parçacığı ile başlar (ana iş parçacığı - main thread), ancak modern uygulamalar genellikle birden fazla iş parçacığı kullanarak eş zamanlı (concurrent) veya paralel (parallel) işlemler gerçekleştirir. Bir süreç içerisindeki tüm iş parçacıkları, o sürecin bellek alanını ve kaynaklarının çoğunu paylaşır.

### Alrawi Chat'te Thread Kullanımının Motivasyonu ve Uygulanışı:

Alrawi Chat gibi ağ tabanlı ve grafiksel kullanıcı arayüzüne (GUI) sahip uygulamalarda, iş parçacıklarının kullanımı kaçınılmazdır ve temel olarak şu sorunları çözmek için kullanılır:

#### Kullanıcı Arayüzü Yanıt Verebilirliği (UI Responsiveness):

**Problem**: Windows Forms uygulamaları, tüm UI olaylarını (buton tıklamaları, pencere yeniden çizimleri vb.) işleyen tek bir ana UI iş parçacığına sahiptir. Ağ işlemleri (Socket.Accept, NetworkStream.Read gibi) doğası gereği engelleyici (blocking) olabilir; yani, işlem tamamlanana kadar (örneğin, bir veri gelene kadar) bulundukları iş parçacığının çalışmasını durdururlar. Eğer bu engelleyici operasyonlar ana UI thread'inde yapılırsa, uygulama "donar" ve kullanıcı hiçbir işlem yapamaz hale gelir.  
**Çözüm**: Alrawi Chat, bu engelleyici ağ operasyonlarını ayrı iş parçacıklarına devreder:  
- Sunucu: ``listenThread``, ``server.Accept()`` çağrısını yaparak yeni bağlantıları beklerken ana UI thread'ini serbest bırakır.  
- İstemci: ``messageThread``, ``clientStream.Read()`` çağrısıyla sunucudan mesaj beklerken ana UI thread'inin donmasını engeller.  
**Sonuç**: Kullanıcı, ağ işlemleri arka planda devam ederken bile arayüzle etkileşime devam edebilir (mesaj yazabilir, pencereyi taşıyabilir vb.), bu da akıcı bir kullanıcı deneyimi sağlar.

#### Eş Zamanlı İstemci Yönetimi (Concurrent Client Handling - Sunucu Tarafı):

**Problem**: Bir sohbet sunucusunun aynı anda birden fazla istemciye hizmet vermesi beklenir. Tek bir iş parçacığı kullanılsaydı, sunucu bir istemciyle iletişim kurarken diğer istemcilerden gelen istekleri veya mesajları işleyemezdi.  
**Çözüm**: Sunucu, ``AcceptClients`` metodunda her yeni istemci bağlantısı kabul edildiğinde, o istemciye özel bir iş parçacığı (``clientThread``) oluşturur ve bu thread ``HandleClient`` metodunu çalıştırır.  
**Sonuç**: Her istemcinin mesajlaşma döngüsü (mesaj alma, işleme, yayınlama) diğerlerinden bağımsız olarak kendi thread'inde çalışır. Bu, sunucunun çok sayıda istemciye eş zamanlı olarak hizmet vermesini sağlar ve uygulamanın ölçeklenebilirliğini artırır. Bir istemcinin yavaş ağı veya işlemi, diğer istemcilerin deneyimini olumsuz etkilemez.

#### Performans ve Avantajlar:

- **Artan Performans**: Özellikle çok çekirdekli işlemcilerde, iş yükünü birden fazla thread'e dağıtmak, görevlerin paralel olarak yürütülmesine olanak tanıyarak toplam işlem süresini kısaltabilir (gerçi bu uygulamada asıl fayda yanıt verebilirlik ve eş zamanlılıktır).
- **Gelişmiş Yanıt Verebilirlik**: Uygulamanın donmasını engelleyerek kullanıcı deneyimini iyileştirir.
- **Verimli Kaynak Kullanımı (Dikkatli Yönetildiğinde)**: Eş zamanlı operasyonlar, özellikle G/Ç (I/O) beklemeleri sırasında (ağdan veri beklerken olduğu gibi), CPU'nun boşta kalmasını engelleyerek sistem kaynaklarının daha verimli kullanılmasına yardımcı olabilir.

#### Zorluklar ve Dikkat Edilmesi Gerekenler:

- **UI Thread Güvenliği**: Arka plan thread'lerinden UI elemanlarına doğrudan erişim, istisnalara yol açar. Bu nedenle ``Control.InvokeRequired`` ve ``Control.Invoke`` (veya ``BeginInvoke``) mekanizmaları kullanılarak UI güncellemelerinin ana UI thread'ine güvenli bir şekilde sıralanması (marshalling) gerekir. ``AppendToChatHistory`` metodundaki implementasyon bu zorunluluğu ele alır.
- **Kaynak Paylaşımı ve Senkronizasyon**: Birden fazla thread aynı kaynaklara (örneğin, sunucudaki clients listesi) erişiyorsa, yarış durumu (race condition) gibi sorunları önlemek için lock anahtar kelimesi gibi senkronizasyon mekanizmaları kullanılmalıdır. ``HandleClient`` ve ``Broadcast`` metotlarında ``lock(clients)`` ifadesi, clients ve addrs listelerine aynı anda sadece bir thread'in erişmesini garanti ederek veri bütünlüğünü korur.
- **Karmaşıklık**: Çoklu iş parçacıklı programlama, tek iş parçacıklı programlamaya göre daha karmaşıktır ve hata ayıklaması (debugging) daha zor olabilir.

## 🌐 Ağ İletişimi: Soket Programlama (System.Net.Sockets)
### Soket (Socket) Nedir?

Ağ programlamanın temel taşı olan soket, ağ üzerindeki iki uygulama süreci arasında çift yönlü bir iletişim kanalı kurmak için kullanılan bir yazılım arayüzü veya uç noktasıdır (endpoint). Bir ağ bağlantısını benzersiz olarak tanımlamak için genellikle bir IP adresi ve bir port numarasının birleşimiyle temsil edilir (örneğin, 192.168.1.10:5050). İşletim sistemleri, uygulamaların ağ donanımıyla doğrudan uğraşmak yerine, soket API'leri (Uygulama Programlama Arayüzleri) aracılığıyla iletişim kurmasını sağlar. System.Net.Sockets isim alanı, .NET platformunda bu işlevselliği sağlar.

### Alrawi Chat'te Soket Kullanımı:

Alrawi Chat, klasik istemci-sunucu (client-server) mimarisini benimser ve iletişim için TCP soketlerini kullanır:

#### Sunucu Rolü:

- Merkezi bir kontrol noktası olarak hareket eder.
- ``Socket`` sınıfını kullanarak belirli bir IP adresi ve port üzerinde dinleme yapar (``Bind``, ``Listen``).
- Gelen istemci bağlantılarını kabul eder (``Accept``). Her kabul edilen bağlantı için istemciye özel yeni bir soket nesnesi oluşturulur.
- Bağlı tüm istemcilerin listesini tutar (clients listesi) ve bir istemciden gelen mesajı diğerlerine iletmek (broadcast) için bu listeyi kullanır.

#### İstemci Rolü:

- Sunucuya bağlanma isteğini başlatır.
- ``TcpClient`` sınıfı (arka planda ``Socket`` kullanan daha kullanıcı dostu bir sarmalayıcı) ile sunucunun IP adresine ve portuna bağlanır (``Connect``).
- Bağlantı kurulduktan sonra, veri göndermek ve almak için ``NetworkStream`` kullanır (``GetStream``). Mesajlar bu akış üzerinden sunucuya gönderilir ve sunucudan gelen mesajlar bu akış üzerinden okunur.

#### Veri Aktarım Mekanizması:

Mesajların sınırlarını belirlemek için, her mesajdan önce sabit boyutlu (HEADER) bir başlık gönderilir. Bu başlık, takip eden asıl mesajın uzunluğunu belirtir.  
Bu basit protokol, alıcının bir mesajın nerede bitip diğerinin nerede başladığını bilmesini sağlar, özellikle TCP gibi akış tabanlı (stream-based) protokollerde bu önemlidir. Veriler UTF-8 formatında kodlanır.  
Soketler, Alrawi Chat'in farklı cihazlardaki örneklerinin yerel ağ üzerinde birbirleriyle konuşabilmesini sağlayan temel altyapıyı oluşturur.

## 🚦 Transport Katmanı Protokolleri: TCP vs. UDP
Soketler iletişim uç noktalarını sağlarken, verinin bu uç noktalar arasında nasıl taşınacağını belirleyen kurallar transport katmanı protokolleri tarafından tanımlanır. En yaygın iki protokol TCP ve UDP'dir.

### TCP (Transmission Control Protocol - İletim Kontrol Protokolü):

- **Bağlantı Yönelimli (Connection-Oriented)**: Veri aktarımı başlamadan önce gönderici ve alıcı arasında sanal bir bağlantı kurulur (üçlü el sıkışma - three-way handshake). Aktarım bittiğinde bağlantı sonlandırılır.
- **Güvenilir (Reliable)**: Gönderilen verinin alıcıya ulaştığını doğrulamak için onay (acknowledgment - ACK) mekanizması kullanır. Veri paketleri kaybolursa veya bozulursa, TCP bunları otomatik olarak yeniden gönderir.
- **Sıralı (Ordered)**: Veri paketlerini sıralı bir şekilde gönderir ve alıcı tarafta doğru sırada birleştirilmesini garanti eder. Paketler ağda farklı yollardan gidip sırası bozulsa bile, TCP bunları doğru sıraya koyar.
- **Akış Kontrolü (Flow Control)**: Alıcının işleyebileceğinden daha fazla veri gönderilmesini engeller.
- **Tıkanıklık Kontrolü (Congestion Control)**: Ağdaki tıkanıklığı algılayarak veri gönderme hızını ayarlar.
- **Overhead**: Bağlantı yönetimi, onaylar, sıra numaraları vb. nedeniyle UDP'ye göre daha fazla başlık bilgisi (overhead) içerir ve genellikle biraz daha yavaştır.
- **Kullanım Alanları**: Veri bütünlüğü ve sırasının kritik olduğu uygulamalar: Web (HTTP/HTTPS), E-posta (SMTP/POP3/IMAP), Dosya Transferi (FTP), Güvenli Kabuk (SSH) ve Alrawi Chat gibi mesajlaşma uygulamaları. Sohbet uygulamasında mesajların kaybolmaması ve gönderildiği sırada alınması önemlidir.

### UDP (User Datagram Protocol - Kullanıcı Datagram Protokolü):

- **Bağlantısız (Connectionless)**: Veri göndermeden önce bir bağlantı kurmaz. Paketler (datagramlar) doğrudan gönderilir.
- **Güvenilmez (Unreliable)**: Paketlerin alıcıya ulaşıp ulaşmadığını, bozulup bozulmadığını veya doğru sırada gidip gitmediğini kontrol etmez. "En iyi çaba" (best-effort) prensibiyle çalışır. Kayıp paketlerin yeniden gönderimi veya sıralama gibi işlemler uygulama katmanına bırakılır (eğer gerekliyse).
- **Sırasız (Unordered)**: Paketler, ağdaki farklı yollarla gidebileceğinden sırasız gelebilir.
- **Daha Hızlı (Faster)**: TCP'nin bağlantı kurma, doğrulama ve sıralama gibi ek işlemleri olmadığı için, UDP daha hızlıdır ve genellikle daha az başlık bilgisini içerir.
- **Düşük Gecikme (Low Latency)**: Özellikle gerçek zamanlı uygulamalarda, verinin hızlı bir şekilde iletilmesi gereken durumlarda kullanılır.
- **Kullanım Alanları**: Gerçek zamanlı uygulamalar (video konferans, canlı yayın), DNS, VoIP, oyunlar, IPTV.

---

## ✨ Sonuç ve Geliştirme Yönleri

Alrawi Chat, yerel ağda güvenli ve verimli mesajlaşmayı sağlamak için TCP tabanlı bir yaklaşım kullanır. Çift yönlü iletişim için soketler kullanılırken, eş zamanlılık ve çoklu istemci desteği iş parçacıkları aracılığıyla yönetilmektedir. Ayrıca, daha fazla kullanıcı ve yüksek trafikli ağlar için ölçeklenebilirliği arttırmak adına belirli geliştirme yönlerine (örn. hata yönetimi, GUI iyileştirmeleri) odaklanılabilir.

