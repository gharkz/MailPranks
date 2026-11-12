using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MailPranks
{
    internal sealed class Recipient
    {
        internal int Uid;
        internal string Name;
        internal bool Dead;
    }

    internal static class Recipients
    {
        // Inspect saved metadata without constructing offscreen characters or their renderers.
        internal static List<Recipient> List(Zone town)
        {
            var result = new Dictionary<int, Recipient>();
            if (town == EClass._zone)
            {
                foreach (var c in EClass._map.charas.Concat(EClass._map.deadCharas)) Add(result, c);
            }
            else
            {
                string path = town.pathSave + "map";
                if (File.Exists(path))
                {
                    var root = JObject.Parse(IO.IsCompressed(path) ? IO.Decompress(path) : File.ReadAllText(path));
                    var refs = root.DescendantsAndSelf().OfType<JObject>().Where(o => o["$id"] != null)
                        .ToDictionary(o => (string)o["$id"]);
                    foreach (string list in new[] { "serializedCharas", "deadCharas" })
                    foreach (var entry in Items(root[list]))
                    {
                        var token = Resolve(entry, refs);
                        if (token == null) continue;
                        var ints = Items(token["A"]).ToArray();
                        if (ints.Length < 2) continue;
                        int uid = (int)ints[1];
                        if (uid <= 0 || uid == EClass.pc.uid) continue;
                        string id = (string)token["B"];
                        var strings = Resolve(token["Z"], refs);
                        if (strings?[PrankService.MarkerKey.ToString()] != null) continue;
                        var bits = Items(token["9"]).FirstOrDefault();
                        bool dead = list == "deadCharas" || (bits != null && ((int)bits & 1) != 0);
                        string name;
                        if (!town.dictCitizen.TryGetValue(uid, out name))
                            name = (string)strings?["1"] ?? (id != null && EClass.sources.charas.map.ContainsKey(id) ? EClass.sources.charas.map[id].GetName() : id);
                        result[uid] = new Recipient { Uid = uid, Name = name ?? uid.ToString(), Dead = dead };
                    }
                }
            }
            foreach (var c in EClass.game.cards.globalCharas.Values)
            {
                if (c.currentZone == town || (c.currentZone == null && c.homeZone == town)) Add(result, c);
                else result.Remove(c.uid); // A global NPC's live location/death state beats an old map snapshot.
            }
            return result.Values.OrderBy(r => r.Name).ThenBy(r => r.Uid).ToList();
        }
        private static void Add(Dictionary<int, Recipient> result, Chara c)
        {
            if (c.IsPC || c.IsPCParty || c.GetStr(PrankService.MarkerKey) != null) { result.Remove(c.uid); return; }
            result[c.uid] = new Recipient { Uid = c.uid, Name = c.Name, Dead = c.isDead };
        }
        private static IEnumerable<JToken> Items(JToken t) => t is JArray array ? array : t?["$values"] as JArray ?? Enumerable.Empty<JToken>();
        private static JObject Resolve(JToken t, Dictionary<string, JObject> refs)
        {
            var obj = t as JObject;
            if (obj?["$ref"] != null) return refs.TryGetValue((string)obj["$ref"], out var found) ? found : null;
            return obj;
        }
    }
}
