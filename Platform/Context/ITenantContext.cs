namespace Platform.Context;

public interface ITenantContext
{
    string TenantId { get; }
    string Slug { get; }
    string SubscriptionTier { get; }
    string KeycloakRealm { get; }
}
