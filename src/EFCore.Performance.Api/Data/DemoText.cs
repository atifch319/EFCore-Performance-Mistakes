namespace EFCore.Performance.Api.Data;

internal static class DemoText
{
    public static string ProductDescription(int productNumber) =>
        $"Fictional catalog description for product {productNumber:00}. " + new string('d', 320);

    public static string CustomerNotes(int customerNumber) =>
        $"Fictional account notes for customer {customerNumber:00}. " + new string('n', 320);

    public static string InternalNotes(int sequence) =>
        $"Fictional internal order notes for sequence {sequence:0000}. " + new string('i', 320);
}
