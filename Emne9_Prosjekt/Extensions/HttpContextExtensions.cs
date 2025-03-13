namespace Emne9_Prosjekt.Extensions;

public static class HttpContextExtensions
{
    public static T GetValueFromContext<T>(
        this HttpContext context, 
        string key, 
        T defaultValue, 
        ILogger ?logger)
    {
        try
        {
            if ((!context.Items.TryGetValue(key, out var item)))
                return defaultValue;

            if (item is T value)
                return value;

            logger?.LogWarning($"Fant value i HttpContext men ikke av riktig type {typeof(T)}. Key: {key}");
        }
        catch (Exception ex)
        {
            logger?.LogError($"Klarte ikke å hente ut verdier fra HTTPContext: {key}", ex);
        }

        return defaultValue;
    }
}