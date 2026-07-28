using Wander.Client.Models;

namespace Wander.Client.Services;

public class PlaytestStateService
{
    static readonly Random Rng = new();

    DeckDetail? _sourceDeck;
    int _sequence;

    public PlaytestState? State { get; private set; }

    public event Action? OnChange;
    void Notify() => OnChange?.Invoke();

    public void Load(DeckDetail deck)
    {
        _sourceDeck = deck;
        State = BuildFreshState(deck);
        Notify();
    }

    public void Reset()
    {
        if (_sourceDeck == null) return;
        State = BuildFreshState(_sourceDeck);
        Notify();
    }

    PlaytestState BuildFreshState(DeckDetail deck)
    {
        _sequence = 0;
        var state = new PlaytestState
        {
            DeckId = deck.Id,
            Format = deck.Format,
            Life = deck.Format == Format.Commander ? 40 : 20
        };

        foreach (var dc in deck.Cards.Where(c => !c.IsSideboard))
        {
            var zone = dc.IsCommander ? PlaytestZone.Command : PlaytestZone.Library;
            for (var i = 0; i < dc.Quantity; i++)
            {
                var card = FromDeckCard(dc, zone);
                Touch(card);
                state.Cards.Add(card);
            }
        }

        state.LibraryOrder.AddRange(state.Cards.Where(c => c.Zone == PlaytestZone.Library).Select(c => c.InstanceId));
        ShuffleList(state.LibraryOrder);

        for (var i = 0; i < 7 && state.LibraryOrder.Count > 0; i++)
            DrawInternal(state);

        return state;
    }

    void Touch(GameCardInstance card) => card.Sequence = ++_sequence;

    static GameCardInstance FromDeckCard(DeckCardDetail dc, PlaytestZone zone) => new()
    {
        CardId = dc.CardId,
        PrintingId = dc.PrintingId ?? dc.ImagePrintingId,
        Name = dc.CardName,
        ManaCost = dc.ManaCost,
        TypeLine = dc.TypeLine,
        ImageUriNormal = dc.ImageUriNormal,
        ImageUriSmall = dc.ImageUriSmall,
        BackImageUriNormal = dc.BackImageUriNormal,
        BackFaceTypeLine = dc.BackFaceTypeLine,
        Zone = zone
    };

    static void ShuffleList(List<Guid> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void DrawInternal(PlaytestState state)
    {
        if (state.LibraryOrder.Count == 0) return;
        var id = state.LibraryOrder[0];
        state.LibraryOrder.RemoveAt(0);
        var card = state.Cards.First(c => c.InstanceId == id);
        card.Zone = PlaytestZone.Hand;
        Touch(card);
    }

    public void Shuffle()
    {
        if (State == null) return;
        ShuffleList(State.LibraryOrder);
        Notify();
    }

    public void DrawCard()
    {
        if (State == null) return;
        DrawInternal(State);
        Notify();
    }

    public void NextTurn()
    {
        if (State == null) return;
        foreach (var card in State.Cards.Where(c => c.Tapped && !c.SkipUntap))
            card.Tapped = false;
        DrawInternal(State);
        State.TurnNumber++;
        Notify();
    }

    public void MoveCard(Guid instanceId, PlaytestZone toZone)
    {
        if (State == null) return;
        var card = State.Cards.FirstOrDefault(c => c.InstanceId == instanceId);
        if (card == null || card.Zone == toZone) return;

        if (card.Zone == PlaytestZone.Library) State.LibraryOrder.Remove(instanceId);

        card.Zone = toZone;
        card.Tapped = false;
        if (toZone != PlaytestZone.Battlefield) { card.PositionX = null; card.PositionY = null; }
        if (toZone == PlaytestZone.Library) State.LibraryOrder.Insert(0, instanceId);
        Touch(card);

        Notify();
    }

    public void SetBattlefieldPosition(Guid instanceId, double x, double y)
    {
        if (State == null) return;
        var card = State.Cards.FirstOrDefault(c => c.InstanceId == instanceId);
        if (card == null) return;

        if (card.Zone != PlaytestZone.Battlefield)
        {
            if (card.Zone == PlaytestZone.Library) State.LibraryOrder.Remove(instanceId);
            card.Zone = PlaytestZone.Battlefield;
            card.Tapped = false;
            Touch(card);
        }
        card.PositionX = x;
        card.PositionY = y;
        Notify();
    }

    public void ToggleTap(Guid instanceId)
    {
        var card = State?.Cards.FirstOrDefault(c => c.InstanceId == instanceId);
        if (card == null) return;
        card.Tapped = !card.Tapped;
        Notify();
    }

    public void ToggleSkipUntap(Guid instanceId)
    {
        var card = State?.Cards.FirstOrDefault(c => c.InstanceId == instanceId);
        if (card == null) return;
        card.SkipUntap = !card.SkipUntap;
        Notify();
    }

    public void Flip(Guid instanceId)
    {
        var card = State?.Cards.FirstOrDefault(c => c.InstanceId == instanceId);
        if (card == null) return;
        card.ShowingBack = !card.ShowingBack;
        Notify();
    }

    public void AdjustCounter(Guid instanceId, int delta)
    {
        var card = State?.Cards.FirstOrDefault(c => c.InstanceId == instanceId);
        if (card == null) return;
        card.Counter += delta;
        Notify();
    }

    public void AdjustLife(int delta)
    {
        if (State == null) return;
        State.Life += delta;
        Notify();
    }

    public void AdjustMana(string color, int delta)
    {
        if (State == null || !State.ManaPool.ContainsKey(color)) return;
        State.ManaPool[color] = Math.Max(0, State.ManaPool[color] + delta);
        Notify();
    }

    public void ResetMana()
    {
        if (State == null) return;
        foreach (var key in State.ManaPool.Keys.ToList())
            State.ManaPool[key] = 0;
        Notify();
    }

    public void CreateToken(string name, string? powerToughness, string? colors)
    {
        if (State == null) return;
        var card = new GameCardInstance
        {
            Name = name,
            TypeLine = "Token",
            IsToken = true,
            TokenPowerToughness = powerToughness,
            TokenColors = colors,
            Zone = PlaytestZone.Battlefield,
            PositionX = 90,
            PositionY = 110
        };
        Touch(card);
        State.Cards.Add(card);
        Notify();
    }
}
