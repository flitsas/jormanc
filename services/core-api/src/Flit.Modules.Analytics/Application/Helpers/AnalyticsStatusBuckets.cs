using Flit.Modules.Analytics.Application.DTOs;

namespace Flit.Modules.Analytics.Application.Helpers;

internal static class AnalyticsStatusBuckets
{
    public static string Bucket(string status) => status switch
    {
        "draft" => "draft",
        "approved" => "approved",
        "rejected" or "cancelled" => "rejected",
        _ => "submitted"
    };

    public static StatusBreakdownDto Aggregate(IEnumerable<(string Status, int Count)> items)
    {
        var buckets = items
            .GroupBy(x => Bucket(x.Status))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

        return new StatusBreakdownDto(
            Draft: buckets.GetValueOrDefault("draft"),
            Submitted: buckets.GetValueOrDefault("submitted"),
            Approved: buckets.GetValueOrDefault("approved"),
            Rejected: buckets.GetValueOrDefault("rejected"));
    }
}
