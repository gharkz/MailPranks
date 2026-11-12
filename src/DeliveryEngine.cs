using System;
using System.Linq;
using System.Collections.Generic;

namespace MailPranks
{
    internal static class DeliveryEngine
    {
        internal static void Tick()
        {
            if (!PrankService.Ready) return;
            var ledger = PrankService.Read();
            if (!ledger.Deliveries.Any(d => d.Status != "opened" && d.Status != "missing")) return;
            AssignRecipients(ledger);
            BundlePending(ledger);
            foreach (var bundle in ledger.Bundles.Where(b => !b.Closed).ToArray())
            {
                var parcel = FindParcel(bundle);
                var target = Target(bundle.RecipientUid);
                if (parcel == null && bundle.ParcelUid == 0)
                {
                    if (bundle.TownUid != EClass._zone.uid || target == null || !Visible(target)) continue;
                    var spot = FreeAdjacent(target.pos);
                    if (spot == null) { SetStatus(ledger, bundle, "waiting"); continue; }
                    // The bundle is saved on the player before creation; its token recovers a partial placement.
                    PrankService.Write(ledger);
                    parcel = ThingGen.CreateCardboardBox();
                    parcel.SetStr(PrankService.MarkerKey, bundle.Id);
                    EClass._zone.AddCard(parcel, spot).Install();
                    bundle.ParcelUid = parcel.uid;
                    bundle.PlacedTurn = EClass.player.stats.turns;
                    foreach (var d in Contents(ledger, bundle)) { d.ParcelUid = parcel.uid; d.Status = "delivered"; }
                    PrankService.Write(ledger);
                }
                else if (parcel == null)
                {
                    if (bundle.TownUid == EClass._zone.uid) { SetStatus(ledger, bundle, "missing"); bundle.Closed = true; }
                    continue;
                }
                if (bundle.ParcelUid == 0) { bundle.ParcelUid = parcel.uid; SetStatus(ledger, bundle, "delivered"); }
                // Old single-creature parcels retain their position and receive a designated opener.
                if (bundle.RecipientUid == 0)
                {
                    target = EClass._map.charas.Where(PrankService.Resident).OrderBy(c => c.Dist(parcel)).FirstOrDefault();
                    if (target != null) bundle.RecipientUid = target.uid;
                }
                if (bundle.Opened) { Release(ledger, bundle, parcel); continue; }
                if (target == null || target.IsDeadOrSleeping || target.enemy != null ||
                    EClass.player.stats.turns <= bundle.PlacedTurn || !EClass._map.things.Contains(parcel)) continue;
                if (!target.CanSee(parcel) || !target.CanSeeLos(parcel)) continue;
                if (target.Dist(parcel) <= 1 && target.CanInteractTo(parcel.pos))
                    Open(ledger, bundle, parcel, target);
                else if (target.ai != null && target.ai.IsIdle)
                    target.SetAI(new AI_Goto(parcel, 1));
            }
            PrankService.Write(ledger);
        }

        private static void AssignRecipients(Ledger ledger)
        {
            var pending = ledger.Deliveries.Where(d => d.TownUid == EClass._zone.uid && d.BundleId == null && d.Status != "opened" && d.Status != "missing").ToArray();
            Chara anyone = null;
            foreach (var d in pending)
            {
                if (d.RecipientUid == 0)
                {
                    if (d.TargetUid != 0) d.RecipientUid = d.TargetUid;
                    else
                    {
                        if (anyone == null) anyone = EClass._map.charas.Where(PrankService.Resident)
                            .OrderBy(c => Visible(c) ? 0 : 1).ThenBy(_ => EClass.rnd(100000)).FirstOrDefault();
                        if (anyone != null) d.RecipientUid = anyone.uid;
                    }
                }
                var target = Target(d.RecipientUid);
                d.Status = target == null ? "awaiting_target" : "pending";
            }
        }

        private static void BundlePending(Ledger ledger)
        {
            var groups = ledger.Deliveries.Where(d => d.TownUid == EClass._zone.uid && d.BundleId == null && d.RecipientUid != 0 && d.Status == "pending")
                .GroupBy(d => d.RecipientUid).ToArray();
            foreach (var group in groups)
            {
                var target = Target(group.Key);
                if (target == null || !Visible(target)) continue;
                var remaining = new Queue<Delivery>(group);
                foreach (var existing in ledger.Bundles.Where(b => b.TownUid == EClass._zone.uid && b.RecipientUid == group.Key && !b.Opened && !b.Closed).ToArray())
                {
                    // A destroyed or stolen parcel must not swallow a new purchase.
                    if (existing.ParcelUid != 0 && FindParcel(existing) == null) continue;
                    int available = PrankPolicy.PackageCapacity - Contents(ledger, existing).Count();
                    while (available-- > 0 && remaining.Count > 0) Assign(remaining.Dequeue(), existing);
                }
                while (remaining.Count > 0)
                {
                    var bundle = new Bundle { Id = Guid.NewGuid().ToString("N"), TownUid = EClass._zone.uid, RecipientUid = group.Key };
                    ledger.Bundles.Add(bundle);
                    for (int n = 0; n < PrankPolicy.PackageCapacity && remaining.Count > 0; n++) Assign(remaining.Dequeue(), bundle);
                }
                PrankService.Write(ledger);
            }
        }
        private static void Assign(Delivery d, Bundle b) { d.BundleId = b.Id; d.ParcelUid = b.ParcelUid; d.Status = b.ParcelUid == 0 ? "waiting" : "delivered"; }

        internal static bool Open(Thing parcel, Chara opener)
        {
            if (!PrankService.Ready || parcel == null || opener == null) return false;
            var ledger = PrankService.Read();
            var bundle = ledger.Bundles.SingleOrDefault(b => b.Id == parcel.GetStr(PrankService.MarkerKey));
            if (bundle == null || bundle.Closed || (!opener.IsPC && opener.uid != bundle.RecipientUid)) return false;
            Open(ledger, bundle, parcel, opener);
            PrankService.Write(ledger);
            return true;
        }
        private static void Open(Ledger ledger, Bundle bundle, Thing parcel, Chara opener)
        {
            if (!bundle.Opened)
            {
                bundle.Opened = true;
                bundle.OpenerUid = opener.uid;
                // Opening is a single synchronous native operation. Persist the once-only marker first.
                bundle.AffinityCharged = !opener.IsPC;
                SetStatus(ledger, bundle, "opening");
                PrankService.Write(ledger);
                if (!opener.IsPC) opener.affinity.Mod(-PrankPolicy.OpeningAffinityLoss);
            }
            Release(ledger, bundle, parcel);
        }
        private static void Release(Ledger ledger, Bundle bundle, Thing parcel)
        {
            var cargo = EClass.pc.GetObj<SlaverData>(PrankService.CargoKey);
            Point center = EClass._map.things.Contains(parcel) ? parcel.pos : EClass.pc.pos;
            foreach (var d in Contents(ledger, bundle).Where(d => d.Status != "opened" && d.Status != "missing").ToArray())
            {
                var creature = cargo?.list.SingleOrDefault(c => c.uid == d.CreatureUid);
                var already = EClass._map.charas.Concat(EClass._map.deadCharas).FirstOrDefault(c => c.GetStr(PrankService.MarkerKey) == d.Id);
                if (already != null) { d.Status = "opened"; if (creature != null) cargo.list.Remove(creature); continue; }
                if (creature == null) { d.Status = "missing"; continue; }
                Point spot = FreeAdjacent(center, placePackage: false);
                if (spot == null) break; // Keep the opened package and remaining cargo; retry when space clears.
                var previouslyPeaceful = new HashSet<int>(EClass._map.charas
                    .Where(c => PrankService.Resident(c) && !c.IsHostile(creature)).Select(c => c.uid));
                creature.SetStr(PrankService.MarkerKey, d.Id);
                creature.c_originalHostility = Hostility.Enemy;
                creature.SetHostility(Hostility.Enemy);
                EClass._zone.AddCard(creature, spot);
                var witnesses = EClass._map.charas.Where(c => PrankService.Resident(c) && c.CanSee(creature) && c.CanSeeLos(creature)).ToArray();
                creature.SetEnemy(Target(bundle.RecipientUid) ?? witnesses.FirstOrDefault());
                foreach (var resident in witnesses)
                {
                    resident.SetEnemy(creature);
                    if (!bundle.Reacted && previouslyPeaceful.Contains(resident.uid)) resident.TalkRaw(Text.Reaction(EClass.rnd(6)));
                }
                bundle.Reacted = true;
                d.Status = "opened";
                cargo.list.Remove(creature);
                PrankService.Write(ledger);
            }
            if (Contents(ledger, bundle).All(d => d.Status == "opened" || d.Status == "missing"))
            {
                bundle.Closed = true;
                PrankService.Write(ledger);
                parcel.Destroy();
            }
        }
        private static IEnumerable<Delivery> Contents(Ledger ledger, Bundle bundle) => ledger.Deliveries.Where(d => d.BundleId == bundle.Id);
        private static void SetStatus(Ledger ledger, Bundle bundle, string status)
        {
            foreach (var d in Contents(ledger, bundle).Where(d => d.Status != "opened" && d.Status != "missing")) d.Status = status;
        }
        private static Chara Target(int uid) => uid == 0 ? null : EClass._map.charas.FirstOrDefault(c => c.uid == uid && PrankService.Resident(c));
        private static bool Visible(Chara c) => EClass.pc.CanSee(c) && EClass.pc.CanSeeLos(c);
        private static Thing FindParcel(Bundle b) => Walk(EClass._map.things).Concat(Walk(EClass.pc.things))
            .FirstOrDefault(t => t.GetStr(PrankService.MarkerKey) == b.Id && (b.ParcelUid == 0 || t.uid == b.ParcelUid));
        private static IEnumerable<Thing> Walk(IEnumerable<Thing> roots)
        {
            foreach (var thing in roots) { yield return thing; foreach (var nested in Walk(thing.things)) yield return nested; }
        }
        private static Point FreeAdjacent(Point center, bool placePackage = true)
        {
            for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                var p = new Point(center.x + dx, center.z + dz);
                if (DeliverySpace.CanLand(p, placePackage)) return p;
            }
            return null;
        }
    }
}
