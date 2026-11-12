namespace MailPranks
{
    internal static class Text
    {
        internal static string Pick(string en, string jp, string cn) => Lang.langCode == "JP" ? jp : Lang.langCode == "CN" ? cn : en;
        internal static string Title => Pick("Mail Pranks", "いたずら郵便", "恶作剧邮件");
        internal static string Ledger => Pick("Pranks ledger", "いたずら台帳", "恶作剧账簿");
        internal static string Buy => Pick("Send a mail prank", "いたずら郵便を送る", "寄送恶作剧邮件");
        internal static string Town => Pick("Choose a town", "送り先の町を選ぶ", "选择目的城镇");
        internal static string Target => Pick("Choose a recipient (optional)", "宛先の住人を選ぶ（任意）", "选择收件人（可选）");
        internal static string Anyone => Pick("Anyone in town", "町の誰か", "镇上的任何人");
        internal static string Dead => Pick("Dead", "死亡", "已死亡");
        internal static string Self => Pick("Send myself as the prank", "自分をいたずらとして送る", "把自己当作恶作剧寄出");
        internal static string SelfDestination => Pick("Where should we send you?", "自分の送り先を選ぶ", "要把你寄往哪里？");
        internal static string TownDelivery => Pick("A town and recipient", "町と宛先の住人", "城镇及收件人");
        internal static string RandomMoongate => Pick("Random moongate destination", "ランダムなムーンゲートの行き先", "随机月之门目的地");
        internal static string ListedMoongate => Pick("Choose a saved moongate destination", "保存済みのムーンゲートの行き先を選ぶ", "选择已保存的月之门目的地");
        internal static string MoonUnavailable => Pick("No available moongate destination, or moongate travel is unavailable here.", "利用できるムーンゲートの行き先がないか、ここからは移動できません。", "没有可用的月之门目的地，或当前无法使用月之门旅行。");
        internal static string MoonLoading => Pick("Sending through the moongate…", "ムーンゲートで発送中…", "正在通过月之门寄送…");
        internal static string MoonConfirm(string name) => Pick(
            $"Send yourself to {name} for free? After arrival, pop out beside a random resident. All residents there become hostile to you. No recipient selection.",
            $"無料で自分を{name}へ送りますか？ 到着後、ランダムな住人の隣の箱から飛び出します。その場所の住人全員が敵対します。宛先の住人は選べません。",
            $"免费把自己寄往{name}？抵达后从随机居民身旁的包裹中跳出。当地所有居民都会敌视你，不可选择收件人。");
        internal static string SelfConfirm(string town, string target) => Pick(
            $"Send yourself to {town} ({target}) for free? Pop out of an empty package beside the recipient and become enemies. Existing hostility, including guards reacting to your karma, still applies.",
            $"無料で自分を{town}（{target}）に送りますか？ 宛先の隣の空箱から飛び出し、お互いに敵対します。カルマによる衛兵の敵対など、既存の敵対関係は変わりません。",
            $"免费将自己寄往{town}（{target}）？从收件人身旁的空包裹里跳出来，双方互相敌对。原有敌意仍然生效，包括卫兵因你的道德值而敌视你。");
        internal static string SelfUnavailable => Pick("No living recipient or free arrival space. Self-delivery cancelled.", "生存中の宛先か転移先の空きがないため、自分の発送を中止しました。", "没有存活的收件人或可用落脚点，已取消寄送自己。");
        internal static string Confirm(string name, string town, string target, int price) => Pick(
            $"Send {name} to {town} ({target}) for {price} orens? The shopkeeper sends it directly. No refunds.",
            $"{price}オレンで{name}を{town}（{target}）に送りますか？ 店主が直接発送します。返品・返金はできません。",
            $"花费{price}奥伦将{name}寄往{town}（{target}）？店主会直接寄出，无法退款。");
        internal static string Sent => Pick("Prank sent!", "いたずら郵便を発送しました！", "恶作剧邮件已寄出！");
        internal static string Stale => Pick("The stock or destination changed. Please choose again.", "在庫か送り先が変わりました。選び直してください。", "库存或目的地已变化，请重新选择。");
        internal static string Error => Pick("Mail Pranks paused after an error. See the game log.", "エラーのためいたずら郵便を停止しました。ログを確認してください。", "恶作剧邮件因错误暂停，请查看游戏日志。");
        internal static string Empty => Pick("No pranks yet.", "まだ発送履歴はありません。", "暂无恶作剧记录。");
        internal static string Status(string status) => status == "opened" ? Pick("Opened", "開封済み", "已拆封") :
            status == "delivered" ? Pick("Waiting to be opened", "開封待ち", "等待拆封") :
            status == "waiting" ? Pick("Waiting for space beside the recipient", "宛先の住人の隣に空きができるまで待機", "等待收件人旁边有空位") :
            status == "awaiting_target" ? Pick("Waiting for the recipient to return", "宛先の住人が戻るまで待機", "等待收件人返回") :
            status == "opening" ? Pick("Opened — waiting for room to release the rest", "開封済み — 残りを出す空き待ち", "已拆封——等待空位放出剩余生物") :
            status == "missing" ? Pick("Package or creature missing", "荷物または中身が不明", "包裹或生物失踪") :
            Pick("Pending — bring the recipient into view", "配達待ち — 宛先の住人を視界に入れてください", "待配送——让收件人进入视野");
        internal static string Reaction(int index)
        {
            string[] en = { "What the hell!", "Who sent us this damn thing!", "Really bloody funny...", "Sigh, not again.", "I'm sharpening my knife, kupo.", "What is this country coming to?!" };
            string[] jp = { "なんだこりゃ！", "こんなクソみたいなもの、誰が送ってきた！", "まったく、笑わせてくれるぜ……", "はぁ、またか。", "包丁を研いでるクポ。", "この国はどうなっちまうんだ？！" };
            string[] cn = { "搞什么鬼！", "谁给我们寄了这该死的东西！", "可真他妈好笑……", "唉，怎么又来了。", "我正在磨刀，库啵。", "这个国家到底怎么了？！" };
            return Pick(en[index], jp[index], cn[index]);
        }
    }
}
