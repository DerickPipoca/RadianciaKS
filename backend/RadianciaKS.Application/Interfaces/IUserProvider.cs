namespace RadianciaKS.Application.Interfaces
{
    public interface IUserProvider
    {
        Guid? GetUserId();
        Guid? GetTenantId();
        string? GetUserRole();
    }
}