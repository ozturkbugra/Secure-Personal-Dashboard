# BugraLife — Secure Personal Dashboard

> Bu dosya projenin tam haritasıdır. Her değişiklikten sonra ilgili bölümü güncel tut ki
> bir sonraki oturumda kod tekrar taranmadan bağlam buradan okunabilsin.

## 1. Genel Bakış

Tek kullanıcılık (kişisel) bir **ASP.NET Core 8.0 MVC** uygulaması. Kişisel finans
(gelir/gider/hesap/kredi kartı/portföy/cari), planlama (yapılacaklar, alışkanlıklar,
günlük), araçlar (dosya merkezi, dosya transfer, harita, şifre kasası) ve raporlamayı
tek panelde toplar. Arayüz **Türkçe**, kültür `tr-TR` olarak sabitlenmiştir (para: `1.000,50`).

- **Solution:** `BugraLife.sln` (kök dizinde)
- **Proje:** `BugraLife/BugraLife.csproj`
- **Framework:** net8.0, `Nullable` + `ImplicitUsings` açık
- **UI:** Bootstrap 5.3 + Bootstrap Icons + Poppins font, jQuery. Server-render Razor + sayfa
  yenilemeden çalışan **AJAX + JSON** işlemleri (Create/Edit/Delete çoğunlukla `Json(...)` döner).

### Çalıştırma
```bash
dotnet run --project BugraLife
```
Migration:
```bash
dotnet ef migrations add <ad> --project BugraLife
dotnet ef database update --project BugraLife
```
Not: `Program.cs` içinde `context.Database.EnsureCreated()` çağrılır. `EnsureCreated` ile
migration'lar birlikte kullanıldığında çakışabilir; şema değişikliğinde bunu akılda tut.

## 2. Bağımlılıklar (NuGet)
- `Microsoft.EntityFrameworkCore` + `.SqlServer` + `.Tools` (8.0.22) — ORM, SQL Server
- `GoogleAuthenticator` (3.2.0) — TOTP tabanlı 2FA (Google Authenticator)
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` — scaffolding

## 3. Konfigürasyon
- **DB bağlantısı:** `appsettings.json` → `ConnectionStrings:DefaultConnection`
  (SQL Server, `Database=BugraLifeDB`). ⚠️ Bağlantı dizesinde düz parola var; production'da secret'a taşınmalı.
- **Kimlik doğrulama:** Cookie auth. Cookie adı `BugraLife_Auth`, ömür **365 gün**,
  sliding expiration, `HttpOnly`, `SameSite=Lax`, `LoginPath=/Login/Index`.
- **DataProtection:** anahtarlar dosya sisteminde `BugraLife/Keys/` altında saklanır,
  app adı `BugraLifeApp` (cookie'lerin restart sonrası geçerli kalması için).
- **Lokalizasyon:** tek kültür `tr-TR` (hem tarih hem para formatı için kritik).

## 4. Mimari & İskelet
Klasik MVC katmanları:
- `Models/` — EF entity'leri + rapor/dashboard için **ViewModel**'ler (aynı klasörde).
- `DBContext/BugraLifeDBContext.cs` — tüm `DbSet`'ler burada.
- `Controllers/` — 27 controller, her modül için bir tane. Hepsi `[Authorize]`
  (istisna: `TransferController` public dosya paylaşımı için kısmen `[AllowAnonymous]`).
- `Views/<Controller>/Index.cshtml` — modül ekranları. Ortak `_Layout.cshtml` sol menü içerir.
- `Migrations/` — EF Core migration geçmişi.
- `wwwroot/` — statik dosyalar; `wwwroot/transfer/` public transfer klasörü, kullanıcı yüklemeleri.

### Program.cs seed mantığı (ilk açılışta)
`EnsureCreated` sonrası şu **varsayılan kayıtlar** oluşturulur (yoksa):
- Admin kullanıcı: `bugra` / parola `123456` (SHA256+Base64), 2FA kapalı.
- `is_bank == true` işaretli birer sistem kaydı: IncomeType, ExpenseType, PaymentType, Person
  — adı hep "BANKA HAREKETİ". Bu kayıtlar **transfer/virman/kredi kartı ödemesi** gibi
  otomatik oluşturulan iç hareketlerin sahibidir ve normal listelerde `is_bank == false`
  filtresiyle gizlenir.

## 5. Veritabanı Yapısı

### Adlandırma kuralı
Kolonlar `tabloadı_alan` snake_case (ör. `expense_amount`), PK `<tablo>_id`.
FK'ler `[ForeignKey]` attribute + `virtual` navigation ile tanımlı.

### Finansal çekirdek tablolar
| Entity | Tablo amacı | Önemli alanlar |
|---|---|---|
| `PaymentType` | Hesaplar (Kasa/Banka/Kredi Kartı) | `paymenttype_balance` (cache'lenen bakiye), `is_bank`, `is_creditcard`, `paymenttype_order` |
| `Income` | Gelir hareketi | `incometype_id`, `paymenttype_id`, `person_id`, `income_amount`, `income_date`, `is_bankmovement` |
| `Expense` | Gider hareketi | `expensetype_id`, `paymenttype_id`, `person_id`, `expense_amount`, `expense_date`, `is_bankmovement` |
| `IncomeType` | Gelir kategorisi | `incometype_order`, `is_bank` |
| `ExpenseType` | Gider kategorisi | `expensetype_order` (string!), `is_bank`, `is_home`, `is_commission`, `description` |
| `PaymentType` | (yukarıda) | |
| `Person` | Gelir/gider işlemini yapan kişi | `person_order`, `is_bank` |
| `FixedExpense` | Sabit/periyodik gider tanımı | `expensetype_id`, `payment_day` (ayın günü 1-31), `frequency_count`, `is_active` |

### Portföy & Cari
| Entity | Amaç | Notlar |
|---|---|---|
| `Ingredient` | Varlık türü (altın, dolar…) | portföyün "birim"i |
| `Asset` | Portföy hareketi | `ingredient_id`, `person_id`, `asset_amount` (miktar), `asset_date` |
| `Debtor` | Borçlu/alacaklı cari | |
| `Movement` | Cari hareketi | `debtor_id`, `ingredient_id`, `person_id`, `movement_amount` (± işaretli) |

### Yaşam / planlama
| Entity | Amaç |
|---|---|
| `PlannedToDo` | Tarihli yapılacaklar (`plannedtodo_done`) |
| `UnPlannedToDo` | Tarihsiz/anlık işler |
| `ActivityDefinition` | Alışkanlık tanımı (`color`, `is_active`) |
| `ActivityLog` | Alışkanlığın yapıldığı gün (`log_date`) |
| `Daily` | Günlük not + ruh hali `DailyStatus` enum (1 Kötü … 4 Süper) |
| `Location` | Kayıtlı konum (ad, adres, link) |
| `PracticalNote` | Pratik not/kod/çözüm arşivi |

### Araçlar / güvenlik
| Entity | Amaç |
|---|---|
| `LoginUser` | Kullanıcı; parola SHA256+Base64, `IsTwoFactorEnabled`, `TwoFactorSecretKey` |
| `WebSite` | Şifre kasasındaki site tanımı |
| `WebSitePassword` | Site kullanıcı adı/parola (⚠️ **düz metin** saklanıyor) |
| `FileShared` | Public dosya paylaşım linki (`Token`, `FilePath`, `IsActive`) |

## 6. Kritik İş Kuralları / Algoritmalar

### 6.1 Hesap bakiyesi — iki ayrı doğruluk kaynağı (DİKKAT)
Bakiye iki farklı yerde tutuluyor ve tutarsızlık riski var:
1. **`PaymentType.paymenttype_balance`** — her gelir/gider ekle/düzenle/sil işleminde
   controller içinde elle `+=` / `-=` yapılır (cache alan).
2. **Hesaplanan bakiye** — `HomeController.Index` ve raporlarda bu alan **kullanılmaz**;
   bakiye anlık olarak `SUM(Income) - SUM(Expense)` (o hesap için) ile bulunur.
   Dashboard yalnızca `income_date/expense_date <= bugün` olan hareketleri sayar (gelecek tarihli hariç).

`PaymentType/Edit` bu ikisini **eşitler**: hedef bakiye ile gerçek (SUM) bakiye arasındaki
farkı, `is_bank` sistem kayıtları adına bir "Bakiye Düzeltme Fişi" (Income veya Expense) yazarak kapatır.
Yeni hesap açılışında bakiye ≠ 0 ise aynı şekilde "Hesap Açılış Bakiyesi" fişi üretilir.

### 6.2 Kredi kartı
Kredi kartı da bir `PaymentType`'tır (`is_creditcard=true`). Kartla yapılan harcama giderdir
(bakiye eksiye gider = borç). **CreditCardPayment** işlemi tek transaction'da: kaynak hesaptan
Expense (para çıkışı) + kredi kartına Income (borç azalışı) yazar; ikisi de `is_bankmovement=true`.

### 6.3 Para transferi / virman (`MoneyTransfer`)
Tek DB transaction'da: kaynaktan Expense, hedefe Income (ana tutar, `is_bankmovement=true`),
komisyon varsa kaynaktan ayrı bir Expense (`is_commission` gider türü, `is_bankmovement=false`
— yani normal gider raporlarında görünür). Kaynak = hedef olamaz.

### 6.4 `is_bankmovement` bayrağının anlamı
`true` = sistemin ürettiği iç hesap-hareketi (virman, kk ödeme, açılış/düzeltme fişi).
Bu kayıtlar `Income`/`Expense` liste ekranlarında `Where(is_bankmovement == false)` ile **gizlenir**,
ama bakiye SUM'larına dahildir. Yeni finansal işlem eklerken bu bayrağı doğru set etmek şart.

### 6.5 Taksitli gider (`Expense/Create`)
`is_installment=true` ise `InstallmentAmounts/Dates/Descriptions` listeleri döngüyle
her taksit için ayrı `Expense` kaydına açılır; her biri hesabın `paymenttype_balance`'ını düşer.

### 6.6 Sabit gider durumu (dashboard)
Her aktif `FixedExpense` için: bu ayın son ödeme günü `payment_day` (ay gün sayısını aşarsa
ayın son gününe çekilir), o ay o `expensetype_id` ile herhangi bir Expense var mı → `IsPaid`.
`DaysDiff = dueDate - bugün` (pozitif: kaldı, negatif: gecikti). Liste önce ödenmemişler, sonra tarihe göre sıralı.

### 6.7 Cari & portföy bakiyeleri
Cari: `Movement` kayıtları `debtor + ingredient` bazında `SUM(movement_amount)`; ± işaret borç/alacak.
**İşaret kuralı:** `movement_amount > 0` = **Alacak** (karşı taraf bize borçlu, alacağımız artar),
`< 0` = **Borç** (biz borçluyuz). `Movement/Index` formunda kullanıcı işareti elle yazmaz;
"Alacak (+) / Borç (−)" seçici (`.dir-radio`) vardır ve işaret JS ile gönderim anında konur,
düzenlemede mevcut işaretten yön otomatik seçilir. Tutar her zaman artı girilir.
Portföy: `Asset` kayıtları `ingredient` bazında `SUM(asset_amount)`.

### 6.8 Para/tarih parse'ı
Tutarlar formdan **string** alınıp `decimal.Parse(x, CultureInfo("tr-TR"))` ile çevrilir
(`1.000,50` → 1000.50). Yeni finansal aksiyon eklerken bu deseni koru; `ModelState.Remove(...)`
ile navigation property doğrulamaları devre dışı bırakılır (aksi halde 500).

## 6.9 Performans notları (dashboard)
`HomeController.Index` optimize edildi: sabit gider durumu artık **N+1 sorgu yerine** tek
`Distinct()` sorgusuyla "bu ay ödenmiş gider türü ID'leri" bir `HashSet`'e çekilir ve bellekte
kontrol edilir. Tarih filtreleri `.Month/.Year` ve `.Date` yerine **sargable aralık** biçimine
çevrildi (`>= ayBaşı && < gelecekAyBaşı`, `< yarınBaşı`) → index kullanılabilir. Salt-okunur
sorgulara `AsNoTracking()` eklendi. Yeni dashboard sorgusu eklerken bu desenlere uy; döngü
içinde `await ...Async()` çağırma.

## 7. Güvenlik
- **Kimlik:** tek `LoginUser`; parola **SHA256 (salt yok)** → zayıf, ama kişisel tek-kullanıcı bağlamı.
- **2FA:** `GoogleAuthenticator` TOTP. Girişte parola doğruysa 2FA açıksa `Verify2FA`'ya
  `TempData` ile geçilir. Ayrıca **Şifre Kasası** (`PasswordsController`) 2FA açıkken her erişimde
  ayrı doğrulama ister; yetki `TempData["CanAccessPasswords"]` ile taşınır (`TempData.Keep` ile korunur,
  başka sayfaya gidip dönünce düşer).
- ⚠️ **Şifre kasasındaki parolalar düz metin** saklanıyor (`WebSitePassword.websitepassword_password`).
- **Rate-limit / kademeli kilit:** `Services/LoginAttemptTracker.cs` (singleton, IP bazlı, bellekte).
  Kural: 5. hatalı denemede **2 dk** kilit; sonra her **3 hatada** bir kademe artar (8→10 dk,
  11→20 dk, 14→30 dk...). Başarılı girişte sayaç sıfırlanır. Hem parola (`Login/Index`) hem
  2FA kod adımı (`Login/Verify2FA`) aynı sayaçla korunur. `Program.cs`'te DI'a kayıtlı.
  ⚠️ Bellekte tutulduğu için app restart'ta sıfırlanır ve proxy arkasında `X-Forwarded-For`
  okunmadığı sürece gerçek IP yerine proxy IP'si görülebilir.
- **Public uçlar:** `TransferController` (dosya listeleme/indirme/paylaşım) `[AllowAnonymous]`.
  Yüklemeler `wwwroot/transfer/` altında; misafirler erişebilir.
- **Path traversal koruması:** `FileManagerController` ve `TransferController` dosya yollarını
  `IsInsideRoot()` (kökü normalize eder + ayraç ekler) ve `IsSafeLeafName()` (`..` / yol ayracı reddi)
  yardımcılarıyla doğrular. Daha önce zayıf `filePath.StartsWith(rootPath)` kontrolü vardı;
  özellikle **anonim `Transfer/Download`** `../` ile disk genelinde dosya okumaya açıktı — kapatıldı.
  Yeni dosya işleme aksiyonu eklerken bu iki yardımcıyı MUTLAKA kullan.
- **Antiforgery tutarsızlığı (açık iş):** Çoğu POST `[ValidateAntiForgeryToken]` taşır ama
  `FileManager`, `Activity`, `PracticalNote`, `Settings`, `Transfer` controller'larında yok.
  `SameSite=Lax` cross-site POST'u büyük ölçüde engellediği için risk sınırlı; yine de eklenmeli
  (özellikle parola değiştiren `Settings`).

## 8. Modül → Controller haritası (sol menü)
- **Finansal:** Gelirler `Income` · Giderler `Expense` · Kredi Kartı Ödeme `CreditCardPayment` ·
  Para Transferi(Virman) `MoneyTransfer` · Portföy `Asset` · Cari `Movement` · Pratik Notlar `PracticalNote`
- **Planlama & Yaşam:** Planlı İşler `PlannedToDo` · Planlanmamış `UnPlannedToDo` ·
  Alışkanlıklar `Activity` · Günlük `Daily`
- **Araçlar & Raporlar:** Raporlar `Report` · Dosya Merkezi `FileManager` ·
  Transfer Merkezi `Transfer` · Harita `Location/Maps` · Şifrelerim `Passwords`
- **Tanımlamalar:** Hesap `PaymentType` · Kişi `Person` · Borçlu/Alacaklı `Debtor` ·
  Gelir Kat. `IncomeType` · Gider Kat. `ExpenseType` · Sabit Giderler `FixedExpense` ·
  Varlık Türleri `Ingredients` · Web Siteleri `WebSite` · Konumlar `Location`
- **Ayarlar:** `Settings` (profil, parola değiştir, 2FA kur/kaldır)

`ReportController` (~555 satır) alt raporları: Hesap Bakiyeleri, Hesap Hareketleri,
Gelir/Gider, Gelir Türü, Gider Türü, Borç/Alacak, Portföy.

## 9. Yaygın desenler (yeni özellik eklerken uy)
- Liste ekranları: `Index` GET, view'e `List<Entity>` + `ViewBag` ile dropdown verileri.
- CRUD: `[HttpPost][ValidateAntiForgeryToken]`, `Json(new { success, message, data })` döner;
  ön yüz AJAX ile tabloyu yeniler. Detay için `GetXDetails(id)` yardımcı metodu tr-TR formatlı obje döner.
- Sıralama: `SaveOrder(List<int> itemIds)` deseni — sürükle-bırak sonrası `_order` alanı 1'den yeniden yazılır
  (PaymentType, IncomeType, ExpenseType, Person'da mevcut; son commit'ler bununla ilgili).
- Sistem (`is_bank`) kayıtları normal listelerde `Where(is_bank == false)` ile filtrelenir.

## 10. Bilinen kırılganlıklar / dikkat noktaları
- `paymenttype_balance` (cache) ile SUM-tabanlı bakiye tutarsız kalabilir; `PaymentType/Edit` eşitler.
- `ExpenseType.expensetype_order` **string** (diğerlerinde int); sıralamada string karşılaştırmasına dikkat.
- `EnsureCreated()` + migration birlikte; şema değişiminde manuel senkron gerekebilir.
- Parola hash'i saltsız SHA256; kasa parolaları düz metin — güvenlik iyileştirmesi yapılırsa buradan başla.
- appsettings.json'da düz DB parolası ve `Keys/` altındaki DataProtection anahtarları repoda.
