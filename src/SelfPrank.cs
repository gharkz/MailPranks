using System;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;

namespace MailPranks
{
    internal static class SelfPrank
    {
        internal const int PendingKey = 1749236806, DuelKey = 1749236807, MoonOriginalKey = 1749236808;
        internal sealed class Request { public int TownUid; public int TargetUid; public bool Moongate; public bool Resolved; public string Token; }
        internal static bool IsDuelTarget(Chara c) => c != null && PrankService.Ready && c.GetInt(DuelKey) == EClass.pc.uid;
        internal static bool IsPair(Chara a, Chara b) => a != null && b != null &&
            ((a.IsPC && IsDuelTarget(b)) || (b.IsPC && IsDuelTarget(a)));

        internal static void Send(Game game, Chara seller, Zone town, int targetUid)
        {
            if (!PrankService.Ready || EClass.game != game || !seller.IsAliveInCurrentZone ||
                !PrankService.TownAllowed(town) || EClass.pc.GetStr(PendingKey) != null ||
                (targetUid != 0 && !Recipients.List(town).Any(c => c.Uid == targetUid && !c.Dead)))
            { Dialog.Ok(Text.Stale); return; }
            // Persist before MoveZone: native transitions may autosave the departure map.
            EClass.pc.SetStr(PendingKey, JsonConvert.SerializeObject(new Request { TownUid = town.uid, TargetUid = targetUid }));
            try
            {
                if (EClass._zone != town) EClass.pc.MoveZone(town, ZoneTransition.EnterState.Return);
                else Tick();
            }
            catch { EClass.pc.SetStr(PendingKey, null); throw; }
        }

        internal static void Tick()
        {
            string raw = EClass.pc.GetStr(PendingKey);
            if (raw == null) return;
            var request = JsonConvert.DeserializeObject<Request>(raw);
            if (request == null) throw new InvalidOperationException("Invalid self-delivery request.");
            if (request.Moongate && !request.Resolved)
            {
                if (!MoonPrank.IsRunning(EClass.game, request.Token))
                {
                    EClass.pc.SetStr(PendingKey, null);
                    Dialog.Ok(Text.MoonUnavailable);
                }
                return;
            }
            if (EClass.game.isLoading || LayerProgress.isActive) return;
            if (request.Moongate && EClass._zone.uid == request.TownUid &&
                !PrankPolicy.MoonArrivalReady(request.Resolved, true, EClass._zone.instance is ZoneInsstanceMoongate, EClass.game.isLoading, LayerProgress.isActive))
            {
                EClass.pc.SetStr(PendingKey, null);
                Dialog.Ok(Text.MoonUnavailable);
                return;
            }
            if (EClass._zone.uid != request.TownUid)
            {
                if (EClass.player.nextZone != null && EClass.player.nextZone.uid == request.TownUid) return;
                EClass.pc.SetStr(PendingKey, null);
                Dialog.Ok(Text.SelfUnavailable);
                return;
            }
            var candidates = EClass._map.charas.Where(PrankService.Resident)
                .Where(c => request.TargetUid == 0 || c.uid == request.TargetUid).ToList();
            Chara target = null;
            Point spot = null;
            // Select from the loaded map, trying another resident if there is no landing tile.
            while (candidates.Count > 0 && spot == null)
            {
                int index = EClass.rnd(candidates.Count);
                target = candidates[index];
                candidates.RemoveAt(index);
                for (int dx = -1; dx <= 1 && spot == null; dx++)
                for (int dz = -1; dz <= 1 && spot == null; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    var p = new Point(target.pos.x + dx, target.pos.z + dz);
                    if (DeliverySpace.CanLand(p)) spot = p;
                }
            }
            // Consume once even on failure; never teleport again on a later turn or reload.
            EClass.pc.SetStr(PendingKey, null);
            if (spot == null) { Dialog.Ok(Text.SelfUnavailable); return; }
            EClass.pc.Teleport(spot, force: true);
            if (!EClass.pc.pos.Equals(spot)) { Dialog.Ok(Text.SelfUnavailable); return; }
            PartyArrival.Gather();
            // Use the native mail container factory; it explicitly clears generated contents.
            EClass._zone.AddCard(ThingGen.CreateCardboardBox(), spot).Install();
            if (request.Moongate)
            {
                foreach (var resident in EClass._map.charas.Where(PrankService.Resident).ToArray())
                {
                    bool newlyAngry = !resident.IsHostile(EClass.pc);
                    resident.SetInt(DuelKey, 0);
                    if (resident.GetInt(MoonOriginalKey) == 0) resident.SetInt(MoonOriginalKey, (int)resident.c_originalHostility + 100);
                    resident.c_originalHostility = Hostility.Enemy;
                    resident.SetHostility(Hostility.Enemy);
                    resident.SetEnemy(EClass.pc);
                    if (newlyAngry && resident.CanSee(EClass.pc) && resident.CanSeeLos(EClass.pc)) resident.TalkRaw(Text.Reaction(EClass.rnd(6)));
                }
                EClass.pc.SetEnemy(target);
                return;
            }
            bool newlyHostile = !IsPair(EClass.pc, target) && !target.IsHostile(EClass.pc);
            target.SetInt(DuelKey, EClass.pc.uid);
            EClass.pc.SetEnemy(target);
            target.SetEnemy(EClass.pc);
            if (newlyHostile && !target.IsPCParty) target.TalkRaw(Text.Reaction(EClass.rnd(6)));
        }
    }

    // Native hover text, interaction choices, and combat reacquisition read this property.
    // Overlay the saved duel marker without changing the NPC's original faction hostility.
    [HarmonyPatch(typeof(Chara), nameof(Chara.hostility), MethodType.Getter)]
    internal static class SelfPrankHostility
    {
        static void Postfix(Chara __instance, ref Hostility __result)
        {
            if (SelfPrank.IsDuelTarget(__instance)) __result = Hostility.Enemy;
        }
    }

    [HarmonyPatch(typeof(Zone), nameof(Zone.ResetHostility))]
    internal static class ResetPrankHostility
    {
        static void Prefix(Zone __instance)
        {
            if (!PrankService.Ready || __instance != EClass._zone) return;
            foreach (var c in EClass._map.charas)
            {
                c.SetInt(SelfPrank.DuelKey, 0);
                int original = c.GetInt(SelfPrank.MoonOriginalKey);
                if (original == 0) continue;
                c.c_originalHostility = (Hostility)(original - 100);
                c.SetInt(SelfPrank.MoonOriginalKey, 0);
            }
            // Native reset now restores hostility and clears combat targets normally.
        }
    }

    // Native DoHostileAction calls guards/party members and changes general hostility.
    // The prank instead establishes only the two participants' enemy relationship.
    [HarmonyPatch(typeof(Chara), nameof(Chara.DoHostileAction))]
    internal static class SelfPrankCombat
    {
        static bool Prefix(Chara __instance, Card _tg)
        {
            var other = _tg?.Chara;
            if (!SelfPrank.IsPair(__instance, other)) return true;
            __instance.SetEnemy(other);
            other.SetEnemy(__instance);
            return false;
        }
    }
    [HarmonyPatch(typeof(Chara), nameof(Chara.CallHelp))]
    internal static class SelfPrankNoHelp
    {
        static bool Prefix(Chara __instance, Chara tg) => !SelfPrank.IsPair(__instance, tg);
    }
}
