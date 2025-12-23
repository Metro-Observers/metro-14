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

    /// <summary>
    /// Время между обновлением проверки торговца на предмет наличия "неполных" предложений, которые необходимо восполнить.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DeltaTime = 5.0f;

    /// <summary>
    /// Список предложений, что нужно восполнить, времени, в которое их нужно добавить, и добавляемого количества.
    /// </summary>
    [AutoNetworkedField]
    public Dictionary<string, (TimeSpan TimeRespawn, int CountOfItems)> RespawnItems = new();

    /// <summary>
    /// Каталог торговца.
    /// В прототипе идет перечесление катологов, привязанных к этому торговцу.
    /// </summary>
    [DataField("catalogs"), AutoNetworkedField]
    public List<string> Catalog = new List<string>();

    /// <summary>
    /// Список предметов из каталогов торговца.
    /// Необходимо для корректного отображения предложений.
    /// </summary>
    [DataField("itemsInCatalog"), AutoNetworkedField, NonSerialized]
    public Dictionary<string, int> ItemsInCatalog = new Dictionary<string, int>();

    /// <summary>
    /// "Идеальный" список всех предметов из каталогов торговца, который содержит изначальные значения восполняемых предметов.
    /// Де факто копия ItemsInCatalog для корректного восстановления продуктов у торговца.
    /// </summary>
    public Dictionary<string, int> CopyItemsInCatalog;

    /// <summary>
    /// Путь к изображению торговца, которое будет показываться игроку во время диалога.
    /// </summary>
    [DataField("pathToImage")]
    public string PathToImage = "/Textures/_Metro14/Interface/NpcTrader/testTrader.png"; // тут должна быть заглушка (аватарка с темным силуэтом торговца)

    /// <summary>
    /// Фраза которую будет произносить торговец, если игрок попытается купить товар, которого нет в наличии.
    /// </summary>
    [DataField("phrasesNoProduct")]
    public List<string> PhrasesNoProduct = new List<string>() { "npc-trader-no-product" };

    /// <summary>
    /// Фраза которую будет произносить торговец, если игрок купит товар.
    /// </summary>
    [DataField("phrasesThankYou")]
    public List<string> PhrasesThankYou = new List<string>() { "npc-trader-thank-you" };

    /// <summary>
    /// Фраза которую будет произносить торговец, если игрок попытается купить товар, не имя должной оплаты.
    /// </summary>
    [DataField("phrasesLittleMoney")]
    public List<string> PhrasesLittleMoney = new List<string>() { "npc-trader-little-money" };

    // перенос серверных вещей в компонент...

    /// <summary>
    /// список предметов, котоыре находятся на земле рядом с торговцем.
    /// </summary>
    public HashSet<EntityUid> EntitiesInRange = new();

    /// <summary>
    /// список предметов, котоыре нужно будет удалить при заключении сделки.
    /// </summary>
    public List<EntityUid> DelItem = new List<EntityUid>();

    /// <summary>
    /// словарь, содержащий информацию о патронах, которые пойдут как оплата при заключении сделки и хранилищах, содержащих их.
    /// </summary>
    public Dictionary<EntityUid, EntityUid> DelRealAmmo = new Dictionary<EntityUid, EntityUid>();

    /// <summary>
    /// словарь, содержащий информацию о виртуальных патронах (еще не были заспавнены),
    /// которые пойдут как оплата при заключении сделки и хранилищах, содержащих их.
    /// </summary>
    public Dictionary<EntityUid, int> DelVirtAmmo = new Dictionary<EntityUid, int>();
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
