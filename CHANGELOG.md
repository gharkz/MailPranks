# Changelog

## 0.2.7

Mail Pranks 0.2.7
The Pranks ledger now shows only the latest 30 entries, newest first, in a fixed-size scrollable window. Older completed records and unused closed-package records are pruned. Pending deliveries and partially opened packages remain intact. NPC recipient selection is unchanged.

日本語
いたずら台帳を固定サイズのスクロール画面に変更し、最新30件を新しい順に表示。古い完了済み履歴と不要になった終了済み小包の記録を整理します。配達待ちや中身を出し終えていない小包は保持。NPCの宛先選択は変更ありません。

简体中文
恶作剧账簿改为固定大小的可滚动窗口，仅显示最近30条记录，新记录在前。清理旧的已完成记录及不再使用的已结束包裹记录。待配送任务和尚未放完内容的包裹保持不变。NPC收件人选择不变。


## 0.2.6

Mail Pranks 0.2.6
Fixed the Pranks ledger disappearing into Loytel's ordinary roster-ledger stack. It now stays separate in shop stock and when moved or sorted in inventories. Reopening Loytel's shop supplies the missing ledger without waiting for restock. Price remains one gold bar. Fixed the custom icon scale to fit a normal inventory slot.

日本語
いたずら台帳がロイテルの通常の名簿と重なって消える問題を修正。店の在庫やアイテムの移動・整理でも別々に保たれます。再入荷を待たず、店を開き直すと不足した台帳が追加されます。価格は金塊1個のままです。専用アイコンが通常のアイテム枠に収まるよう表示倍率も修正。

简体中文
修复恶作剧账簿与罗伊特尔的普通名册堆叠后消失的问题。商店库存及物品移动、整理时，两者现在保持分离。重新打开商店即可补上缺失的账簿，无需等待补货。价格仍为1根金条。修正专用图标的缩放，使其适合普通物品格。


## 0.2.5

Mail Pranks 0.2.5
Fixed false "no delivery space" failures: self-delivery and party arrival now check the destination tile, not walking paths from the arrival gate. Loose dropped items no longer block mail placement. Walls, characters, and conflicting installed furniture still block package placement.
Added five tightly cropped English UI screenshots covering trader choices, slave and animal stock, recipients, and self-delivery destinations.

日本語
「配達先に空きがない」と誤判定する問題を修正。自分と仲間の到着は、ゲートからの歩行経路ではなく着地点を確認します。地面に落ちたアイテムは郵便の設置を妨げません。壁・キャラクター・設置済み家具との重なりは引き続き回避します。
商人の選択肢、奴隷・動物の在庫、宛先、自分の発送先を紹介する英語UIのスクリーンショット5枚を追加。必要な画面部分のみ掲載。

简体中文
修复错误的“没有配送空间”提示：寄送自己和队友抵达时检查目标格，而非从入口出发的行走路径。地上的掉落物不再阻止放置邮件包裹，仍会避开墙壁、角色及冲突的已安装家具。
新增5张裁剪后的英语界面截图，展示商人选项、奴隶和动物库存、收件人及寄送自己的目的地。


## 0.2.4

Mail Pranks 0.2.4
Self-delivery targets now show red names and hostile actions immediately. Incognito and native hostility resets clear prank duels, and moongate residents recover their original hostility. Ordinary bystanders and companions stay outside town duels.

日本語
自分を発送した相手が即座に赤い名前と敵対行動を示すよう修正。変装と通常の敵対解除でいたずらの決闘を解除し、ムーンゲートの住人は元の敵対状態に戻ります。町の決闘には他の住人や仲間は巻き込まれません。

简体中文
修复寄送自己后目标立即显示红名及敌对操作。隐姓埋名和原生敌意重置会解除恶作剧决斗，月之门居民恢复原本的敌意状态。城镇决斗不会牵连旁观者和队友。


## 0.2.3

First public release: independent four-day prank stock, bundled mail delivery, recipient selection, per-package affinity penalties, and self-delivery to towns or moongates. Party members arrive nearby; mail containers are installed properly. English, Japanese and Simplified Chinese UI.

初公開：4日ごとの専用在庫、まとめて配達、宛先選択、箱ごとの好感度低下、町やムーンゲートへの自分の発送。仲間も近くに到着し、郵便コンテナを正しく設置。英語・日本語・簡体字中国語UI対応。

首次公开发布：独立库存每4天补货、合并配送、收件人选择、每箱好感度惩罚，以及寄送自己到城镇或月之门。队友一起抵达，邮件容器正确放置。支持英语、日语和简体中文界面。
