namespace OAuth2.Repositories;

public enum RefreshTokenRotationStatus
{
    Succeeded,
    InvalidGrant,
    InvalidScope
}
