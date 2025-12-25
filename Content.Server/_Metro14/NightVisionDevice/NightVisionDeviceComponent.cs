using System.Collections;
using System.Text;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Server._Metro14.NightVisionDevice;

/// <summary>
/// Компонент ПНВ.
/// </summary>
[RegisterComponent]
public sealed partial class NightVisionDeviceComponent : Component
{
    /// <summary>
    /// Состояние прибора в текущий моментю.
    /// </summary>
    public bool Enabled = false;

    /// <summary>
    /// Потребление энергии в секунду.
    /// </summary>
    [DataField("wattage")]
    public float Wattage = 0.5f;

    /// <summary>
    /// Путь до звука включения.
    /// </summary>
    [DataField("soundPathActivate")]
    public SoundSpecifier SoundPathActivate = new SoundPathSpecifier("/Audio/_Metro14/NightVisionDevice/nvd-on.ogg");

    /// <summary>
    /// Путь до звука выключения.
    /// </summary>
    [DataField("soundPathDisable")]
    public SoundSpecifier SoundPathDisable = new SoundPathSpecifier("/Audio/_Metro14/NightVisionDevice/nvd-off.ogg");

    /// <summary>
    /// ID прототипа действия, которое дается носителю ПНВ.
    /// </summary>
    [DataField("toggleAction")]
    public EntProtoId ToggleAction = "ActionToggleNightVisionDevice";

    /// <summary>
    /// Сущность действия, которое дается носителю ПНВ.
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;
}
