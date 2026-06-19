namespace Platform.Context;

public class TenantContext : ITenantContext
{
    public string TenantId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public string KeycloakRealm { get; set; } = string.Empty;
}
