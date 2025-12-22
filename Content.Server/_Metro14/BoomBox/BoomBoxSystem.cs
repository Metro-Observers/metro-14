using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Interaction;
using Content.Server.NPC.HTN;
using Content.Server.Speech.Components;
using Content.Server.UserInterface;
using Content.Shared._Metro14.BoomBox;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Speech.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using AudioComponent = Robust.Shared.Audio.Components.AudioComponent;
using Robust.Server.GameObjects;

namespace Content.Server._Metro14.BoomBox;

/// <summary>
/// Данный класс содержит всю логику, связанную с проигрывателем (BoomBox).
/// Если есть какие-то вопросы по реализации, то можете написать в discord (BeatusCrow).
/// Также стоит добавить, что данный код был опубликован как PR официальным разработчикам 2 февраля 2024 года.
/// С тех пор он не менялся, и в данный момент этот код является простой адаптацией с исправлением некоторых ошибок.
/// Ссылка на PR: https://github.com/space-wizards/space-station-14/pull/24875
/// </summary>
public sealed class BoomBoxSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Методы, отвечающие за логику при установке/извлечении кассет.        
        SubscribeLocalEvent<BoomBoxComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<BoomBoxComponent, EntRemovedFromContainerMessage>(OnItemRemoved);

        // Методы, реализующие логику нажатия кнопок
        SubscribeLocalEvent<BoomBoxComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxPlusVolMessage>(OnPlusVolButtonPressed);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxMinusVolMessage>(OnMinusVolButtonPressed);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxStartMessage>(OnStartButtonPressed);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxStopMessage>(OnStopButtonPressed);
    }

    /// <summary>
    /// Метод вызывается при установке кассеты в проигрыватель.
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="comp"> Компонент проигрывателя </param>
    /// <param name="args"> Аргументы события </param>
    private void OnItemInserted(EntityUid uid, BoomBoxComponent comp, EntInsertedIntoContainerMessage args)
    {
        _popup.PopupEntity(Loc.GetString("tape-in"), uid);
        comp.Inserted = true;

        UpdateSoundPath(uid, comp);
    }

    /// <summary>
    /// Метод вызывается при извелчении кассеты из проигрывателя.
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="comp"> Компонент проигрывателя </param>
    /// <param name="args"> Аргументы события </param>
    private void OnItemRemoved(EntityUid uid, BoomBoxComponent comp, EntRemovedFromContainerMessage args)
    {
        _popup.PopupEntity(Loc.GetString("tape-out"), uid);

        comp.Stream = _audioSystem.Stop(comp.Stream);

        comp.Inserted = false;
        comp.Enabled = false;
    }

    /// <summary>
    /// Метод находит кассету в слоте проигрывателя и узнает у нее путь к .ogg файлу.
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="comp"> Компонент проигрывателя </param>
    private void UpdateSoundPath(EntityUid uid, BoomBoxComponent comp)
    {
        if (!TryComp<ItemSlotsComponent>(uid, out var itemSlots))
            return;

        if (_itemSlotsSystem.TryGetSlot(uid, "boombox_slot", out var slot, itemSlots))
        {
            if (slot.HasItem && slot.Item is not null)
            {
                AddCurrentSoundPath(uid, comp, slot.Item.Value);
            }
        }
    }

    /// <summary>
    /// Метод, который добавляет путь к .ogg файлу в компонент проигрывателя.
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="comp"> Компонент проигрывателя </param>
    /// <param name="added"> Кассета </param>
    private void AddCurrentSoundPath(EntityUid uid, BoomBoxComponent comp, EntityUid added)
    {
        if (!TryComp<BoomBoxTapeComponent>(added, out var BoomBoxTapeComp) || BoomBoxTapeComp.SoundPath is null)
            return;

        comp.SoundPath = BoomBoxTapeComp.SoundPath;
    }

    /// <summary>
    /// Метод-обработчик нажатия кнопки "уменьшить громкость"
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="component"> Компонент проигрывателя </param>
    /// <param name="args"> Аргументы события </param>
    private void OnMinusVolButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxMinusVolMessage args)
    {
        MinusVol(uid, component);
    }

    /// <summary>
    /// Метод-обработчик нажатия кнопки "увеличить громкость"
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="component"> Компонент проигрывателя </param>
    /// <param name="args"> Аргументы события </param>
    private void OnPlusVolButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxPlusVolMessage args)
    {
        PlusVol(uid, component);
    }

    /// <summary>
    /// Метод-обработчик нажатия кнопки "начать воспроизведение"
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="component"> Компонент проигрывателя </param>
    /// <param name="args"> Аргументы события </param>
    private void OnStartButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxStartMessage args)
    {
        StartPlay(uid, component);
    }

    /// <summary>
    /// Метод-обработчик нажатия кнопки "остановить воспроизведение"
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="component"> Компонент проигрывателя </param>
    /// <param name="args"> Аргументы события </param>
    private void OnStopButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxStopMessage args)
    {
        StopPlay(uid, component);
    }

    /// <summary>
    /// Обработчик переключение пользовательского интерфейса.
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="component"> Компонент проигрывателя </param>
    /// <param name="args"> Аргументы события </param>
    private void OnToggleInterface(EntityUid uid, BoomBoxComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    #region Методы реализующие логику нажатой кнопки.

    private void UpdateUserInterface(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        bool canPlusVol = true;
        bool canMinusVol = true;
        bool canStop = false;
        bool canStart = false;

        if (component.Volume >= 5)
            canPlusVol = false;

        if (component.Volume <= -13)
            canMinusVol = false;

        if (component.Inserted)
        {
            if (component.Enabled)
            {
                canStart = false;
                canStop = true;
            }
            else
            {
                canStart = true;
                canStop = false;
            }
        }
        else
        {
            canStart = false;
            canStop = false;
        }


        var state = new BoomBoxUiState(canPlusVol, canMinusVol, canStop, canStart);
        _userInterface.SetUiState(uid, BoomBoxUiKey.Key, state);
    }

    /// <summary>
    /// Метод для уменьшения громкости.
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="component"> Компонент проигрывателя </param>
    private void MinusVol(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Volume <= -13)
            return;

        component.Volume = component.Volume - 3f;
        _audioSystem.SetVolume(component.Stream, component.Volume);

        _signalSystem.InvokePort(uid, component.Port);

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Метод для учеличения громкости.
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="component"> Компонент проигрывателя </param>
    private void PlusVol(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Volume >= 5)
            return;

        component.Volume = component.Volume + 3f;
        _audioSystem.SetVolume(component.Stream, component.Volume);

        _signalSystem.InvokePort(uid, component.Port);

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Метод для начала воспроизведения музыки.
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="component"> Компонент проигрывателя </param>
    private void StartPlay(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Inserted && !component.Enabled)
        {
            component.Enabled = true;
            _popup.PopupEntity(Loc.GetString("boombox-on"), uid);

            // Мы воспроизводим музыку с этими параметрами. Обязательно установите значение "WithLoop(true)", это позволит воспроизводить музыку бесконечно.
            component.Stream = _audioSystem.PlayPvs(component.SoundPath, uid, AudioParams.Default.WithVolume(component.Volume).WithLoop(true).WithMaxDistance(7f))?.Entity;
        }

        _signalSystem.InvokePort(uid, component.Port);
        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Метод для приостановки воспроизведения музыки.
    /// </summary>
    /// <param name="uid"> Проигрыватель </param>
    /// <param name="component"> Компонент проигрывателя </param>
    private void StopPlay(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Inserted && component.Enabled)
        {
            component.Enabled = false;
            _popup.PopupEntity(Loc.GetString("boombox-off"), uid);

            // Отключение зацикленного аудиопотока
            component.Stream = _audioSystem.Stop(component.Stream);
        }

        _signalSystem.InvokePort(uid, component.Port);
        UpdateUserInterface(uid, component);
    }

    #endregion
}
