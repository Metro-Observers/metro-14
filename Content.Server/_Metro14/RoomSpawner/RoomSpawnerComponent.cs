using Content.Shared.Random;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Content.Shared.Procedural;

namespace Content.Server._Metro14.RoomSpawner;

/// <summary>
/// Позволяет вам создать одну комнату из заранее заготовленного пула (определяется тегами комнат и спавнеров) во время инициализации карты.
/// </summary>
[RegisterComponent, Access(typeof(RoomSpawnerSystem))]
public sealed partial class RoomSpawnerComponent : Component
{
    /// <summary>
    /// Теги, по которым будут выбиратсья комнаты.
    /// Если хотя бы 1 тег спавнера совпал с тегами комнаты, то она будет добавлена в список возможных для спавна.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<TagPrototype>> RoomsTag = new();

    /// <summary>
    /// Поле, значение которого определяет, будут ли стерты сущности, поверх которых установлен спавнер.
    /// </summary>
    [DataField]
    public bool ClearExisting = true;

    /// <summary>
    /// Могут ли комнаты, создаваемые через данный спавнер, повторяться.
    /// </summary>
    [DataField]
    public bool CanRepetitions = true;

    /// <summary>
    /// Id прототипа-заглушки, который будет вставляться, если комнаты без повторений исчерпаны.
    /// Если значение данного поля равно null, то CanRepetitions в прототипе вновь поднимется в true.
    /// </summary>
    [DataField]
    public ProtoId<DungeonRoomPrototype>? RoomId = null;
}
