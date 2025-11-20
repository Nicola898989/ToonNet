#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    // Polyfill per consentire l'uso di record e init-only in netstandard2.0
    internal static class IsExternalInit
    {
    }
}
#endif
