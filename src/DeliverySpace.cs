using System.Linq;

namespace MailPranks
{
    internal static class DeliverySpace
    {
        internal static bool CanLand(Point p, bool placePackage = true)
        {
            // CanMoveTo is a walking edge/diagonal test relative to the actor's origin.
            // Teleports and package placement need destination collision checks only.
            if (!p.IsInBounds) return false;
            return PrankPolicy.DeliveryTile(p.IsBlocked, p.HasChara,
                placePackage && p.cell.Things.Any(t => t.IsInstalled));
        }
    }
}
