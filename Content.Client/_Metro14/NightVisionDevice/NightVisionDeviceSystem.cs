using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Shared.Overlays;
using Content.Shared._Metro14.NightVisionDevice;

namespace Content.Client._Metro14.NightVisionDevice;

public sealed class NightVisionDeviceSystem : EntitySystem
{
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private NightVisionDeviceOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
        SubscribeNetworkEvent<ToggleNightVisionDeviceEvent>(OnToggleNightVisionDeviceEvent);
    }

    /// <summary>
    /// Метод-обработчик события ToggleNightVisionDeviceEvent, которое поднимается сервером при нажатии на кнопку дейтсвия.
    /// </summary>
    /// <param name="args"> Аргументы события:
    /// State (bool) - в какое состояние перевести шейдеры;
    /// NVDUid (NetEntity?) - сетевой UID ПНВ;
    /// PathToSound (ResolvedSoundSpecifier?) - звук при включении/выключении ПНВ;
    /// </param>
    private void OnToggleNightVisionDeviceEvent(ToggleNightVisionDeviceEvent args)
    {
        if (args.NVDUid == null)
            return;

        EntityUid? tempEntity = GetEntity(args.NVDUid);
        if (tempEntity == null)
            return;

        if (args.PathToSound == null)
            return;

        EntityUid nvdEntity = (EntityUid) tempEntity;

        if (args.State)
        {
            EnableNightVision(nvdEntity, (ResolvedSoundSpecifier) args.PathToSound);
        }
        else
        {
            DisableNightVision(nvdEntity, (ResolvedSoundSpecifier) args.PathToSound);
        }
    }

    /// <summary>
    /// Метод для включения ПНВ.
    /// </summary>
    /// <param name="uid"> ПНВ </param>
    /// <param name="sound"> Звук при включении/выключении ПНВ </param>
    private void EnableNightVision(EntityUid uid, ResolvedSoundSpecifier sound)
    {
        _audioSystem.PlayPvs(sound, uid, AudioParams.Default.WithVolume(-2f));

        var light = EnsureComp<PointLightComponent>(uid);

        light.NetSyncEnabled = false; // только false, иначе остальные увидят!
        _pointLightSystem.SetRadius(uid, 7f, light);
        _pointLightSystem.SetEnergy(uid, 0.5f, light);
        _pointLightSystem.SetEnabled(uid, true, light);

        _overlayMan.AddOverlay(_overlay);
    }

    /// <summary>
    /// Метод для выключения ПНВ.
    /// </summary>
    /// <param name="uid"> ПНВ </param>
    /// <param name="sound"> Звук при включении/выключении ПНВ </param>
    private void DisableNightVision(EntityUid uid, ResolvedSoundSpecifier sound)
    {
        _audioSystem.PlayPvs(sound, uid, AudioParams.Default.WithVolume(-2f));

        _overlayMan.RemoveOverlay(_overlay);

        if (TryComp<PointLightComponent>(uid, out var light))
        {
            _pointLightSystem.SetEnabled(uid, false, light);
        }
    }
}
