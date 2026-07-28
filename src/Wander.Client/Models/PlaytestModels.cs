namespace Wander.Client.Models;

public enum PlaytestZone { Library, Hand, Battlefield, Graveyard, Exile, Command }

public class GameCardInstance
{
    public Guid InstanceId { get; init; } = Guid.NewGuid();
    public Guid? CardId { get; init; }
    public Guid? PrintingId { get; init; }
    public string Name { get; init; } = "";
    public string? ManaCost { get; init; }
    public string TypeLine { get; init; } = "";
    public string? ImageUriNormal { get; init; }
    public string? ImageUriSmall { get; init; }
    public string? BackImageUriNormal { get; init; }
    public string? BackFaceTypeLine { get; init; }
    public bool IsToken { get; init; }
    public string? TokenPowerToughness { get; init; }
    public string? TokenColors { get; init; }

    public bool IsDfc => BackImageUriNormal != null;

    public PlaytestZone Zone { get; set; }
    public bool Tapped { get; set; }
    public bool SkipUntap { get; set; }
    public bool ShowingBack { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public int Counter { get; set; }
    public int Sequence { get; set; }
}

public record TokenCreateResult(string Name, string? PowerToughness, string? Colors);

public class PlaytestState
{
    public Guid DeckId { get; init; }
    public Format Format { get; init; }
    public int Life { get; set; }
    public int TurnNumber { get; set; } = 1;
    public Dictionary<string, int> ManaPool { get; } = new() { ["W"] = 0, ["U"] = 0, ["B"] = 0, ["R"] = 0, ["G"] = 0, ["C"] = 0 };
    public List<GameCardInstance> Cards { get; } = [];
    public List<Guid> LibraryOrder { get; } = [];
}
