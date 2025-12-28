using Content.Server.Procedural;
using Content.Shared.Procedural;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Metro14.RoomSpawner;

/// <summary>
/// Данный класс содержит логику для спавнеров комнат из допустимого пула сцен.
/// </summary>
public sealed class RoomSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    private Dictionary<string, List<string>> _notRepetitionsList = new Dictionary<string, List<string>>();


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoomSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    /// Метод, который создает на месте спавнера заранее замаплинную сцену и удаляет его.
    /// </summary>
    /// <param name="spawner"> Спавнер </param>
    /// <param name="args"> Аргументы события инициализации карты </param>
    private void OnMapInit(Entity<RoomSpawnerComponent> spawner, ref MapInitEvent args)
    {
        SpawnRoom(spawner);
        QueueDel(spawner);
    }

    /// <summary>
    /// Метод, в котормо происходит выбор шаблона для спавна комнаты.
    /// </summary>
    /// <param name="spawner"> Спавнер </param>
    private void SpawnRoom(Entity<RoomSpawnerComponent> spawner)
    {
        var rooms = new HashSet<DungeonRoomPrototype>();

        foreach (var roomProto in _proto.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            var whitelisted = false;

            foreach (var tag in spawner.Comp.RoomsTag)
            {
                if (roomProto.Tags.Contains(tag))
                {
                    whitelisted = true;
                    break;
                }
            }

            if (!whitelisted)
                continue;

            rooms.Add(roomProto);
        }

        if (rooms.Count == 0)
            return;

        DungeonRoomPrototype selectedRoom = _random.Pick(rooms)!;

        if (!spawner.Comp.CanRepetitions && spawner.Comp.RoomId != null) // комнаты не могут повторяться!
        {
            var uid = spawner.Owner;
            var meta = EntityManager.GetComponent<MetaDataComponent>(uid);

            var prototypeId = meta.EntityPrototype?.ID;
            if (prototypeId != null && _notRepetitionsList.ContainsKey(prototypeId)) // если прототип есть в списке использованных комнат для этого спавнера
            {
                if (_notRepetitionsList[prototypeId] != null && !_notRepetitionsList[prototypeId].Contains(selectedRoom.ID)) // если выпала не использованная комната
                {
                    _notRepetitionsList[prototypeId].Add(selectedRoom.ID);
                }
                else if (_notRepetitionsList[prototypeId] != null && _notRepetitionsList[prototypeId].Contains(selectedRoom.ID)) // если выпала использованная комната
                {
                    bool flag = false;
                    foreach (var _selecRoom in rooms)
                    {
                        if (!_notRepetitionsList[prototypeId].Contains(_selecRoom.ID))
                        {
                            selectedRoom = _selecRoom;
                            _notRepetitionsList[prototypeId].Add(selectedRoom.ID);
                            flag = true;
                            break;
                        }
                    }

                    if (!flag)
                    {
                        if (_proto.TryIndex(spawner.Comp.RoomId, out var _room))
                        {
                            selectedRoom = _room!;
                        }
                    }   
                }
            }
            else if (prototypeId != null)  // если прототипа нет в списке использованных комнат для этого спавнера
            {
                List<string> tempList = new List<string>();
                tempList.Add(selectedRoom.ID);

                _notRepetitionsList.Add(prototypeId, tempList);
            }
        }

        if (selectedRoom == null)
            return;

        if (!_proto.TryIndex<DungeonRoomPrototype>(selectedRoom, out var room))
        {
            Log.Error($"Unable to find matching room prototype ({room}) for {ToPrettyString(spawner)}");
            return;
        }

        var gridUid = Transform(spawner).GridUid;

        if (!TryComp<MapGridComponent>(gridUid, out var gridComp))
            return;

        var xform = Transform(spawner).Coordinates.Offset(-room.Size / 2);
        var random = new Random();

        var localAngle = Angle.Zero;
        if (TryComp<TransformComponent>(spawner.Owner, out var xformSpawner))
            localAngle = xformSpawner.LocalRotation;

        _dungeon.SpawnRoom(
            gridUid.Value, //  Уникальный идентификатор сетки карты, где будет размещена комната
            gridComp, // Компонент сетки карты MapGridComponent, содержащий информацию о тайлах
            _maps.LocalToTile(gridUid.Value, gridComp, xform), // Координаты Vector2i левого верхнего угла для размещения комнаты
            room, // Прототип комнаты DungeonRoomPrototype со структурой и содержимым
            localAngle, // Угол поворота сущности спавнера комнаты, на который будет поверенута матрица с сущностями для спавна (на сколько градусов развернется комната)
            null, // Набор зарезервированных тайлов HashSet<Vector2i>, которые нельзя использовать для размещения
            spawner.Comp.ClearExisting); // Если true, очищает существующие сущности на месте размещения комнаты
    }
}
