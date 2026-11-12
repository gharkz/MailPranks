using System;
using System.Linq;
using Newtonsoft.Json;

namespace MailPranks
{
    internal static class PrankService
    {
        // Fixed numeric keys avoid runtime string-hash changes between boots.
        internal const int LedgerKey = 1749236801, CargoKey = 1749236802, MarkerKey = 1749236803, BookKey = 1749236804, StockKey = 1749236805;
        internal static SlaverData Stock(Chara seller)
        {
            var stock = seller.GetObj<SlaverData>(StockKey);
            if (stock == null) seller.SetObj(StockKey, stock = new SlaverData());
            // Native stock generation uses the same trader type, with this list's own expiry.
            // Preserve the seller's ordinary-shop bookkeeping changed by TryRefresh.
            bool restocking = seller.isRestocking;
            bool expired = EClass.world.date.IsExpired(stock.dateRefresh);
            try
            {
                stock.TryRefresh(seller);
                int deadline = EClass.world.date.GetRaw(PrankPolicy.RestockHours);
                // Also shorten existing 0.1.1 stock's longer timer once; never extend it on reopening.
                if (expired || stock.dateRefresh > deadline) stock.dateRefresh = deadline;
            }
            finally { seller.isRestocking = restocking; }
            return stock;
        }
        internal static string StockLabel(Chara seller)
        {
            var stock = Stock(seller);
            int hours = EClass.world.date.GetRemainingHours(stock.dateRefresh);
            return hours > 0 ? "nextRestock".lang(Text.Buy, Date.GetText(hours)) : Text.Buy;
        }
        internal static bool Ready => EClass.core != null && EClass.core.IsGameStarted && EClass.game != null && EClass.pc != null && EClass._zone != null;
        internal static Ledger Read()
        {
            var raw = EClass.pc.GetStr(LedgerKey);
            var ledger = raw == null ? new Ledger() : JsonConvert.DeserializeObject<Ledger>(raw);
            if (ledger == null) throw new InvalidOperationException("Null prank ledger.");
            ledger.Migrate();
            ledger.Validate();
            if (ledger.TrimHistory()) Write(ledger);
            return ledger;
        }
        internal static void Write(Ledger ledger)
        {
            ledger.Validate();
            ledger.TrimHistory();
            ledger.Validate();
            EClass.pc.SetStr(LedgerKey, JsonConvert.SerializeObject(ledger));
        }
        internal static bool TownAllowed(Zone town) => town != null && town.IsTown && !town.IsPCFaction && !town.IsInstance && town.lv == 0;
        internal static void Purchase(Game game, Chara seller, SlaverData stock, Chara creature, Zone town, int targetUid, string targetName, int quotedPrice)
        {
            if (!Ready || EClass.game != game || !seller.IsAliveInCurrentZone || seller.GetObj<SlaverData>(StockKey) != stock ||
                !stock.list.Contains(creature) || creature.isDead || !TownAllowed(town) || EClass.game.spatials.Find(town.uid) != town ||
                (targetUid != 0 && !Recipients.List(town).Any(c => c.Uid == targetUid && !c.Dead)) || PrankPolicy.Price(CalcMoney.BuySlave(creature)) != quotedPrice)
            { Dialog.Ok(Text.Stale); return; }
            var ledger = Read();
            var cargo = EClass.pc.GetObj<SlaverData>(CargoKey) ?? new SlaverData();
            if (cargo.list.Any(c => c.uid == creature.uid)) throw new InvalidOperationException("Creature already in mail cargo.");
            var delivery = new Delivery { Id = Guid.NewGuid().ToString("N"), CreatureUid = creature.uid, TownUid = town.uid,
                TargetUid = targetUid, CreatureName = creature.Name, TownName = town.Name, TargetName = targetName, Cost = quotedPrice };
            ledger.Deliveries.Add(delivery);
            ledger.Validate();
            var json = JsonConvert.SerializeObject(ledger);
            var previous = EClass.pc.GetStr(LedgerKey);
            var oldCargo = EClass.pc.GetObj<SlaverData>(CargoKey);
            if (!EClass.pc.TryPay(quotedPrice)) return;
            try
            {
                cargo.list.Add(creature);
                EClass.pc.SetObj(CargoKey, cargo);
                EClass.pc.SetStr(LedgerKey, json);
                stock.list.Remove(creature);
            }
            catch
            {
                cargo.list.Remove(creature);
                EClass.pc.SetObj(CargoKey, oldCargo);
                EClass.pc.SetStr(LedgerKey, previous);
                if (!stock.list.Contains(creature)) stock.list.Add(creature);
                EClass.pc.ModCurrency(quotedPrice);
                throw;
            }
            Dialog.Ok(Text.Sent);
        }

        internal static void Tick() => DeliveryEngine.Tick();
        internal static bool Resident(Chara c) => c != null && c.IsAliveInCurrentZone && !c.IsPCParty && !c.IsPCPartyMinion &&
            c.GetStr(MarkerKey) == null;
    }
}
