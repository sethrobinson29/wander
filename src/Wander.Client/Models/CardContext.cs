namespace Wander.Client.Models;

public enum CardContext
{
    None,             // no context menu (default, used in Viewer without right-click)
    BuilderMain,      // deck builder — main deck cards
    BuilderCommander, // deck builder — command zone cards
    BuilderSearch,    // deck builder — search result items
    Viewer,           // deck viewer — opens Scryfall
}