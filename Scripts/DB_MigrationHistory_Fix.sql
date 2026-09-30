/*
====================================================================
  BugraLife — Migration History Onarım + Yeni Migration Uygulama
====================================================================
  NEDEN?
  Uygulama Program.cs içinde context.Database.EnsureCreated() kullandığı
  için, DB şeması migration'larla değil "tek seferde" oluşturuldu.
  Bu yüzden __EFMigrationsHistory tablosu BOŞ kaldı. Boş history ile
  `dotnet ef database update` çalıştırılırsa EF, zaten var olan tabloları
  yeniden oluşturmaya çalışır ve patlar.

  BU SCRIPT NE YAPAR? (bir DB'de yalnızca BİR KEZ çalıştır)
  1) __EFMigrationsHistory tablosunu (yoksa) oluşturur.
  2) Bu güne kadarki TÜM eski migration'ları "uygulanmış" olarak işaretler
     (yani DB şemasını olduğu gibi baseline alır).
  3) En yeni migration'ın (ödeme türü + kesim günü) kolonlarını —yoksa—
     ekler ve onu da history'e yazar.

  Tüm adımlar IF NOT EXISTS ile korunmuştur; tekrar çalıştırmak zararsızdır.

  KULLANIM: Bu dosyayı SSMS'te ilgili veritabanına (BugraLifeDB) bağlıyken
  aç ve çalıştır. Hem ev PC'ndeki hem diğer PC'deki DB için ayrı ayrı çalıştır.
  Sonrasında her iki makinede `dotnet ef database update` "No migrations
  were applied" der ve artık migration'lar düzgün ilerler.
====================================================================
*/

SET NOCOUNT ON;

-- 1) History tablosunu oluştur (yoksa)
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

-- 2) Eski migration'ları baseline'la (uygulanmış say)
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT v.MigrationId, N'8.0.22'
FROM (VALUES
    (N'20251214145045_db_olusuturuldu'),
    (N'20251214152335_tum_tablolar_olusturuldu'),
    (N'20251214185913_person_tablosunda_bankamı_veritipi_degistirildi'),
    (N'20251214194347_gider_turu_tablosuna_aciklama_getirildi'),
    (N'20251215180204_paymenttype_tablosuna_iscredircard_sutunu_eklendi'),
    (N'20251216180455_sabit_giderler_tablosu_eklendi'),
    (N'20251216191422_aktiviteler_tablolari_olusturuldu'),
    (N'20251216195951_2fa_eklendi'),
    (N'20251220152907_praticalnotes_tablosueklendi'),
    (N'20251222172924_dosyapaylasimiozelligigetirildi'),
    (N'20251226180954_gider_turune_komisyon_eklendi')
) AS v(MigrationId)
WHERE NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory] h WHERE h.MigrationId = v.MigrationId
);
GO

-- 3) En yeni migration: kolonları ekle (yoksa) + history'e yaz
--    PaymentTypes.statement_day (kredi kartı hesap kesim günü)
IF COL_LENGTH(N'[PaymentTypes]', N'statement_day') IS NULL
BEGIN
    ALTER TABLE [PaymentTypes] ADD [statement_day] int NULL;
END;
GO

--    FixedExpenses.paymenttype_id (sabit giderin ödeme hesabı)
IF COL_LENGTH(N'[FixedExpenses]', N'paymenttype_id') IS NULL
BEGIN
    ALTER TABLE [FixedExpenses] ADD [paymenttype_id] int NULL;
END;
GO

--    Index (yoksa)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FixedExpenses_paymenttype_id' AND object_id = OBJECT_ID(N'[FixedExpenses]'))
BEGIN
    CREATE INDEX [IX_FixedExpenses_paymenttype_id] ON [FixedExpenses] ([paymenttype_id]);
END;
GO

--    Foreign key (yoksa)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FixedExpenses_PaymentTypes_paymenttype_id')
BEGIN
    ALTER TABLE [FixedExpenses]
        ADD CONSTRAINT [FK_FixedExpenses_PaymentTypes_paymenttype_id]
        FOREIGN KEY ([paymenttype_id]) REFERENCES [PaymentTypes] ([paymenttype_id]);
END;
GO

--    History kaydı (yoksa)
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = N'20260930102439_sabit_gidere_odeme_turu_ve_krediye_kesim_gunu')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260930102439_sabit_gidere_odeme_turu_ve_krediye_kesim_gunu', N'8.0.22');
END;
GO

PRINT 'Bitti: migration history dolduruldu ve yeni kolonlar uygulandı.';
GO
