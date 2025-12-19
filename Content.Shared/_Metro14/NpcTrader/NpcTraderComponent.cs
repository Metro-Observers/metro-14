using System.Collections;
using System.Text;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Metro14.NpcTrader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NpcTraderComponent : Component
{
    [DataField, AutoNetworkedField, NonSerialized]
    public TimeSpan NextTick;

    [DataField, AutoNetworkedField]
    public float DeltaTime = 5.0f;

    [AutoNetworkedField]
    public Dictionary<string, (TimeSpan TimeRespawn, int CountOfItems)> RespawnItems = new();

    [DataField("catalogs"), AutoNetworkedField]
    public List<string> Catalog = new List<string>(); // в прототипе идет перечесление катологов, привязанных к этому торговцу.

    [DataField("itemsInCatalog"), AutoNetworkedField, NonSerialized]
    public Dictionary<string, int> ItemsInCatalog = new Dictionary<string, int>(); // нужно для корректного отображения предложений

    public Dictionary<string, int> CopyItemsInCatalog; // копия ItemsInCatalog для корректного восстановления продуктов у торговца

    [DataField("pathToImage")]
    public string PathToImage = "/Textures/_Metro14/Interface/NpcTrader/testTrader.png"; // тут должна быть заглушка (аватарка с темным силуэтом торговца)

    [DataField("phrasesNoProduct")]
    public List<string> PhrasesNoProduct = new List<string>() { "npc-trader-no-product" };

    [DataField("phrasesThankYou")]
    public List<string> PhrasesThankYou = new List<string>() { "npc-trader-thank-you" };

    [DataField("phrasesLittleMoney")]
    public List<string> PhrasesLittleMoney = new List<string>() { "npc-trader-little-money" };
}

[Serializable, NetSerializable]
public sealed class NpcTraderBuyMessage : BoundUserInterfaceMessage
{
    public NetEntity Buyer;
    public string ProductId;

    public NpcTraderBuyMessage(NetEntity buyer, string productId)
    {
        Buyer = buyer;
        ProductId = productId;
    }
}

[NetSerializable, Serializable]
public enum NpcTraderUiKey
{
    Key,
}
