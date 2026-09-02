namespace VoltStream.Application.Commons.Security;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RequirePermissionAttribute(string permission) : Attribute
{
    public string Permission { get; } = permission;
}
