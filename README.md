# 🚀 Alrawi Chat - Yerel Ağ Mesajlaşma Uygulaması

Alrawi Chat, aynı yerel ağ (WiFi) üzerinde bulunan kullanıcıların birbirleriyle anlık olarak mesajlaşmasını sağlayan, C# ve Windows Forms kullanılarak geliştirilmiş bir masaüstü uygulamasıdır. Uygulama, merkezi bir sunucu ve bu sunucuya bağlanan istemciler (clients) mimarisi üzerine kurulmuştur. Temel iletişim altyapısı TCP/IP soketleri ile sağlanmakta, eş zamanlılık ve kullanıcı arayüzü yanıt verebilirliği ise System.Threading kütüphanesi kullanılarak yönetilmektedir.

<div align = center >
  <img src = 'https://github.com/user-attachments/assets/e05c3b70-e825-4eca-8ebd-f707cc9a756e' >
</div>

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

---

# Kod Yapısı

## Sabitler ve Değişkenler

### 🔧 **Constants (Sabitler)**

- **`HEADER`**:  
  Bu sabit, mesajların başlık kısmı için ayrılacak olan bayt sayısını belirtir. Değeri **64** olarak belirlenmiştir, yani her mesajın başında bu kadar baytlık bir alan olacak. Bu alan, mesajın boyutunu (header) içerir.

```csharp
private const int HEADER = 64;
```

- **`FORMAT`**:  
  Bu sabit, mesajların iletilirken kullanılacak **karakter formatı** (encoding) belirtir. Burada **UTF-8** formatı kullanılmıştır. UTF-8, dünya çapında en yaygın kullanılan metin formatlarından biridir ve birçok dildeki karakteri destekler.

```csharp
private const string FORMAT = "utf-8";
```

- **`DISCONNECT_MESSAGE`**:  
  Bu sabit, bir istemcinin bağlantıyı kesmek istediğinde gönderdiği özel bir mesajı temsil eder. **`"!DISCONNECT"`** mesajı, istemcinin sunucuya veya diğer istemcilere "Bağlantımı kesiyorum" demesini sağlar.

```csharp
private const string DISCONNECT_MESSAGE = "!DISCONNECT";
```

---

### 🧰 **Variables (Değişkenler)**

- **`isServer`**:  
  Bu **boolean** (doğru/yanlış) değişken, programın şu an sunucu modunda mı çalıştığını belirler. Eğer değer **true** ise, program sunucu olarak çalışıyordur. Eğer **false** ise, program istemci olarak çalışıyordur.

```csharp
private bool isServer = false;
```

- **`server`**:  
  **`Socket`** türünde bir değişkendir ve sunucu soketini temsil eder. Sunucu, bu soket üzerinden istemcilerle bağlantı kurar ve mesajlaşma işlemlerini yönetir.

```csharp
private Socket server = null;
```

- **`client`**:  
  **`TcpClient`** türünde bir değişkendir ve istemci tarafındaki bağlantıyı temsil eder. Bu, sunucuya bağlanan istemcinin bağlantısını yönetir.

```csharp
private TcpClient client = null;
```

- **`clientStream`**:  
  **`NetworkStream`** türünde bir değişken olup, istemci ile sunucu arasındaki veri iletimini sağlamak için kullanılır. Bu stream üzerinden mesajlar gönderilir ve alınır.

```csharp
private NetworkStream clientStream = null;
```

- **`listenThread`**:  
  **`Thread`** türünde bir değişkendir ve sunucunun istemci bağlantılarını dinlemek için kullanılan iş parçacığını temsil eder. Sunucu yeni istemci bağlantıları kabul etmek için bu iş parçacığını kullanır.

```csharp
private Thread listenThread = null;
```

- **`messageThread`**:  
  **`Thread`** türünde bir değişkendir ve istemciden veya sunucudan gelen mesajları almak ve işlemek için kullanılan iş parçacığını temsil eder.

```csharp
private Thread messageThread = null;
```

- **`username`**:  
  Bu, kullanıcının adını tutan bir **string** değişkendir. Mesajlaşma sırasında, hangi kullanıcıdan mesaj geldiğini belirlemek için kullanılır.

```csharp
private string username;
```

---

### 🖥️ **Server-Specific Variables (Sunucuya Özgü Değişkenler)**

- **`clients`**:  
  Bu, **`Socket`** türünde bir liste olup, sunucuya bağlı olan tüm istemcilerin soketlerini saklar. Her istemci, sunucuyla bağlantı kurduğunda, bu listeye eklenir.

```csharp
private static List<Socket> clients = new List<Socket>();
```

- **`addrs`**:  
  Bu, **`IPEndPoint`** türünde bir liste olup, sunucuya bağlı olan tüm istemcilerin IP adreslerini ve bağlantı noktalarını saklar. Her istemci bağlantısı kurduğunda, bu listeye eklenir.

```csharp
private static List<IPEndPoint> addrs = new List<IPEndPoint>();
```

---
## Network Methods 🌐
Bu bölümdeki metodlar, istemci ve sunucu arasındaki ağ bağlantısını yönetir ve mesajlaşma işlemlerini sağlar. Bu metodlar, ağ üzerinde verilerin doğru şekilde iletilmesi, bağlantı kurulması, istemci kabul edilmesi ve mesajların iletilmesi gibi görevleri yerine getirir.

### **GetLocalIPAddress** 🖥️

Bir istemci ve sunucu arasında iletişim kurmak için önce bilgisayarın yerel IP adresini bilmemiz gerekiyor. Bu fonksiyon, sunucu veya istemci olalım, sistemde hangi IP adresini kullanarak bağlantı kurmamız gerektiğini belirler. Mesela, bir sunucu açıldığında, sistemin hangi IP adresi üzerinden diğer istemcilerle iletişim kuracağı belirlenir. Eğer IP adresi bulunamazsa, güvenli bir şekilde `127.0.0.1` (localhost) döndürülür.

```csharp
private string GetLocalIPAddress()
{
    var host = Dns.GetHostEntry(Dns.GetHostName());  // Bilgisayarın hostname'ini alır.
    foreach (var ip in host.AddressList)  // Bilgisayarın tüm IP adreslerini kontrol eder.
    {
        if (ip.AddressFamily == AddressFamily.InterNetwork)  // IPv4 adresini arar.
        {
            return ip.ToString();  // İlk bulduğu IPv4 adresini döner.
        }
    }
    return "127.0.0.1";  // Eğer IP adresi bulunmazsa, default olarak localhost (127.0.0.1) döner.
}
```

---

### **StartServer** 🖧

Bir sunucu açmak için bu fonksiyon kullanılır. Diyelim ki bir kişi, bir odada sohbet etmek isteyen herkesin katılabileceği bir grup kuruyor. Bu fonksiyon, sunucuyu başlatmak ve diğer istemcilerin bağlantı kurabilmesi için bir "dinleme" başlatır. Sunucu, belirlenen IP ve port üzerinden dinlemeye başlar. Sunucunun başlatıldığına dair de bir mesaj gösterilir.

```csharp
private void StartServer(string ip, int port)
{
    try
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);  // Sunucu için IP ve port adresi oluşturulur.
        server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  // TCP soketi oluşturulur.
        server.Bind(endPoint);  // Soket, belirtilen IP ve port adresine bağlanır.
        server.Listen(5);  // Sunucu, 5 istemciye kadar bağlanmayı bekler.
        
        listenThread = new Thread(AcceptClients);  // İstemcileri kabul etmek için yeni bir thread başlatılır.
        listenThread.IsBackground = true;  // Bu thread arka planda çalışır.
        listenThread.Start();  // Thread başlatılır.

        AppendToChatHistory($"[SYSTEM] Server started on {ip}:{port}");  // Sunucu başlatıldığına dair mesaj eklenir.
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error starting server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);  // Hata durumunda mesaj gösterilir.
    }
}
```

---

### **AcceptClients** 👥

Sunucu, her yeni bağlantı için bir istemci kabul etmek üzere bir "bekleme" modunda çalışır. Birisi bağlandığında, yeni bir istemci kabul edilir ve ona yardımcı olmak için yeni bir thread başlatılır. Bu, her bir istemciyle eşzamanlı çalışmayı sağlar. Yani, birden fazla kullanıcı birbirine mesaj gönderebilirken, her birinin bağımsız olarak işlemesi sağlanır.

```csharp
private void AcceptClients()
{
    try
    {
        while (true)  // Sonsuz döngü ile sürekli istemci kabul edilir.
        {
            Socket clientSocket = server.Accept();  // Bağlanan istemci kabul edilir.
            IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;  // İstemcinin IP adresi ve portu alınır.
            
            lock (clients)  // İstemci listesi üzerinde kilitlenir, böylece eşzamanlı işlemler güvenli olur.
            {
                clients.Add(clientSocket);  // Yeni istemci listeye eklenir.
                addrs.Add(clientEndPoint);  // İstemcinin IP adresi listeye eklenir.
            }

            Thread clientThread = new Thread(() => HandleClient(clientSocket, clientEndPoint));  // Yeni istemci için thread başlatılır.
            clientThread.IsBackground = true;  // Arka planda çalışacak thread.
            clientThread.Start();  // Thread çalıştırılır.

            AppendToChatHistory($"[SYSTEM] New client connected: {clientEndPoint}");  // Yeni istemcinin bağlandığına dair mesaj eklenir.
        }
    }
    catch (Exception ex)
    {
        if (!(ex is ObjectDisposedException))  // Hata kontrolü, sunucu kapatıldığında ObjectDisposedException hata verir.
        {
            AppendToChatHistory($"[ERROR] Server error: {ex.Message}");  // Hata mesajı gösterilir.
        }
    }
}
```

---

### **HandleClient** 💬

Bir istemci sunucuya bağlandığında, her istemcinin kendi mesajları ve verileri vardır. Bu fonksiyon, bir istemcinin mesajlarını alır, işler ve geri gönderir. Örneğin, biri başka birine mesaj yazarsa, bu fonksiyon o mesajı alır ve tüm diğer istemcilere iletir. Eğer bir istemci "DISCONNECT_MESSAGE" gönderirse, bağlantıyı kapatır ve sohbeti sonlandırır.

```csharp
private void HandleClient(Socket clientSocket, IPEndPoint clientEndPoint)
{
    byte[] buffer = new byte[HEADER];  // Mesajları almak için bir buffer (önbellek) tanımlanır.

    try
    {
        while (true)  // Sonsuz döngü ile istemciden sürekli veri alınır.
        {
            int msgLength = clientSocket.Receive(buffer, 0, HEADER, SocketFlags.None);  // Verinin başlığı alınır.
            if (msgLength > 0)
            {
                byte[] msgBuffer = new byte[msgLength];  // Gelen mesajın boyutu kadar bir array oluşturulur.
                clientSocket.Receive(msgBuffer, 0, msgLength, SocketFlags.None);  // Mesaj alınır.
                string msg = Encoding.UTF8.GetString(msgBuffer);  // Mesaj UTF-8 formatında string'e dönüştürülür.

                if (msg == DISCONNECT_MESSAGE)  // Bağlantı kesme mesajı kontrol edilir.
                    break;  // Eğer "DISCONNECT" mesajı alınırsa, bağlantı sonlandırılır.

                AppendToChatHistory($"{msg}");  // Mesaj, sohbet geçmişine eklenir.
                Broadcast(clientEndPoint, msg);  // Mesaj, diğer istemcilere iletilir.
            }
        }
    }
    catch
    {
        // Bağlantı kesildiğinde veya bir hata oluştuğunda hiçbir işlem yapılmaz.
    }
    finally
    {
        lock (clients)  // İstemci listesi üzerinde kilitlenir.
        {
            int index = clients.IndexOf(clientSocket);  // Bağlantı kesilen istemci listeden çıkarılır.
            if (index >= 0)
            {
                clients.RemoveAt(index);  // İstemci listeden silinir.
                addrs.RemoveAt(index);  // İstemcinin adresi listeden silinir.
            }
        }

        clientSocket.Close();  // İstemci bağlantısı kapatılır.
        AppendToChatHistory($"[SYSTEM] Client disconnected: {clientEndPoint}");  // Bağlantı kesildiğine dair mesaj eklenir.
    }
}
```

---

### **Broadcast** 📡
Sunucu, bir istemciden gelen mesajı alır ve tüm diğer istemcilere iletmek ister. Buradaki amaç, mesajın tüm katılımcılara ulaşmasını sağlamaktır. Ancak, mesajı gönderen istemciye geri göndermemek için onun IP adresi kontrol edilir. 

```csharp
private void Broadcast(IPEndPoint senderEndPoint, string message)
{
    lock (clients)  // İstemci listesi üzerinde kilitlenir.
    {
        for (int i = 0; i < clients.Count; i++)  // Tüm istemciler için döngü başlatılır.
        {
            if (addrs[i].Equals(senderEndPoint)) continue;  // Mesajı gönderen istemciye mesaj gönderilmez.
            
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);  // Mesaj UTF-8 formatında byte dizisine dönüştürülür.
                clients[i].Send(data);  // Mesaj istemciye gönderilir.
            }
            catch
            {
                // Hata durumunda işlem yapılmaz, istemci zaten thread ile temizlenir.
            }
        }
    }
}
```

---

### **ConnectToServer** 🔌
Bir istemci sunucuya bağlanmak istediğinde, bu fonksiyon kullanılır. Sunucuya başarılı bir şekilde bağlanırsa, istemci sürekli mesaj alabilmek için arka planda bir thread çalıştırılır. Bağlantı hatalıysa, kullanıcıya hata mesajı gösterilir.

```csharp
private bool ConnectToServer(string serverIP, int port)
{
    try
    {
        client = new TcpClient();  // Yeni bir TCP istemcisi oluşturulur.
        client.Connect(serverIP, port);  // Sunucuya bağlanılır.
        clientStream = client.GetStream();  // Bağlantı üzerinden veri akışı başlatılır.

        messageThread = new Thread(ReceiveMessages);  // Mesajları almak için yeni bir thread başlatılır.
        messageThread.IsBackground = true;  // Bu thread arka planda çalışır.
        messageThread.Start();  // Thread baş

latılır.
        
        return true;  // Bağlantı başarılı.
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);  // Hata mesajı gösterilir.
        return false;  // Bağlantı hatalı.
    }
}
```

---
## Helper Methods 🛠️ 

### **AppendToChatHistory** 💬

Bir sohbet uygulamasında, her gelen mesajın doğru bir şekilde kullanıcıya gösterilmesi önemlidir. Bu fonksiyon, mesajları sohbet geçmişine ekler ve her mesajı uygun renk, stil ve biçimde kullanıcıya sunar. Örneğin, bir sistem mesajı farklı bir renk ve kalın yazı tipinde gösterilirken, bir hata mesajı kırmızı renkte ve yine kalın olabilir. Ayrıca, her mesajdan önce kullanıcıya "►" sembolü eklenir. Bu sayede kullanıcı, mesajın kimden geldiğini kolayca anlayabilir.

```csharp
private void AppendToChatHistory(string message)
{
    if (this.InvokeRequired)  // Eğer çağrı arka planda yapılmışsa, UI thread'inde çalıştırmak için Invoke kullanılır.
    {
        try
        {
            this.Invoke(new Action<string>(AppendToChatHistory), message);  // UI thread'inde mesajı ekler.
            return;
        }
        catch (Exception) { return; }  // Hata durumunda bir şey yapılmaz.
    }

    Panel currentPanel = this.Tag as Panel;  // Mevcut paneli alır.
    if (currentPanel != null)
    {
        RichTextBox chatBox = currentPanel.Tag as RichTextBox;  // Panelin içinde bulunan RichTextBox'ı alır.
        if (chatBox != null)
        {
            // Mesaj türüne göre renk ve stil belirlenir.
            Color messageColor;
            bool isBold = false;

            if (message.StartsWith("[SYSTEM]"))
            {
                messageColor = Color.DarkGray;  // Sistem mesajı için gri renk
                isBold = true;  // Mesaj kalın yazı stilinde olacak
            }
            else if (message.StartsWith("[SERVER]"))
            {
                messageColor = Color.FromArgb(0, 120, 215);  // Sunucu mesajı için mavi renk
                isBold = true;
            }
            else if (message.StartsWith("[ERROR]"))
            {
                messageColor = Color.Red;  // Hata mesajı için kırmızı renk
                isBold = true;
            }
            else if (message.StartsWith("You:"))
            {
                messageColor = Color.FromArgb(0, 150, 136);  // Kullanıcı mesajı için yeşil tonları
                message = "► " + message;
            }
            else
            {
                messageColor = Color.FromArgb(50, 50, 50);  // Diğer mesajlar için gri
                message = "► " + message;
            }

            // Mesajın yazıldığı yerin başlangıç pozisyonu alınır.
            int startPos = chatBox.TextLength;

            // Mesaj eklenir.
            chatBox.AppendText(message + Environment.NewLine);

            // Eklenen mesaj seçilir ve rengi ayarlanır.
            chatBox.Select(startPos, message.Length);
            chatBox.SelectionColor = messageColor;

            // Eğer mesaj kalın (bold) yazılacaksa, yazı tipini kalın yapar.
            if (isBold)
                chatBox.SelectionFont = new Font(chatBox.Font, FontStyle.Bold);

            // Sistem mesajları için bir ayırıcı çizgi eklenir.
            if (message.StartsWith("[SYSTEM]") || message.StartsWith("[ERROR]"))
            {
                int linePos = chatBox.TextLength;
                chatBox.AppendText("─────────────────────────────────" + Environment.NewLine);
                chatBox.Select(linePos, 35);
                chatBox.SelectionColor = Color.LightGray;
            }

            // Seçim sıfırlanır.
            chatBox.SelectionStart = chatBox.TextLength;
            chatBox.SelectionLength = 0;

            // Sohbet kutusu son mesajı gösterecek şekilde kaydırılır.
            chatBox.ScrollToCaret();
        }
    }
}
```

**Açıklama**:
- **Mesaj Tipi ve Renk Seçimi**: Mesajın türüne göre farklı renkler ve stiller seçilir. Bu sayede kullanıcı, gelen mesajın türünü (sistem mesajı, hata, vs.) kolayca ayırt edebilir.
- **RichTextBox Kullanımı**: `RichTextBox` kullanılarak yazı stilini değiştirme, renkleri ayarlama gibi işlemler yapılır. Bu, görsel olarak kullanıcı deneyimini geliştirir.
- **Karmaşık UI İşlemleri**: `InvokeRequired` ve `Invoke` kullanımı, çoklu thread'ler arasında UI güncellemeleri yapmayı sağlar. Bu, özellikle kullanıcı arayüzü ile arka planda çalışan iş parçacıkları arasındaki uyumu sağlar.

---

### **CleanupConnections** 🧹

Bir istemci veya sunucu bağlantısını sonlandırmak ve temizlemek gerekir. Diyelim ki bir sohbet odasındaki kullanıcı, sohbeti sonlandırmaya karar verdi. Bu fonksiyon, istemci ve sunucu bağlantılarını düzgün bir şekilde kapatır, kullanılmayan kaynakları temizler ve tüm bağlantıları sonlandırır. Ayrıca, açık olan thread’ler (iş parçacıkları) güvenli bir şekilde durdurulur.

```csharp
private void CleanupConnections()
{
    // Sunucu bağlantısını temizler.
    if (server != null)
    {
        try
        {
            server.Close();  // Sunucu kapatılır.
        }
        catch { }
        server = null;  // Sunucu nesnesi null yapılır.
    }

    // İstemci bağlantısını temizler.
    if (client != null)
    {
        try
        {
            if (client.Connected)
            {
                if (clientStream != null)
                {
                    // Bağlantıyı sonlandırmak için disconnect mesajı gönderilir.
                    string fullMessage = $"{username}: {DISCONNECT_MESSAGE}\n";
                    byte[] messageBytes = Encoding.UTF8.GetBytes(fullMessage);
                    string header = messageBytes.Length.ToString();
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header.PadRight(HEADER));
                    clientStream.Write(headerBytes, 0, headerBytes.Length);
                    clientStream.Write(messageBytes, 0, messageBytes.Length);

                    clientStream.Close();  // Stream kapatılır.
                }
                client.Close();  // İstemci bağlantısı kapatılır.
            }
        }
        catch { }
        client = null;  // İstemci nesnesi null yapılır.
        clientStream = null;  // Client stream nesnesi null yapılır.
    }

    // Thread’ler durdurulur.
    if (listenThread != null && listenThread.IsAlive)
    {
        try { listenThread.Abort(); } catch { }
        listenThread = null;
    }

    if (messageThread != null && messageThread.IsAlive)
    {
        try { messageThread.Abort(); } catch { }
        messageThread = null;
    }

    // İstemci listeleri temizlenir.
    lock (clients)
    {
        clients.Clear();  // İstemciler listesi temizlenir.
        addrs.Clear();  // Adresler listesi temizlenir.
    }
}
```

**Açıklama**:
- **Bağlantı Temizliği**: Sunucu ve istemci bağlantılarının doğru şekilde kapatılması sağlanır. Bağlantılar kapatılırken, her iki taraf da düzgün bir şekilde sonlandırılır.
- **Thread Yönetimi**: Thread’lerin güvenli bir şekilde sonlandırılması için `Abort()` kullanılır. Bu, çalışan iş parçacıklarının düzgün bir şekilde sonlanmasını sağlar.
- **Kaynak Temizliği**: `client`, `clientStream`, `server` gibi kaynakların null yapılması, bellek sızıntılarını engeller ve uygulamanın verimli çalışmasını sağlar.

---

### **MainForm_FormClosing** 🏁

Bir kullanıcı programı kapatmaya çalıştığında, bu fonksiyon çalışır. Programın düzgün bir şekilde kapanması için önce bağlantılar temizlenir. Bu, kullanıcıdan gelen son işlemden önce kaynakların doğru bir şekilde temizlenmesini sağlar.

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    CleanupConnections();  // Bağlantılar temizlenir ve sonlandırılır.
}
```

**Açıklama**:
- **Form Kapanışı**: Form kapanmadan önce tüm kaynakların düzgün bir şekilde temizlenmesi sağlanır. Bu, programın düzgün kapanmasını sağlar ve gelecekteki hataları engeller.

---




