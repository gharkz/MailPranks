using System;
using System.Linq;
using System.Collections.Generic;

namespace MailPranks
{
    internal static class PrankUi
    {
        internal static void OpenTrader(Chara seller)
        {
            if (!PrankService.Ready || !seller.IsAliveInCurrentZone) return;
            var list = new PrankStock { owner = seller, textHeader = Text.Buy, Game = EClass.game };
            var layer = EClass.ui.AddLayer(LayerPeople.Create(list));
            layer.windows[0].AttachCurrency().Build(new UICurrency.Options { money = true });
        }
        internal static void Destination(Game game, Chara seller, SlaverData stock, Chara creature)
        {
            if (!PrankService.Ready || EClass.game != game) return;
            var towns = EClass.game.spatials.Zones.Where(PrankService.TownAllowed).OrderBy(z => z.Name).ToList();
            Dialog.List(Text.Town, towns, z => z.Name, (index, _) =>
            {
                var town = towns[index];
                EClass.core.actionsNextFrame.Add(() => Recipient(game, seller, stock, creature, town));
                return true;
            }, canCancel: true);
        }
        private static void Recipient(Game game, Chara seller, SlaverData stock, Chara creature, Zone town)
        {
            if (!PrankService.Ready || EClass.game != game) return;
            var choices = new List<Recipient> { new Recipient { Uid = 0, Name = Text.Anyone } };
            choices.AddRange(Recipients.List(town));
            var layer = EClass.ui.AddLayer<LayerList>();
            layer.SetHeader(Text.Target);
            layer.SetList2(choices, p => p.Dead ? "<color=#C43C3C>" + p.Name + " — " + Text.Dead + "</color>" : p.Name, (selected, _) =>
            {
                if (selected.Dead) return;
                layer.Close();
                if (creature == null)
                {
                    EClass.core.actionsNextFrame.Add(() => Dialog.YesNo(Text.SelfConfirm(town.Name, selected.Name),
                        () => Plugin.Guard(() => SelfPrank.Send(game, seller, town, selected.Uid))));
                    return;
                }
                var price = PrankPolicy.Price(CalcMoney.BuySlave(creature));
                EClass.core.actionsNextFrame.Add(() => Dialog.YesNo(Text.Confirm(creature.Name, town.Name, selected.Name, price),
                    () => Plugin.Guard(() => PrankService.Purchase(game, seller, stock, creature, town, selected.Uid, selected.Name, price))));
            }, (recipient, item) => item.button1.interactable = !recipient.Dead, autoClose: false);
            layer.SetSize(620, 520);
        }
        internal static void ShowLedger()
        {
            if (!PrankService.Ready) return;
            var entries = PrankService.Read().Deliveries.AsEnumerable().Reverse().ToList();
            if (entries.Count == 0) { Dialog.Ok(Text.Empty); return; }
            Dialog.List(Text.Ledger, entries, d => $"{d.CreatureName} → {d.TownName}: {Text.Status(d.Status)}", (index, _) =>
            {
                var d = entries[index];
                EClass.core.actionsNextFrame.Add(() => Dialog.Ok($"{d.CreatureName} → {d.TownName}\n{(d.TargetUid == 0 ? Text.Anyone : d.TargetName)}\n{Text.Pick(d.Cost + " orens", d.Cost + "オレン", d.Cost + "奥伦")}\n{Text.Status(d.Status)}"));
                return true;
            }, canCancel: true);
        }
    }
    public sealed class PrankStock : BaseListPeople
    {
        internal Game Game;
        private SlaverData stock;
        public override LayerPeople.ShowMode ShowMode => LayerPeople.ShowMode.Work;
        public override bool IsDisabled(Chara c) => c.isDead;
        public override void OnCreate()
        {
            stock = PrankService.Stock(owner);
        }
        public override void OnList() { foreach (var c in stock.list) list.Add(c); }
        public override void OnInstantiate(Chara c, ItemGeneral item)
        {
            base.OnInstantiate(c, item);
            int cost = PrankPolicy.Price(CalcMoney.BuySlave(c));
            item.AddPrefab<UIItem>("costMoney").text1.SetText(cost.ToString().TagColorGoodBad(() => EClass.pc.GetCurrency() >= cost));
        }
        public override void OnClick(Chara c, ItemGeneral item)
        {
            layer.Close();
            EClass.core.actionsNextFrame.Add(() => Plugin.Guard(() => PrankUi.Destination(Game, owner, stock, c)));
        }
    }
}
