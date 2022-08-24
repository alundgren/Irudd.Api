// ReSharper disable once CheckNamespace
namespace Irudd.Api;

public static class ObjectExtensions
{
    public static T Require<T>(this T? value, string exceptionMessageOnNull) where T : class
    {
        if (value == null)
            throw new ArgumentException(exceptionMessageOnNull, nameof(value));
            
        return value;
    }
}