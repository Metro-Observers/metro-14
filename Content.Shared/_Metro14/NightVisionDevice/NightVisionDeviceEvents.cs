using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared._Metro14.NightVisionDevice;

/// <summary>
/// Событие, поднимаемое при нажатии кнопки-действия.
/// </summary>
public sealed partial class ToggleNightVisionDeviceActionEvent : InstantActionEvent { }

/// <summary>
/// Сетевой ивент для передачи информации о необходимости переключить шейдеры.
/// </summary>
[Serializable, NetSerializable]
public sealed class ToggleNightVisionDeviceEvent : EntityEventArgs
{
    public bool State { get; } // в какое состояние переключить шейдеры.
    public NetEntity? NVDUid { get; } // UID ПНВ для передачи клиенту.
    public ResolvedSoundSpecifier? PathToSound { get; } // звук при включении/выключении ПНВ

    public ToggleNightVisionDeviceEvent(bool state, NetEntity? nvdUid, ResolvedSoundSpecifier? pathToSound)
    {
        State = state;
        NVDUid = nvdUid;
        PathToSound = pathToSound;
    }
}
