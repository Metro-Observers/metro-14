using System.Numerics;
using Content.Server.Chat.Managers;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Camera;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.DoAfter;
using Content.Shared._Metro14.Cestoid;

namespace Content.Server._Metro14.Cestoid;

/// <summary>
/// Класс, содержащий логику ленточников.
/// </summary>
public sealed class CestoidSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CestoidComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CestoidComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<CestoidComponent, WieldAttemptEvent>(OnWieldAttempt);
        SubscribeLocalEvent<CestoidComponent, CestoidInfectionActionEvent>(OnCestoidInfectionActionPressed);
        SubscribeLocalEvent<HumanoidAppearanceComponent, CestoidInfectionDoAfterEvent>(OnCestoidInfectionDoAfter);
        SubscribeLocalEvent<CestoidComponent, CestoidShootingDownActionEvent>(OnCestoidShootingDownActionPressed);
    }

    /// <summary>
    /// При получении компонента добавляем способности
    /// </summary>
    /// <param name="uid"> Ленточник </param>
    /// <param name="component"> Компонент ленточника </param>
    /// <param name="args"> Аргументы события инициализации </param>
    private void OnComponentInit(EntityUid uid, CestoidComponent component, ComponentInit args)
    {
        TrySetAction(uid, component.CestoidInfectionAction, component.CestoidInfectionActionEntity);
        TrySetAction(uid, component.CestoidShootingDownAction, component.CestoidShootingDownActionEntity);
        TrySetEnlargedTresholds(uid);

        if (component.IdAimProto != null && _mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            _mindSystem.TryAddObjective(mindId, mind, component.IdAimProto);
        }

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var message = Loc.GetString("cestoid-component-greeting");
        _chatMan.DispatchServerMessage(actor.PlayerSession, message);
    }

    /// <summary>
    /// Метод для установки кнопки-действия.
    /// </summary>
    /// <param name="uid"> Ленточник </param>
    /// <param name="actionProtoId"> Id прототипа действия </param>
    /// <param name="actionEntityUid"> Сущность, хранящая действие </param>
    private void TrySetAction(EntityUid uid, EntProtoId actionProtoId, EntityUid? actionEntityUid)
    {
        actionEntityUid = _actionsSystem.AddAction(uid, actionProtoId);

        if (actionEntityUid != null)
        {
            _actionsSystem.StartUseDelay(actionEntityUid.Value);
        }
    }

    /// <summary>
    /// Увеличиваем порог вхождения в критическое состояние и отодвигаем порог смерти.
    /// </summary>
    /// <param name="uid"> Ленточник </param>
    private void TrySetEnlargedTresholds(EntityUid uid)
    {
        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        _mobThresholdSystem.SetMobStateThreshold(uid, FixedPoint2.New(200), MobState.Critical);
        _mobThresholdSystem.SetMobStateThreshold(uid, FixedPoint2.New(210), MobState.Dead);
        _mobThresholdSystem.VerifyThresholds(uid, thresholds);
    }

    /// <summary>
    /// Возвращаем в норму порог вхождения в критическое состояние и порог смерти.
    /// </summary>
    /// <param name="uid"> Ленточник </param>
    private void TrySetStandartTresholds(EntityUid uid)
    {
        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        _mobThresholdSystem.SetMobStateThreshold(uid, FixedPoint2.New(100), MobState.Critical);
        _mobThresholdSystem.SetMobStateThreshold(uid, FixedPoint2.New(200), MobState.Dead);
        _mobThresholdSystem.VerifyThresholds(uid, thresholds);
    }

    /// <summary>
    /// Удаляем кнопки-действия при удалении компонента.
    /// </summary>
    /// <param name="uid"> Ленточник </param>
    /// <param name="component"> Компонент ленточника </param>
    /// <param name="args"> Аргументы события удаления компонента </param>
    private void OnComponentRemove(EntityUid uid, CestoidComponent component, ComponentRemove args)
    {
        if (!TryComp<ActionsComponent>(uid, out var actionsComp))
            return;

        if (component.CestoidInfectionActionEntity != null)
            _actionsSystem.RemoveAction((uid, actionsComp), component.CestoidInfectionActionEntity);

        if (component.CestoidShootingDownActionEntity != null)
            _actionsSystem.RemoveAction((uid, actionsComp), component.CestoidShootingDownActionEntity);

        TrySetStandartTresholds(uid);
    }

    /// <summary>
    /// Отменяем действие, если игрок пытается взять предмет в две руки.
    /// </summary>
    /// <param name="uid"> Ленточник </param>
    /// <param name="component"> Компонент ленточника </param>
    private void OnWieldAttempt(EntityUid uid, CestoidComponent component, ref WieldAttemptEvent args)
    {
        // args.User - кто пытается взять предмет  
        // args.Wielded - предмет, который пытаются взять  
        if (HasComp<WieldableComponent>(args.Wielded))
        {
            _popup.PopupEntity(Loc.GetString("cestoid-cant-wield-message"), uid, uid);

            var direction = new Vector2((float)_random.NextDouble() * 2 - 1, (float)_random.NextDouble() * 2 - 1);
            var intensity = 0.5f;
            _recoil.KickCamera(uid, direction * intensity);

            args.Cancel();
        }
    }

    /// <summary>
    /// Поднимаем DoAfter действие для попытки заразить игрока.
    /// </summary>
    /// <param name="uid"> Ленточник </param>
    /// <param name="component"> Компонент ленточника </param>
    /// <param name="args"> Аргументы таргетного действия заражения </param>
    private void OnCestoidInfectionActionPressed(EntityUid uid, CestoidComponent component, CestoidInfectionActionEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(args.Target, out var _compHuman))
            return;

        if (TryComp<CestoidComponent>(args.Target, out var _comp))
            return;

        if (_mobStateSystem.IsCritical(args.Target) && !_mobStateSystem.IsDead(args.Target))
        {
            var doAfterArgs = new DoAfterArgs(EntityManager, args.Performer,
                TimeSpan.FromSeconds(component.DoAfterActionTime),
                new CestoidInfectionDoAfterEvent(),
                args.Target)
            {
                BreakOnMove = true,      // Прервать при движении  
                BreakOnDamage = true,     // Прервать при уроне  
                NeedHand = true,          // Требуется свободная рука  
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
            args.Handled = true;
        }
    }

    /// <summary>
    /// Заражаем выбранную сущность и увеличиваем ее порог вхождения в критическое состояние.
    /// </summary>
    /// <param name="uid"> Ленточник </param>
    /// <param name="component"> Компонент гуманоида </param>
    /// <param name="args"> Аргументы таргетного действия заражения </param>
    private void OnCestoidInfectionDoAfter(EntityUid uid, HumanoidAppearanceComponent component, CestoidInfectionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        AddComp<CestoidComponent>(uid);

        var rejuvenateSystem = EntityManager.System<RejuvenateSystem>();
        rejuvenateSystem.PerformRejuvenate(uid);

        TrySetEnlargedTresholds(uid);
        args.Handled = true;
    }

    /// <summary>
    /// Сбиваем с ног сущность.
    /// </summary>
    /// <param name="uid"> Ленточник </param>
    /// <param name="component"> Компонент ленточника </param>
    /// <param name="args"> Аргументы таргетного действия сбития с ног </param>
    private void OnCestoidShootingDownActionPressed(EntityUid uid, CestoidComponent component, CestoidShootingDownActionEvent args)
    {
        if (_mobStateSystem.IsAlive(args.Target))
        {
            var stunSystem = EntityManager.System<SharedStunSystem>();
            stunSystem.TryKnockdown(args.Target, TimeSpan.FromSeconds(5));
            args.Handled = true;
        }
    }
}
