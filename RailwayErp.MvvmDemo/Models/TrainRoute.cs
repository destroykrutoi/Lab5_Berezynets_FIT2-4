using SQLite;

namespace RailwayErp.MvvmDemo.Models;

// Таблиця TrainRoute — маршрут поїзда
[Table("TrainRoute")]
public class TrainRoute
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [NotNull, MaxLength(100)]
    public string Origin { get; set; } = string.Empty;

    [NotNull, MaxLength(100)]
    public string Destination { get; set; } = string.Empty;

    public DateTime DepartureTime { get; set; }

    // Номер документа начальника поїзда — у БД зберігається лише в зашифрованому вигляді
    public string? ChiefDocumentNumber { get; set; }

    public DateTime UpdatedAt { get; set; }

    [Ignore]
    public string Route => $"{Origin} — {Destination}";
}

// Таблиця SyncQueue — черга відкладених змін для синхронізації
[Table("SyncQueue")]
public class SyncQueueItem
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Operation { get; set; } = string.Empty; // Insert, Update, Delete
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Attempts { get; set; }
}
