using System;
using System.Collections.Generic;

namespace MailPranks
{
    public static class PrankPolicy
    {
        public const int RestockHours = 4 * 24;
        public const int PackageCapacity = 8;
        public const int OpeningAffinityLoss = 5;
        public static bool MoonArrivalReady(bool resolved, bool destinationLoaded, bool moongateInstance, bool loading, bool progressActive)
            => resolved && destinationLoaded && moongateInstance && !loading && !progressActive;
        public static int PackageCount(int count) => count <= 0 ? 0 : (count - 1) / PackageCapacity + 1;
        public static int Price(int normalPrice)
        {
            if (normalPrice < 0) throw new ArgumentOutOfRangeException(nameof(normalPrice));
            return Math.Max(1, (int)(((long)normalPrice + 19) / 20));
        }
    }

    public sealed class Delivery
    {
        public string Id;
        public int CreatureUid, TownUid, TargetUid, ParcelUid, Cost;
        public string CreatureName, TownName, TargetName;
        public string BundleId;
        public int RecipientUid;
        public string Status = "pending";
    }

    public sealed class Bundle
    {
        public string Id;
        public int TownUid, RecipientUid, ParcelUid, PlacedTurn, OpenerUid;
        public bool Opened, Closed, AffinityCharged, Reacted;
    }

    public sealed class Ledger
    {
        public int Schema = 2;
        public List<Delivery> Deliveries = new List<Delivery>();
        public List<Bundle> Bundles = new List<Bundle>();
        public void Migrate()
        {
            if (Schema != 1) return;
            if (Deliveries == null) throw new InvalidOperationException("Missing legacy deliveries.");
            Bundles = new List<Bundle>();
            foreach (var d in Deliveries)
            {
                if (d == null) throw new InvalidOperationException("Invalid legacy delivery.");
                d.RecipientUid = d.TargetUid;
                if (d.ParcelUid == 0) continue;
                d.BundleId = d.Id;
                Bundles.Add(new Bundle { Id = d.Id, TownUid = d.TownUid, RecipientUid = d.TargetUid,
                    ParcelUid = d.ParcelUid, Opened = d.Status == "opened", Closed = d.Status == "opened" || d.Status == "missing" });
            }
            Schema = 2;
        }
        public void Validate()
        {
            if (Schema != 2 || Deliveries == null || Bundles == null) throw new InvalidOperationException("Unsupported ledger schema.");
            var bundles = new HashSet<string>();
            foreach (var b in Bundles)
                if (b == null || string.IsNullOrEmpty(b.Id) || !bundles.Add(b.Id) || b.TownUid <= 0 || b.RecipientUid < 0 || b.ParcelUid < 0)
                    throw new InvalidOperationException("Invalid package record.");
            var ids = new HashSet<string>();
            foreach (var d in Deliveries)
                if (d == null || string.IsNullOrEmpty(d.Id) || !ids.Add(d.Id) || d.CreatureUid <= 0 || d.TownUid <= 0 || d.Cost < 1 ||
                    (d.BundleId != null && !bundles.Contains(d.BundleId)) ||
                    !(d.Status == "pending" || d.Status == "waiting" || d.Status == "awaiting_target" || d.Status == "opening" || d.Status == "delivered" || d.Status == "opened" || d.Status == "missing"))
                    throw new InvalidOperationException("Invalid delivery record; ledger left untouched.");
        }
    }
}
