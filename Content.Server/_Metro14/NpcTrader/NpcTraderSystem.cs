using Content.Server.Advertise.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared._Metro14.NpcTrader;
using Content.Shared.Advertise.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Arcade;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Power;
using Content.Shared.Storage;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.ActionBlocker;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged;

namespace Content.Server._Metro14.NpcTrader;

/// <summary>
/// Класс, содержащий логику обработки процесса обмена между игроков и торговцем.
/// </summary>
public sealed class NpcTraderSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _handSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

    private static readonly Random _random = new Random();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NpcTraderComponent, MapInitEvent>(OnMapInit);
        Subs.BuiEvents<NpcTraderComponent>(NpcTraderUiKey.Key, subs =>
        {
            subs.Event<NpcTraderBuyMessage>(OnNpcTraderBuy);
        });
    }

    /// <summary>
    /// Здесь содержится логика восполнения вещей в каталоге торговца.
    /// </summary>
    /// <param name="frameTime"></param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var npcTraders = EntityQueryEnumerator<NpcTraderComponent>();
        while (npcTraders.MoveNext(out var uid, out var component))
        {
            if (_gameTiming.CurTime > component.NextTick)
            {
                component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(component.DeltaTime);

                if (component.CopyItemsInCatalog == null || component.CopyItemsInCatalog.Count == 0)
                    continue;

                // не думаю, что от этих комментариев будет польза, но тут проверяется вообще возможность данного торговца "респавнить" товары
                if (component.RespawnItems.Count != null && component.RespawnItems.Count != 0)
                {
                    foreach (var itemForRespawn in component.RespawnItems)
                    {
                        if (_gameTiming.CurTime > itemForRespawn.Value.TimeRespawn)
                        {
                            if (itemForRespawn.Value.CountOfItems == -1)
                            {
                                component.ItemsInCatalog[itemForRespawn.Key] = component.CopyItemsInCatalog[itemForRespawn.Key];
                                Dirty(uid, component);
                            }
                            else
                            {
                                if (component.ItemsInCatalog[itemForRespawn.Key] + itemForRespawn.Value.CountOfItems <= component.CopyItemsInCatalog[itemForRespawn.Key])
                                {
                                    component.ItemsInCatalog[itemForRespawn.Key] += itemForRespawn.Value.CountOfItems;
                                    Dirty(uid, component);
                                }    
                                else
                                {
                                    component.ItemsInCatalog[itemForRespawn.Key] = component.CopyItemsInCatalog[itemForRespawn.Key];
                                    Dirty(uid, component);
                                }
                            }
                        }
                    }
                }

                // по факту в тупую перебираем предложения торговца
                foreach (var item in component.ItemsInCatalog)
                {
                    if (!_prototype.TryIndex<NpcTraderItemForCatalogPrototype>(item.Key, out var product))
                        continue;

                    // оставляем только те, которые можно восстановить
                    if (!product.CanRespawn)
                        continue;

                    // если текущее значение меньше изначального, то записываем в очередь на восстановление :)
                    if (component.ItemsInCatalog[item.Key] != component.CopyItemsInCatalog[item.Key])
                    {
                        if (!component.RespawnItems.ContainsKey(item.Key))
                            component.RespawnItems.Add(item.Key, (
                                TimeRespawn: _gameTiming.CurTime + TimeSpan.FromSeconds(product.TimeRespawn),
                                CountOfItems: product.CountRespawn
                            ));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Стандартный метод инициализации компонента.
    /// </summary>
    /// <param name="uid"> торговец </param>
    /// <param name="npcTraderComponent"> компонент торговца </param>
    /// <param name="args"></param>
    private void OnMapInit(EntityUid uid, NpcTraderComponent npcTraderComponent, MapInitEvent args)
    {
        // проверяем, что к нему привязаны хоть какие-то каталоги
        if (npcTraderComponent.ItemsInCatalog.Count <= 0)
        {
            // дальше перебираем все привязанные каталоги
            foreach (var nameOfCatalog in npcTraderComponent.Catalog)
            {
                // проверяем, что указанные каталоги корректны
                if (!_prototype.TryIndex<NpcTraderSalesCatalogPrototype>(nameOfCatalog, out var typeOfCatalog))
                    continue;

                // перебираем все предложения в данном каталоге 
                foreach (var nameOfItem in typeOfCatalog.Catalog)
                {
                    // проверяем, что указанные предложения существуют
                    if (!_prototype.TryIndex<NpcTraderItemForCatalogPrototype>(nameOfItem.Key, out var prototypeItemOfCatalog))
                        continue;

                    // помещаем в словарь компонента торговца все незаписанные раннее предложения 
                    if (!npcTraderComponent.ItemsInCatalog.ContainsKey(prototypeItemOfCatalog.ID))
                        npcTraderComponent.ItemsInCatalog.Add(prototypeItemOfCatalog.ID, nameOfItem.Value);
                }
            }
        }

        npcTraderComponent.CopyItemsInCatalog = new Dictionary<string, int>(npcTraderComponent.ItemsInCatalog); // создаем изначальный "слепок" предложений
        npcTraderComponent.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(npcTraderComponent.DeltaTime);
        Dirty(uid, npcTraderComponent);
    }


    /// <summary>
    /// Метод, вызывающийся при нажатии кнопки "купить" на клиентской части.
    /// </summary>
    /// <param name="uid"> торговец </param>
    /// <param name="component"> компонент торговца </param>
    /// <param name="args"> параметры сообщения | args.Buyer - NetEntity покупателя | args.ProductId - ID продукта </param>
    public void OnNpcTraderBuy(EntityUid uid, NpcTraderComponent component, NpcTraderBuyMessage args)
    {
        component.DelItem.Clear();
        component.DelRealAmmo.Clear();
        component.DelVirtAmmo.Clear();

        // проверяем, что получили корректный ID предложения
        if (!component.ItemsInCatalog.ContainsKey(args.ProductId))
            return;

        // елси данное предложение не является бесконечным, то нужно проверить, не закончился ли товар
        if (component.ItemsInCatalog[args.ProductId] != -1)
        {
            if (component.ItemsInCatalog[args.ProductId] == 0)
            {
                TrySayPhrase(uid, component.PhrasesNoProduct[_random.Next(component.PhrasesNoProduct.Count)]);
                return;
            }
        }

        if (!_prototype.TryIndex<NpcTraderItemForCatalogPrototype>(args.ProductId, out var npcTraderItemForCatalogProto))
            return;

        bool tempFlag = false;
        foreach (var givingItem in npcTraderItemForCatalogProto.GivingItems)
        {
            if (!_prototype.TryIndex<EntityPrototype>(givingItem.Key, out var tempProto))
                continue;

            if (givingItem.Value > 0)
            {
                for (int i = 0; i < givingItem.Value; i++)
                {
                    if (!TryFindItem(uid, component, givingItem.Key, _entityManager.GetEntity(args.Buyer)))
                    {
                        tempFlag = true;
                        break;
                    }
                }
            }
        }

        if (!tempFlag)
        {
            TryDeleteItems(_entityManager.GetEntity(args.Buyer), component);
            TryGiveItems(uid, args.ProductId, _entityManager.GetEntity(args.Buyer));
            tempFlag = false;
        }
        else
        {
            TrySayPhrase(uid, component.PhrasesLittleMoney[_random.Next(component.PhrasesLittleMoney.Count)]);
        }
    }

    /// <summary>
    /// Метод, который позволяет торговцам "сказать" что-то игрокам.
    /// </summary>
    /// <param name="npcTrader"> от лица какой сущности будет произнесена фраза </param>
    /// <param name="text"> собственно фраза </param>
    private void TrySayPhrase(EntityUid npcTrader, string text)
    {
        var chatSystem = _entityManager.EntitySysManager.GetEntitySystem<ChatSystem>();

        chatSystem.TrySendInGameICMessage(
            npcTrader,
            Loc.GetString(text),
            InGameICChatType.Speak,
            hideChat: false,  // возможность скрыть из чата  
            hideLog: false    // возможность скрыть из логов  
        );
    }

    /// <summary>
    /// Метод, который "изымает" предметы в счет оплаты товара.
    /// </summary>
    private void TryDeleteItems(EntityUid player, NpcTraderComponent component)
    {
        foreach (var virtBullet in component.DelVirtAmmo)
        {
            if (!_entityManager.TryGetComponent(virtBullet.Key, out BallisticAmmoProviderComponent? ballisticProviderComponent))
                continue;

            for (int i = 0; i < virtBullet.Value; i++)
            {
                if (ballisticProviderComponent.UnspawnedCount == 0)
                    break;

                var ammo = new List<(EntityUid? Entity, IShootable Shootable)>();

                var ev = new TakeAmmoEvent(1, ammo, Transform(virtBullet.Key).Coordinates, player);
                RaiseLocalEvent(virtBullet.Key, ev);

                foreach (var (entity, _) in ammo)
                {
                    if (entity != null)
                    {
                        QueueDel(entity.Value);
                    }
                }
            }
        }

        foreach (var realBullet in component.DelRealAmmo)
        {
            var ammo = new List<(EntityUid? Entity, IShootable Shootable)>();

            var ev = new TakeAmmoEvent(1, ammo, Transform(realBullet.Value).Coordinates, player);
            RaiseLocalEvent(realBullet.Value, ev);

            foreach (var (entity, _) in ammo)
            {
                if (entity != null)
                {
                    QueueDel(entity.Value);
                }
            }                    
        }

        foreach (var item in component.DelItem)
        {
            QueueDel(item);
        }
    }

    /// <summary>
    /// Метод, который пытается выдать предметы игроку после оплаты.
    /// </summary>
    /// <param name="npcUid"> торговец </param>
    /// <param name="productId"> UID предложения торговца </param>
    /// <param name="playerUid"> игрок </param>
    private void TryGiveItems(EntityUid npcUid, string productId, EntityUid playerUid)
    {
        if (!_prototype.TryIndex<NpcTraderItemForCatalogPrototype>(productId, out var tradeItemComp))
            return;

        if (tradeItemComp.TakingItems == null || tradeItemComp.TakingItems.Count == 0)
            return;

        foreach(var itemId in tradeItemComp.TakingItems)
        {
            if (itemId.Value > 0)
                for (int i = 0; i < itemId.Value; i++)
                    SpawnItemOnEntityValidated(playerUid, itemId.Key);
        }

        if (_entityManager.TryGetComponent(npcUid, out NpcTraderComponent? npcTraderComponent))
        {
            if (npcTraderComponent.ItemsInCatalog.ContainsKey(productId) && npcTraderComponent.ItemsInCatalog[productId] != -1)
            {
                npcTraderComponent.ItemsInCatalog[productId] -= 1;
                Dirty(npcUid, npcTraderComponent);
            }

            TrySayPhrase(npcUid, npcTraderComponent.PhrasesThankYou[_random.Next(npcTraderComponent.PhrasesThankYou.Count)]);
        }

        _adminLogger.Add(LogType.Action, LogImpact.Low, $"Игрок {playerUid} купил '{productId}'");
    }

    /// <summary>  
    /// Спавнит предмет и пытается разместить его в инвентаре с соблюдением всех проверок.  
    /// В отличие от оригинального метода, не обходит проверки через контейнеры.  
    /// </summary>  
    public bool SpawnItemOnEntityValidated(EntityUid uid, string prototype, InventoryComponent? inventory = null)
    {
        if (!Resolve(uid, ref inventory, false))
            return false;

        if (Deleted(uid))
            return false;

        if (!_prototype.HasIndex<EntityPrototype>(prototype))
            return false;

        var item = Spawn(prototype, Transform(uid).Coordinates);

        bool DeleteItem()
        {
            Del(item);
            return false;
        }

        // Получаем все слоты инвентаря  
        if (!_inventory.TryGetSlots(uid, out var slotDefinitions))
            return DeleteItem();

        // Пробуем разместить в каждом подходящем слоте  
        foreach (var slotDef in slotDefinitions)
        {
            // Проверяем можно ли экипировать предмет в этот слот  
            if (!_inventory.CanEquip(uid, item, slotDef.Name, out var reason, slotDef, inventory))
                continue;

            // Проверяем что слот пуст  
            if (_inventory.TryGetSlotEntity(uid, slotDef.Name, out _, inventory))
                continue;

            // Пытаемся экипировать с соблюдением всех проверок  
            if (_inventory.TryEquip(uid, item, slotDef.Name, silent: true, force: false, inventory: inventory))
                return true;
        }

        // Если не удалось разместить в инвентарь, пробуем взять в руки  
        if (TryComp<HandsComponent>(uid, out var hands))
        {
            return _handSystem.TryPickup(uid, item, handsComp: hands);
        }

        return DeleteItem();
    }

    /// <summary>
    /// Метод, который проверяет, можно ли из обоймы/коробки патрон вытащить пулю для покупки товара.
    /// </summary>
    /// <param name="itemPrice"> UID предложения торговца </param>
    /// <param name="npcTraderComponent"> компонент торговца </param>
    /// <param name="ballisticProviderComponent"> компонент хранилища патронов </param>
    /// <param name="providerUid"> UID хранилища патронов, который мы проверяем на наличии возможной оплаты </param>
    /// <returns></returns>
    private bool TryGiveEntityFromAmmoProvider(
        string itemPrice,
        NpcTraderComponent npcTraderComponent,
        BallisticAmmoProviderComponent ballisticProviderComponent,
        EntityUid providerUid)
    {
        if (ballisticProviderComponent.Proto == null)
            return false;

        var tempPer = ballisticProviderComponent.Proto; // На будущее... Напрямую работать с полем Proto нельзя.

        string protoIdString = "";
        if (tempPer != null)
        {
            var notNullTempPer = tempPer.Value;
            protoIdString = notNullTempPer.ToString();
        }
        else
        {
            return false;
        }

        if (!protoIdString.Equals(itemPrice))
            return false;

        if (ballisticProviderComponent.UnspawnedCount != 0)
        {
            if (npcTraderComponent.DelVirtAmmo.ContainsKey(providerUid))
            {
                if (ballisticProviderComponent.UnspawnedCount - npcTraderComponent.DelVirtAmmo[providerUid] > 0)
                {
                    npcTraderComponent.DelVirtAmmo[providerUid] += 1;
                    return true;
                }
            }
            else
            {
                npcTraderComponent.DelVirtAmmo.Add(providerUid, 1);
                return true;
            }
        }

        if (ballisticProviderComponent.Entities.Count != 0)
        {

            bool flag = false;
            foreach (var bullet in ballisticProviderComponent.Entities)
            {
                if (!CheckCartridgeComp((EntityUid)bullet, npcTraderComponent, false))
                {
                    continue;
                }

                if (!npcTraderComponent.DelRealAmmo.ContainsKey(bullet))
                {
                    npcTraderComponent.DelRealAmmo.Add(bullet, providerUid);
                    flag = true;
                    break;
                }
            }

            if (flag)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Метод, который ищет предметы для оплаты.
    /// </summary>
    /// <param name="npcUid"> торговец </param>
    /// <param name="itemPrice"> UID предложения торговца </param>
    /// <param name="buyer"> игрок </param>
    /// <returns></returns>
    public bool TryFindItem(EntityUid npcUid, NpcTraderComponent npcTraderComponent, string itemPrice, EntityUid buyer)
    {
        // Проверяем руки на наличие предмета-оплаты
        if (_entityManager.TryGetComponent(buyer, out HandsComponent? handsComponent))
        {
            foreach (var hand in handsComponent.Hands.Keys)
            {
                var tempHoldItem = _handSystem.GetHeldItem(buyer, hand);

                if (tempHoldItem != null)
                {
                    // если в руках обойма/коробка с патронами, то пытаемся найти нужные пули в ней.
                    if (_entityManager.TryGetComponent(tempHoldItem, out BallisticAmmoProviderComponent? ballisticProviderComponent))
                        if (TryGiveEntityFromAmmoProvider(itemPrice, npcTraderComponent, ballisticProviderComponent, (EntityUid) tempHoldItem))
                            return true;

                    // если в руках есть контейнер, то проверяем вещи внутри него
                    if (_entityManager.TryGetComponent(tempHoldItem, out StorageComponent? storageCmp))
                        if (TryFindEntityInStorage(storageCmp, npcTraderComponent, itemPrice))
                            return true;

                    if (!TryComp<MetaDataComponent>(tempHoldItem, out var metaData)) //BallisticAmmoProviderComponent
                        continue;

                    var prototypeId = metaData.EntityPrototype?.ID;
                    if (prototypeId == null)
                        continue;

                    if (!_prototype.TryIndex<EntityPrototype>(prototypeId, out var tempProto))
                        continue;

                    if (tempProto.ID.Equals(itemPrice))
                    {
                        if (npcTraderComponent.DelItem.Contains((EntityUid)tempHoldItem))
                            continue;

                        return CheckCartridgeComp((EntityUid)tempHoldItem, npcTraderComponent);    
                    }
                }
            }
        }

        // теперь ищем в карманах, на поясе, спине или в рюкзаке
        var slotEnumerator = _inventory.GetSlotEnumerator(buyer);
        while (slotEnumerator.NextItem(out var item, out var slot))
        {
            if (!_entityManager.TryGetComponent(item, out StorageComponent? storageComponent))
            {
                if (_entityManager.TryGetComponent(item, out BallisticAmmoProviderComponent? ballisticProvComponent))
                    if (TryGiveEntityFromAmmoProvider(itemPrice, npcTraderComponent, ballisticProvComponent, item))
                        return true;

                if (!TryComp<MetaDataComponent>(item, out var _metaData))
                    continue;

                var protoId = _metaData.EntityPrototype?.ID;
                if (protoId == null)
                    continue;

                if (!_prototype.TryIndex<EntityPrototype>(protoId, out var tmpProto))
                    continue;

                if (tmpProto.ID.Equals(itemPrice))
                {
                    if (npcTraderComponent.DelItem.Contains((EntityUid)item))
                        continue;

                    return CheckCartridgeComp((EntityUid)item, npcTraderComponent);
                }
            }
            else
            {
                if (storageComponent == null)
                    continue;

                if (TryFindEntityInStorage(storageComponent, npcTraderComponent, itemPrice))
                    return true;
            }
        }

        // if we didn't find anything on ourselves, we look for something nearby
        npcTraderComponent.EntitiesInRange.Clear();
        var Coordinates = _entityManager.GetComponent<TransformComponent>(npcUid).Coordinates;
        _entityLookup.GetEntitiesInRange(Coordinates, 1, npcTraderComponent.EntitiesInRange, flags: LookupFlags.Uncontained);

        foreach (var nearEntity in npcTraderComponent.EntitiesInRange)
        {
            if (_entityManager.TryGetComponent(nearEntity, out BallisticAmmoProviderComponent? ballisticAmmoProvComponent))
                if (TryGiveEntityFromAmmoProvider(itemPrice, npcTraderComponent, ballisticAmmoProvComponent, nearEntity))
                    return true;

            if (_entityManager.TryGetComponent(nearEntity, out StorageComponent? storageComp))
                if (TryFindEntityInStorage(storageComp, npcTraderComponent, itemPrice))
                    return true;

            if (!TryComp<MetaDataComponent>(nearEntity, out var _meta))
                continue;

            var protoID = _meta.EntityPrototype?.ID;
            if (protoID == null)
                continue;

            if (!_prototype.TryIndex<EntityPrototype>(protoID, out var tmpProt))
                continue;

            if (tmpProt.ID.Equals(itemPrice))
            {
                if (npcTraderComponent.DelItem.Contains((EntityUid)nearEntity))
                    continue;

                return CheckCartridgeComp((EntityUid)nearEntity, npcTraderComponent);
            }
        }

        return false;
    }

    /// <summary>
    /// Метод, который рекурсивно ищет оплату в хранилище (рюкзак, коробка и т.п.)
    /// </summary>
    /// <param name="storageComp"> Компонент хранилища </param>
    /// <param name="npcTraderComponent"> Компонент торговца </param>
    /// <param name="itemPrice"> Идентификатор предложения торговца </param>
    /// <returns></returns>
    private bool TryFindEntityInStorage(StorageComponent storageComp, NpcTraderComponent npcTraderComponent, string itemPrice)
    {
        foreach (var storageItem in storageComp.StoredItems) // проверяем рюкзак
        {
            if (_entityManager.TryGetComponent(storageItem.Key, out BallisticAmmoProviderComponent? ballisticProvComponent))
                if (TryGiveEntityFromAmmoProvider(itemPrice, npcTraderComponent, ballisticProvComponent, storageItem.Key))
                    return true;

            if (_entityManager.TryGetComponent(storageItem.Key, out StorageComponent? storageComponent))
                TryFindEntityInStorage(storageComponent, npcTraderComponent, itemPrice);

            if (!TryComp<MetaDataComponent>(storageItem.Key, out var meta))
                continue;

            var tempPrototypeId = meta.EntityPrototype?.ID;
            if (tempPrototypeId == null)
                continue;

            if (!_prototype.TryIndex<EntityPrototype>(tempPrototypeId, out var tempProt))
                continue;

            if (tempProt.ID.Equals(itemPrice))
            {
                if (npcTraderComponent.DelItem.Contains((EntityUid)storageItem.Key))
                    continue;

                return CheckCartridgeComp((EntityUid)storageItem.Key, npcTraderComponent);
            }
        }

        return false;
    }

    /// <summary>
    /// Метод, проверяющий, что патрон не израсходован. 
    /// </summary>
    /// <param name="uid"> UID патрона </param>
    /// <param name="npcTraderComponent"> Компонент торговца </param>
    /// <param name="addInDelQueu"> Нужно ли добавлять патрон в очередь на удаление... Необходимо для корректной обработки
    /// обойм и коробок с патронами </param>
    /// <returns></returns>
    private bool CheckCartridgeComp(EntityUid uid, NpcTraderComponent npcTraderComponent, bool addInDelQueu = true)
    {
        if (TryComp<CartridgeAmmoComponent>(uid, out var cartridge))
        {
            if (!cartridge.Spent)
            {
                if (addInDelQueu)
                    npcTraderComponent.DelItem.Add(uid);

                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            npcTraderComponent.DelItem.Add(uid);
            return true;
        }
    }
}
