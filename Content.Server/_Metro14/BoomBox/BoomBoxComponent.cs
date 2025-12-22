using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Metro14.BoomBox;


[RegisterComponent, Access(typeof(BoomBoxSystem))]
public sealed partial class BoomBoxComponent : Component
{
    /// <summary>
    /// Поле, отвечающие за показатель воспроизведения музыки в текущий момент.
    /// </summary>
    public bool Enabled = false;

    /// <summary>
    /// Поле требуемое для работы с звуковым потоком.
    /// </summary>
    public EntityUid? Stream = null;

    /// <summary>
    /// Поле необходимое для регулировки громкости проигрывател.
    /// </summary>
    public float Volume = -13f;

    /// <summary>
    /// Поле, показывающее, есть ли в данный момент в проигрывателе кассета.
    /// </summary>
    public bool Inserted = false;

    /// <summary>
    /// Поле, необходимое для передачи сигнала на нужный выход.
    /// Обязательно должно совпадать с полем указанным в прототипе под компонентом DeviceLinkSource.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string Port = "Pressed";

    /// <summary>
    /// Поле, содержащее путь к файлу, который будет воспроизводится по умолчанию, если в кассете будет указан неправильный аудио-файл.
    /// </summary>
    public string SoundPath = "Audio/_Metro14/BoomBox/Default/BelyjShum.ogg";
}
