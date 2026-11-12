using System;
using System.Text.RegularExpressions;

namespace InventoryApp;

/// <summary>
/// Индивидуальный вариант: формат инвентарного номера и максимальный срок выдачи.
/// Менять только здесь.
/// </summary>
public static class Variant
{
    public const string Prefix = "LAB";   // префикс из варианта
    public const int MaxDays = 7;         // максимальный срок выдачи в днях

    public static string Example => $"{Prefix}-000";

    public static bool IsValidNumber(string number)
    {
        return Regex.IsMatch(number, $"^{Prefix}-\\d{{3}}$");
    }

    // День выдачи считается нулевым, поэтому просто прибавляем срок
    public static DateTime CalcDueDate(DateTime issueDate)
    {
        return issueDate.Date.AddDays(MaxDays);
    }
}
