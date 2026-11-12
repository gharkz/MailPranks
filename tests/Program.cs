using System;
using System.Text.Json;
using MailPranks;

public static class Lang { public static string langCode; }
internal static class Program
{
    static int checks;
    static void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
    static void Reject(Action action, string label) { try { action(); } catch (InvalidOperationException) { checks++; return; } throw new Exception(label); }
    static Delivery Entry() => new Delivery { Id = "one", CreatureUid = 99, TownUid = 3, Cost = 1, CreatureName = "sheep", TownName = "town" };
    static void Main()
    {
        Check(PrankPolicy.Price(0) == 1, "minimum");
        Check(PrankPolicy.DeliveryTile(false,false,false), "clear teleport landing independent of origin or loose items");
        Check(!PrankPolicy.DeliveryTile(true,false,false), "blocked destination rejected");
        Check(!PrankPolicy.DeliveryTile(false,true,false), "occupied destination rejected");
        Check(!PrankPolicy.DeliveryTile(false,false,true), "installed container conflict rejected");
        Check(PrankPolicy.MoonArrivalReady(true, true, true, false, false), "moongate arrival after resolution and loading");
        Check(!PrankPolicy.MoonArrivalReady(false, true, true, false, false), "no placement before action resolves");
        Check(!PrankPolicy.MoonArrivalReady(true, false, true, false, false), "no placement in departure map");
        Check(!PrankPolicy.MoonArrivalReady(true, true, false, false, false), "no placement after unrelated travel");
        Check(!PrankPolicy.MoonArrivalReady(true, true, true, true, false), "no placement during map loading");
        Check(!PrankPolicy.MoonArrivalReady(true, true, true, false, true), "no placement before progress layer finishes");
        Check(PrankPolicy.Price(1) == 1, "small purchase");
        Check(PrankPolicy.Price(20) == 1, "exact five percent");
        Check(PrankPolicy.Price(21) == 2, "round up");
        Check(PrankPolicy.Price(int.MaxValue) == 107374183, "overflow-safe price");
        try { PrankPolicy.Price(-1); throw new Exception("negative accepted"); } catch (ArgumentOutOfRangeException) { checks++; }
        for (int i = 1; i <= 10000; i++) Check(PrankPolicy.Price(i) == (int)Math.Ceiling(i * 0.05m), "five percent property");
        var ledger = new Ledger(); ledger.Deliveries.Add(Entry()); ledger.Validate();
        var options = new JsonSerializerOptions { IncludeFields = true };
        string json = JsonSerializer.Serialize(ledger, options);
        var loaded = JsonSerializer.Deserialize<Ledger>(json, options); loaded.Validate();
        Check(loaded.Deliveries[0].CreatureUid == 99 && loaded.Deliveries[0].Status == "pending", "metadata round trip");
        var separate = new Ledger(); Check(separate.Deliveries.Count == 0, "independent ledgers");
        loaded.Deliveries.Add(Entry()); Reject(loaded.Validate, "duplicate delivery");
        ledger.Schema = 3; Reject(ledger.Validate, "future schema"); ledger.Schema = 2;
        ledger.Deliveries[0].Status = "invalid"; Reject(ledger.Validate, "invalid status");
        foreach (var status in new[] { "pending", "waiting", "delivered", "opened", "missing" }) { ledger.Deliveries[0].Status = status; ledger.Validate(); checks++; }
        ledger.Deliveries[0].CreatureUid = 0; Reject(ledger.Validate, "invalid creature");
        Check(PrankPolicy.PackageCount(0) == 0 && PrankPolicy.PackageCount(8) == 1 && PrankPolicy.PackageCount(9) == 2 && PrankPolicy.PackageCount(20) == 3, "overflow boundaries");
        var old = new Ledger { Schema = 1 }; var oldEntry = Entry(); oldEntry.ParcelUid = 40; oldEntry.Status = "delivered"; old.Deliveries.Add(oldEntry);
        old.Migrate(); old.Validate();
        Check(old.Schema == 2 && old.Bundles.Count == 1 && old.Bundles[0].ParcelUid == 40 && oldEntry.BundleId == oldEntry.Id, "legacy parcel migration");
        old.Migrate(); Check(old.Bundles.Count == 1, "migration idempotent");
        var history = new Ledger { Schema = 1 }; var past = Entry(); past.Status = "opened"; past.ParcelUid = 41; history.Deliveries.Add(past);
        history.Migrate(); history.Validate(); Check(history.Bundles[0].Closed && history.Bundles[0].Opened && !history.Bundles[0].AffinityCharged, "old history never retroactively charged");
        foreach (var language in new[] { "EN", "JP", "CN", "FR", null })
        {
            Lang.langCode = language;
            Check(!string.IsNullOrWhiteSpace(Text.Ledger), "ledger localization");
            Check(!string.IsNullOrWhiteSpace(Text.Confirm("sheep", "town", "any", 1)), "confirmation localization");
            Check(!string.IsNullOrWhiteSpace(Text.SelfConfirm("town", "any")), "self confirmation localization");
            Check(!string.IsNullOrWhiteSpace(Text.MoonConfirm("gate")), "moongate confirmation localization");
            foreach (var property in typeof(Text).GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic))
                Check(!string.IsNullOrWhiteSpace((string)property.GetValue(null)), "UI label " + property.Name);
            foreach (var status in new[] { "opened", "delivered", "waiting", "awaiting_target", "opening", "missing", "pending" })
                Check(!string.IsNullOrWhiteSpace(Text.Status(status)), "status localization " + status);
            for (int i = 0; i < 6; i++) Check(!string.IsNullOrWhiteSpace(Text.Reaction(i)), "reaction localization");
        }
        Lang.langCode = "EN"; var english = Text.Ledger; Lang.langCode = "FR"; Check(Text.Ledger == english, "English fallback");
        Console.WriteLine($"{checks} checks passed. Native purchase, movement, combat and save loading are not covered.");
    }
}
