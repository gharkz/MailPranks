using System;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace MailPranks
{
    [BepInPlugin(Id, "Mail Pranks", "0.2.5")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "gharkz.elin.mailpranks";
        internal static Plugin Instance;
        internal static Sprite LedgerSprite;
        private Harmony harmony;
        private Game observedGame;
        private Zone observedZone;
        private int lastTurn = -1;
        private bool faulted;
        private void Awake()
        {
            Instance = this;
            var path = Path.Combine(Path.GetDirectoryName(Info.Location), "assets", "ledger.png");
            if (File.Exists(path))
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path))) throw new InvalidDataException("Invalid ledger sprite.");
                LedgerSprite = SpriteAsset.CreateInventorySprite(texture);
                UnityEngine.Object.Destroy(texture);
            }
            harmony = new Harmony(Id);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Mail Pranks 0.2.5 loaded.");
        }
        internal void ReportMoonError(Exception error) => Logger.LogError(error);
        internal static void Guard(Action action)
        {
            try { action(); }
            catch (Exception ex) { Instance.Logger.LogError(ex); Instance.faulted = true; Dialog.Ok(Text.Error); }
        }
        private void Update()
        {
            if (!PrankService.Ready) { observedGame = null; observedZone = null; lastTurn = -1; return; }
            if (observedGame != EClass.game) { observedGame = EClass.game; observedZone = null; lastTurn = -1; faulted = false; }
            if (faulted || Game.isPaused) return;
            Guard(SelfPrank.Tick);
            if (faulted) return;
            if (observedZone == EClass._zone && lastTurn == EClass.player.stats.turns) return;
            observedZone = EClass._zone;
            lastTurn = EClass.player.stats.turns;
            Guard(PrankService.Tick);
        }
        private void OnDestroy() { harmony?.UnpatchSelf(); }
    }

    [HarmonyPatch(typeof(DramaCustomSequence), nameof(DramaCustomSequence.Build))]
    internal static class ConversationContext
    {
        [ThreadStatic] internal static Chara Seller;
        static void Prefix(Chara c, out Chara __state) { __state = Seller; Seller = c; }
        static Exception Finalizer(Exception __exception, Chara __state) { Seller = __state; return __exception; }
    }
    [HarmonyPatch(typeof(DramaCustomSequence), nameof(DramaCustomSequence.Choice2))]
    internal static class TraderChoice
    {
        static void Postfix(DramaCustomSequence __instance, string idJump)
        {
            var seller = ConversationContext.Seller;
            if (idJump != "_buySlave" || seller == null || !(seller.trait is TraitSlaver || seller.trait is TraitAnimalTamer)) return;
            var choice = new DramaChoice(PrankService.StockLabel(seller), "_bye");
            choice.SetOnClick(() => EClass.core.actionsNextFrame.Add(() => Plugin.Guard(() => PrankUi.OpenTrader(seller))));
            __instance.manager.lastTalk.AddChoice(choice);
            __instance.sequence.manager._choices.Add(choice);
            var self = new DramaChoice(Text.Self, "_bye");
            self.SetOnClick(() => EClass.core.actionsNextFrame.Add(() => Plugin.Guard(() => MoonPrank.Choose(EClass.game, seller))));
            __instance.manager.lastTalk.AddChoice(self);
            __instance.sequence.manager._choices.Add(self);
        }
    }
    [HarmonyPatch(typeof(Trait), nameof(Trait.OnBarter))]
    internal static class LoytelStock
    {
        static void Postfix(Trait __instance)
        {
            if (!(__instance is TraitLoytel)) return;
            var chest = __instance.owner.things.Find("chest_merchant");
            if (chest == null || chest.things.Any(t => t.GetBool(PrankService.BookKey))) return;
            var book = ThingGen.Create("book_roster");
            book.SetBool(PrankService.BookKey, true);
            book.SetInt(101, 1);
            chest.AddThing(book);
        }
    }
    [HarmonyPatch(typeof(Trait), nameof(Trait.SetName))]
    internal static class LedgerName
    {
        static void Postfix(Trait __instance, ref string s)
        {
            if (__instance.owner != null && __instance.owner.GetBool(PrankService.BookKey)) s = Text.Ledger;
        }
    }
    [HarmonyPatch(typeof(Chara), nameof(Chara.IsHostile), new Type[] { typeof(Chara) })]
    internal static class PrankHostility
    {
        static void Postfix(Chara __instance, Chara c, ref bool __result)
        {
            if (!PrankService.Ready || c == null) return;
            if (SelfPrank.IsPair(__instance, c)) { __result = true; return; }
            // Companions should not inherit the player's duel target through native pc.enemy handling.
            if (SelfPrank.IsDuelTarget(c) && __instance.IsPCParty && !__instance.IsPC) { __result = false; return; }
            if (SelfPrank.IsDuelTarget(__instance) && c.IsPCParty && !c.IsPC) { __result = false; return; }
            if ((__instance.GetStr(PrankService.MarkerKey) != null && PrankService.Resident(c)) ||
                (c.GetStr(PrankService.MarkerKey) != null && PrankService.Resident(__instance))) __result = true;
        }
    }
    [HarmonyPatch(typeof(Card), nameof(Card.GetPrice))]
    internal static class LedgerPrice
    {
        static bool Prefix(Card __instance, CurrencyType currency, bool sell, ref int __result)
        {
            if (!__instance.GetBool(PrankService.BookKey)) return true;
            __result = sell ? 0 : currency == CurrencyType.Money2 ? 1 : 10000;
            return false;
        }
    }
    [HarmonyPatch(typeof(TraitBookRoster), nameof(TraitBookRoster.OnUse))]
    internal static class ReadLedger
    {
        static bool Prefix(TraitBookRoster __instance, ref bool __result)
        {
            if (!__instance.owner.GetBool(PrankService.BookKey)) return true;
            Plugin.Guard(PrankUi.ShowLedger);
            __result = false;
            return false;
        }
    }
    [HarmonyPatch(typeof(Card), nameof(Card.GetSprite))]
    internal static class LedgerImage
    {
        static void Postfix(Card __instance, ref Sprite __result)
        {
            if (__instance.GetBool(PrankService.BookKey) && Plugin.LedgerSprite != null) __result = Plugin.LedgerSprite;
        }
    }
    [HarmonyPatch(typeof(TraitContainer), nameof(TraitContainer.Open))]
    internal static class PrankPackage
    {
        static bool Prefix(TraitContainer __instance)
        {
            if (__instance.owner.GetStr(PrankService.MarkerKey) == null) return true;
            Plugin.Guard(() => DeliveryEngine.Open(__instance.owner.Thing, EClass.pc));
            return false;
        }
    }
    // Old saves may still contain the previous parcel item; preserve their opening route.
    [HarmonyPatch(typeof(TraitParcel), nameof(TraitParcel.OnUse))]
    internal static class PrankParcel
    {
        static bool Prefix(TraitParcel __instance, Chara c, ref bool __result)
        {
            if (__instance.owner.GetStr(PrankService.MarkerKey) == null) return true;
            bool opened = false;
            Plugin.Guard(() => opened = DeliveryEngine.Open(__instance.owner.Thing, c));
            __result = opened;
            return false;
        }
    }
}
