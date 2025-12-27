using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Actions;
using Content.Shared.StatusIcon;

namespace Content.Shared._Metro14.Cestoid;

[RegisterComponent, NetworkedComponent]
public sealed partial class CestoidComponent : Component
{
    [DataField("cestoidStatusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "CestoidFaction";

    /// <summary>
    /// ID прототипа действия заражения игрока ленточником.
    /// </summary>
    [DataField]
    public EntProtoId CestoidInfectionAction = "ActionCestoidInfection";

    /// <summary>
    /// Сущность, хранящая действие заражения игрока ленточником.
    /// </summary>
    [DataField]
    public EntityUid? CestoidInfectionActionEntity;

    /// <summary>
    /// ID прототипа действия сбития с ног игрока.
    /// </summary>
    [DataField]
    public EntProtoId CestoidShootingDownAction = "ActionCestoidShootingDown";

    /// <summary>
    /// Сущность, хранящая действие сбития с ног игрока.
    /// </summary>
    [DataField]
    public EntityUid? CestoidShootingDownActionEntity;
}

/// <summary>
/// Событие, поднимаемое при нажатии кнопки-действия "ActionCestoidInfection".
/// </summary>
public sealed partial class CestoidInfectionActionEvent : EntityTargetActionEvent { }

/// <summary>
/// Событие, поднимаемое при нажатии кнопки-действия "ActionCestoidShootingDown".
/// </summary>
public sealed partial class CestoidShootingDownActionEvent : EntityTargetActionEvent { }
