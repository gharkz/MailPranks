using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace MailPranks
{
    internal static class MoonPrank
    {
        private static Game runningGame;
        private static string runningToken;
        internal static bool IsRunning(Game game, string token) => runningGame == game && token != null && token == runningToken;

        internal static void Choose(Game game, Chara seller)
        {
            if (!Valid(game, seller)) return;
            var choices = new[] { Text.TownDelivery, Text.RandomMoongate, Text.ListedMoongate };
            Dialog.List(Text.SelfDestination, choices, s => s, (index, _) =>
            {
                EClass.core.actionsNextFrame.Add(() => Plugin.Guard(() =>
                {
                    if (!Valid(game, seller)) return;
                    if (index == 0) PrankUi.Destination(game, seller, null, null);
                    else if (index == 1) Confirm(game, seller, null);
                    else List(game, seller);
                }));
                return true;
            }, canCancel: true);
        }

        private static bool Valid(Game game, Chara seller) => PrankService.Ready && EClass.game == game && seller.IsAliveInCurrentZone;
        private static bool CanTravel() => !EClass.core.version.demo && !EClass._zone.IsInstance &&
            EClass._zone.dateExpire == 0 && !EClass._zone.IsRegion && EClass.pc.burden.GetPhase() != 4;

        private static void List(Game game, Chara seller)
        {
            var maps = new TraitMoongate().ListSavedUserMap()
                .Where(m => !(EClass.core.config.net.noAdult && m.tag.HasTag("adult")))
                .OrderByDescending(m => EClass.player.favMoongate.Contains(m.id)).ThenBy(m => m.name).ToList();
            if (maps.Count == 0) { Dialog.Ok(Text.MoonUnavailable); return; }
            var layer = EClass.ui.AddLayer<LayerList>();
            layer.SetHeader(Text.ListedMoongate);
            layer.SetList2(maps, m => m.name, (m, _) =>
                EClass.core.actionsNextFrame.Add(() => Plugin.Guard(() => Confirm(game, seller, m))), null);
            layer.SetSize(620, 520);
        }

        private static void Confirm(Game game, Chara seller, MapMetaData map)
        {
            if (!Valid(game, seller)) return;
            if (!CanTravel()) { Dialog.Ok(Text.MoonUnavailable); return; }
            Dialog.YesNo(Text.MoonConfirm(map == null ? Text.RandomMoongate : map.name),
                () => Plugin.Guard(() => Start(game, seller, map)));
        }

        private static void Start(Game game, Chara seller, MapMetaData map)
        {
            if (!Valid(game, seller) || !CanTravel() || EClass.pc.GetStr(SelfPrank.PendingKey) != null)
            { Dialog.Ok(Text.MoonUnavailable); return; }
            if (map != null && (!map.IsValidVersion() || !System.IO.File.Exists(map.path) ||
                (EClass.core.config.net.noAdult && map.tag.HasTag("adult"))))
            { Dialog.Ok(Text.MoonUnavailable); return; }
            var request = new SelfPrank.Request { Moongate = true, Token = Guid.NewGuid().ToString("N") };
            int origin = EClass._zone.uid;
            runningGame = game;
            runningToken = request.Token;
            EClass.pc.SetStr(SelfPrank.PendingKey, JsonConvert.SerializeObject(request));
            // Native random selection/download and saved-map loading own the transition.
            LayerProgress.StartAsync(Text.MoonLoading, Resolve(game, request, origin, map));
        }

        private static async UniTask<bool> Resolve(Game game, SelfPrank.Request request, int origin, MapMetaData map)
        {
            bool ok = false;
            try
            {
                var gate = new TraitMoongate();
                if (map == null) ok = await gate.UseMoongate();
                else { gate.LoadMap(map); ok = true; }
                // Resolve the destination now; residents and positions are selected only after entry.
                if (!PrankService.Ready || EClass.game != game || !IsRunning(game, request.Token)) return false;
                var destination = EClass.player.nextZone;
                if (destination == null || destination.uid == origin) destination = EClass._zone;
                ok = ok && destination.uid != origin && destination.instance is ZoneInsstanceMoongate;
                if (ok)
                {
                    request.TownUid = destination.uid;
                    request.Resolved = true;
                    EClass.pc.SetStr(SelfPrank.PendingKey, JsonConvert.SerializeObject(request));
                }
                else EClass.pc.SetStr(SelfPrank.PendingKey, null);
            }
            catch (Exception ex)
            {
                ok = false;
                Plugin.Instance.ReportMoonError(ex);
                if (PrankService.Ready && EClass.game == game) EClass.pc.SetStr(SelfPrank.PendingKey, null);
            }
            finally
            {
                if (IsRunning(game, request.Token)) { runningGame = null; runningToken = null; }
            }
            if (!ok && PrankService.Ready && EClass.game == game)
                EClass.core.actionsNextFrame.Add(() => { if (PrankService.Ready && EClass.game == game) Dialog.Ok(Text.MoonUnavailable); });
            return ok;
        }
    }
}
