using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

/// <summary>
///     NOTE: THIS ENTIRE FILE IS AN IMP ADD.
///     THE ONLY REASON IT IS IN UPSTREAM AND NOT IMP IS SO THAT
///     IF UPSTREAM DOES PREDICT STORESYSTEM, WE CAN CARRY OVER
///     OUR METHODS WITH NO CONFLICTS
///
///     THIS IS NOT A PLACE OF HONOUR. BUT ITS BETTER THAN ME
///     COPYPASTING 500 LINES FOR VENDING MACHINES
/// </summary>
public abstract class SharedStoreSystem : EntitySystem
{
    public virtual IEnumerable<ListingDataWithCostModifiers> GetAvailableListings(
        EntityUid buyer,
        IReadOnlyCollection<ListingDataWithCostModifiers>? listings,
        HashSet<ProtoId<StoreCategoryPrototype>> categories,
        EntityUid? storeEntity = null
    )
    {
        return [];
    }
}
