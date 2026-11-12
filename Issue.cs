using System;
using System.Text.Json.Serialization;

namespace InventoryApp;

/// <summary>
/// Одна запись журнала выдачи.
/// </summary>
public class Issue
{
    public int Id { get; set; }
    public string InventoryNumber { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string Recipient { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }

    // null = предмет ещё не вернули
    public DateTime? ReturnedDate { get; set; }

    // Статус не хранится, а вычисляется из данных записи
    [JsonIgnore]
    public string Status
    {
        get
        {
            if (ReturnedDate.HasValue) return "Возвращено";
            if (DateTime.Today > DueDate.Date) return "Просрочено";
            return "Выдано";
        }
    }
}
