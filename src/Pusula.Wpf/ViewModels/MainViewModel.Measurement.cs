using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Pusula.Core.Notes;
using Pusula.Core.Testing;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Kayıt penceresinde ölçüm alanları: işlem "Ölçüm" (ya da "Test bitti") seçilince <b>Madde</b> (— Yok · doğrulama
    /// maddeleri · + Yeni ek madde) ve <b>Sonuç</b> (Geçti · Kaldı; madde yoksa Bilgi) açılır. Kayıt Günlük'e
    /// <c>madde: TG-1 · sonuç: Kaldı</c> bilgisiyle yazılır; madde seçiliyse doğrulama dosyasında maddenin durumu
    /// sonuçla güncellenir, yeni ek madde (<c>EK-n</c>) tabloya eklenir.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private const string NewItemKey = "\u0001new";

        private string journalItem = string.Empty;
        private string journalResult = string.Empty;
        private string journalNewItemText = string.Empty;
        private string journalNewItemSource = string.Empty;
        private string journalNewItemNo = string.Empty;
        private string journalItemStock = string.Empty;
        private List<VerificationItem> journalItems = new List<VerificationItem>();

        /// <summary>Ölçüm alanları görünür mü (yapıldı kaydı ve işlem ölçüm türünden).</summary>
        public bool IsJournalMeasurement
        {
            get { return JournalDone && IsMeasurementKind(journalKind); }
        }

        /// <summary>Madde seçenekleri: — Yok, stokun doğrulama maddeleri (durum rozetiyle), + Yeni ek madde.</summary>
        public ObservableCollection<ChoiceOption> JournalItemOptions { get; } = new ObservableCollection<ChoiceOption>();

        /// <summary>Seçili madde (açılır liste).</summary>
        public ChoiceOption? JournalItemOption
        {
            get { return JournalItemOptions.FirstOrDefault(o => o.Key == journalItem) ?? JournalItemOptions.FirstOrDefault(); }
            set { JournalItem = value?.Key ?? string.Empty; }
        }

        /// <summary>Seçili maddenin anahtarı (TG no, <c>EK-1</c>); boş = yok; yeni ek madde için iç anahtar.</summary>
        public string JournalItem
        {
            get
            {
                return journalItem;
            }

            set
            {
                if (!SetProperty(ref journalItem, value ?? string.Empty))
                {
                    return;
                }

                if (journalItem.Length == 0)
                {
                    journalResult = Resources.Journal_ResultInfo;
                }
                else if (string.Equals(journalResult, Resources.Journal_ResultInfo, StringComparison.CurrentCultureIgnoreCase))
                {
                    journalResult = string.Empty;
                }

                OnMeasurementChanged();
            }
        }

        /// <summary>Yeni ek madde açılıyor mu (pencerede madde metni ve kaynak kutusu görünür).</summary>
        public bool IsJournalNewItem
        {
            get { return IsJournalMeasurement && journalItem == NewItemKey; }
        }

        /// <summary>Yeni ek madde kutusunun başlığı ("YENİ EK MADDE · EK-3").</summary>
        public string JournalNewItemCaption
        {
            get { return string.Format(CultureInfo.CurrentCulture, Resources.Journal_NewItemCaption, journalNewItemNo); }
        }

        /// <summary>Yeni ek maddenin metni (zorunlu).</summary>
        public string JournalNewItemText
        {
            get
            {
                return journalNewItemText;
            }

            set
            {
                if (SetProperty(ref journalNewItemText, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(JournalPrimaryText));
                }
            }
        }

        /// <summary>Yeni ek maddenin kaynağı (doküman bölümü ya da sözlü isteyen); isteğe bağlı.</summary>
        public string JournalNewItemSource
        {
            get { return journalNewItemSource; }
            set { SetProperty(ref journalNewItemSource, value ?? string.Empty); }
        }

        /// <summary>Seçili sonuç (Geçti, Kaldı, Bilgi; madde seçiliyken seçilmemişse boş).</summary>
        public string JournalResult
        {
            get
            {
                return journalResult;
            }

            set
            {
                if (SetProperty(ref journalResult, value ?? string.Empty))
                {
                    OnMeasurementChanged();
                }
            }
        }

        /// <summary>Sonuç hapları: madde varsa Geçti · Kaldı, yoksa yalnız Bilgi.</summary>
        public IList<ChoiceOption> JournalResultChoices
        {
            get
            {
                var names = journalItem.Length == 0
                    ? new[] { Resources.Journal_ResultInfo }
                    : new[] { Resources.Verify_StatusPassed, Resources.Verify_StatusFailed };
                return names.Select(n => new ChoiceOption(n, n, string.Equals(n, journalResult, StringComparison.CurrentCultureIgnoreCase), o => JournalResult = o.Key)).ToList();
            }
        }

        /// <summary>Sonucun altındaki açıklama: madde yoksa bilgi amaçlı; varsa durumun nasıl değişeceği.</summary>
        public string JournalResultHint
        {
            get
            {
                if (journalItem.Length == 0)
                {
                    return Resources.Journal_ResultNoItem;
                }

                var item = journalItems.FirstOrDefault(i => string.Equals(VerificationList.KeyOf(i), journalItem, StringComparison.OrdinalIgnoreCase));
                if (journalResult.Length > 0 && journalItem != NewItemKey)
                {
                    var before = item == null || item.EffectiveStatus.Length == 0 ? Resources.Verify_StatusPending : item.EffectiveStatus;
                    return string.Format(CultureInfo.CurrentCulture, Resources.Journal_ResultChange, before, journalResult);
                }

                return Resources.Journal_ResultHint;
            }
        }

        /// <summary>Ölçüm alanları tamam mı: madde seçiliyse sonuç seçilmiş, yeni ek maddede metin yazılmış olmalı.</summary>
        private bool JournalMeasurementReady
        {
            get
            {
                if (!IsJournalMeasurement)
                {
                    return true;
                }

                if (journalItem == NewItemKey && journalNewItemText.Trim().Length == 0)
                {
                    return false;
                }

                return journalItem.Length == 0 || journalResult.Length > 0;
            }
        }

        /// <summary>İşlem ölçüm türünden mi (Ölçüm ya da Test bitti).</summary>
        public static bool IsMeasurementKind(string kind)
        {
            var key = Journal.KindKey(kind ?? string.Empty);
            return key.Length > 0 && (key == Journal.KindKey(Resources.Event_Measurement) || key == Journal.KindKey(Resources.Event_TestEnd));
        }

        /// <summary>Doğrulama satırından "+ Deneme": kayıt penceresini Ölçüm ve madde seçili açar.</summary>
        public bool OpenMeasurementFor(VerificationRow row)
        {
            if (row == null || !OpenJournalHere())
            {
                return false;
            }

            JournalKind = Resources.Event_Measurement;
            JournalItem = VerificationList.KeyOf(row.Item);
            OnPropertyChanged(nameof(JournalItemOption));
            return true;
        }

        // Pencere açılınca (ya da düzenlemede) ölçüm alanları kurulur: madde listesi stokun doğrulama dosyasından.
        private void ResetJournalMeasurement(string folder, EventNote? note)
        {
            journalItemStock = KindOf(folder) == Pusula.Core.Tracking.RecordKind.Stock ? folder : string.Empty;
            journalItems = journalItemStock.Length == 0 ? new List<VerificationItem>() : VerificationList.Read(journalItemStock).ToList();
            journalNewItemNo = VerificationList.NextExtraNo(journalItems);
            journalNewItemText = string.Empty;
            journalNewItemSource = string.Empty;

            JournalItemOptions.Clear();
            JournalItemOptions.Add(new ChoiceOption(string.Empty, Resources.Journal_ItemNone, false, o => { }));
            foreach (var item in journalItems)
            {
                var key = VerificationList.KeyOf(item);
                var title = item.Text.Length > 0 ? item.Text : item.SubGroup;
                JournalItemOptions.Add(new ChoiceOption(key, title.Length > 0 ? key + " · " + title : key, false, o => { })
                {
                    Badge = item.EffectiveStatus.Length > 0 ? item.EffectiveStatus : Resources.Verify_StatusPending,
                });
            }

            if (journalItemStock.Length > 0)
            {
                JournalItemOptions.Add(new ChoiceOption(NewItemKey, Resources.Journal_ItemNew, false, o => { }));
            }

            journalItem = note?.Item ?? string.Empty;
            if (journalItem.Length > 0 && JournalItemOptions.All(o => !string.Equals(o.Key, journalItem, StringComparison.OrdinalIgnoreCase)))
            {
                // Dosyada artık olmayan madde: kaybolmasın diye listede tutulur.
                JournalItemOptions.Insert(1, new ChoiceOption(journalItem, journalItem, false, o => { }));
            }

            journalResult = note != null ? note.Result : (journalItem.Length == 0 ? Resources.Journal_ResultInfo : string.Empty);
            if (journalResult.Length == 0 && journalItem.Length == 0)
            {
                journalResult = Resources.Journal_ResultInfo;
            }

            OnMeasurementChanged();
            OnPropertyChanged(nameof(JournalNewItemText));
            OnPropertyChanged(nameof(JournalNewItemSource));
            ResetJournalFiles(note);
        }

        // Kayıt yazılmadan önce: yeni ek madde tabloya eklenir, sonuç kayda işlenir. Hata olursa false (durum çubuğunda).
        private bool PrepareJournalMeasurement(JournalEntry entry)
        {
            if (!IsJournalMeasurement)
            {
                return true;
            }

            if (journalItem == NewItemKey)
            {
                var text = journalNewItemText.Trim();
                if (text.Length == 0 || journalItemStock.Length == 0)
                {
                    return false;
                }

                var added = new VerificationItem
                {
                    Text = text,
                    Source = journalNewItemSource.Trim(),
                    Group = Resources.Verify_GroupOther,
                    TgNo = VerificationList.NextExtraNo(VerificationList.Read(journalItemStock)),
                };
                var template = string.Format(CultureInfo.CurrentCulture, Resources.Verify_FileTemplate, System.IO.Path.GetFileName(journalItemStock));
                var error = VerificationList.Add(journalItemStock, added, template);
                if (error.Length > 0)
                {
                    Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
                    return false;
                }

                journalItem = added.TgNo;
            }

            entry.Item = journalItem;
            entry.Result = journalResult.Trim();
            return true;
        }

        // Kayıt yazıldıktan sonra: madde seçili ve sonuç Geçti / Kaldı ise doğrulama dosyasında maddenin durumu güncellenir.
        private void ApplyJournalMeasurement(JournalEntry entry)
        {
            if (entry.Item.Length == 0 || journalItemStock.Length == 0
                || string.Equals(entry.Result, Resources.Journal_ResultInfo, StringComparison.CurrentCultureIgnoreCase) || entry.Result.Length == 0)
            {
                return;
            }

            var item = VerificationList.Read(journalItemStock).FirstOrDefault(i => string.Equals(VerificationList.KeyOf(i), entry.Item, StringComparison.OrdinalIgnoreCase));
            if (item == null || string.Equals(item.EffectiveStatus, entry.Result, StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }

            // Ölçüm sonucu elle durumdur (bağlardan hesaplananla aynıysa elle durum kalkar).
            item.ManualStatus = ManualStatusFor(entry.Result, item.Status);
            var error = VerificationList.Update(journalItemStock, item);
            if (error.Length > 0 && error != "missing")
            {
                Status = string.Format(CultureInfo.CurrentCulture, Resources.Verify_WriteFailed, error);
            }
        }

        private void OnMeasurementChanged()
        {
            OnPropertyChanged(nameof(JournalItem));
            OnPropertyChanged(nameof(JournalItemOption));
            OnPropertyChanged(nameof(IsJournalMeasurement));
            OnPropertyChanged(nameof(IsJournalNewItem));
            OnPropertyChanged(nameof(JournalNewItemCaption));
            OnPropertyChanged(nameof(JournalResult));
            OnPropertyChanged(nameof(JournalResultChoices));
            OnPropertyChanged(nameof(JournalResultHint));
            OnPropertyChanged(nameof(JournalPrimaryText));
            OnPropertyChanged(nameof(JournalFolderText));
        }
    }
}
