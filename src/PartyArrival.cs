using System.Linq;

namespace MailPranks
{
    internal static class PartyArrival
    {
        internal static int Gather()
        {
            int moved = 0;
            foreach (var member in EClass.pc.party.members.ToArray())
            {
                // Riding/symbiosis follow their host through native SyncRide; do not detach them.
                if (member.IsPC || member.isDead || member.host != null || !member.IsAliveInCurrentZone || member.Dist(EClass.pc) <= 1) continue;
                Point spot = null;
                for (int radius = 1; radius <= 12 && spot == null; radius++)
                for (int dx = -radius; dx <= radius && spot == null; dx++)
                for (int dz = -radius; dz <= radius && spot == null; dz++)
                {
                    if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dz)) != radius) continue;
                    var p = new Point(EClass.pc.pos.x + dx, EClass.pc.pos.z + dz);
                    if (DeliverySpace.CanLand(p, placePackage: false) && EClass.pc.CanSeeLos(p)) spot = p;
                }
                if (spot == null) continue;
                member.Teleport(spot, silent: true, force: true);
                if (member.pos.Equals(spot)) moved++;
            }
            return moved;
        }
    }
}
