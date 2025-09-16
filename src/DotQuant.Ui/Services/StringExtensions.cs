namespace DotQuant.Ui.Services;

public static class StringExtensions
{
    public static string CreateCorrelationId(this string value)
    {
        return Deterministic.Create(Deterministic.Namespaces.Commands, value).ToString();
    }
}