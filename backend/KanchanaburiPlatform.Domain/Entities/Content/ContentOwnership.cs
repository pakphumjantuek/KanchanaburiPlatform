namespace KanchanaburiPlatform.Domain.Entities;

public static class ContentOwnership
{
    public static bool CanManage(Content content, string? userId, bool isAdmin, string? ownerShopUserId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;
        if (isAdmin) return true;
        return content.ContentType switch
        {
            "User" => content.CreatedByUserId == userId,
            "Shop" => content.OwnerShopId.HasValue && ownerShopUserId == userId,
            _ => false
        };
    }
}
