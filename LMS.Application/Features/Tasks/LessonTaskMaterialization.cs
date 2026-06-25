namespace LMS.Application.Features.Tasks;

/// <summary>
/// Pure idempotency core for materialising lesson default-task blueprints into
/// real tasks. Kept DB-free so the "re-run creates nothing" guarantee is unit-tested.
/// </summary>
public static class LessonTaskMaterialization
{
    /// <summary>
    /// Given a lesson's blueprint Orders and the Orders already materialised under
    /// its assignment, returns the blueprint Orders still needing creation, in input
    /// order, de-duplicated. Re-running once everything exists returns empty.
    /// </summary>
    public static IReadOnlyList<int> OrdersToCreate(IEnumerable<int> blueprintOrders, ISet<int> existingOrders)
    {
        var seen = new HashSet<int>(existingOrders);
        var toCreate = new List<int>();
        foreach (var order in blueprintOrders)
            if (seen.Add(order)) toCreate.Add(order);
        return toCreate;
    }
}
