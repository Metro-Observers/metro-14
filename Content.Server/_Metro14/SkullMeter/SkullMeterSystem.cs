using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Content.Shared._Metro14.SkullMeter;

namespace Content.Server._Metro14.SkullMeter;

public sealed class SkullMeterSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkullMeterComponent, InteractUsingEvent>(OnInteractUsing);
    }

    /// <summary>
    /// Данный метод является обработчиков события взаимодействия с объектом, который имеет компонент SkullMeterComponent.
    /// </summary>
    /// <param name="uid"> Человек с компонентом SkullMeterComponent, у которого измеряют череп. </param>
    /// <param name="comp"> Компонент, который сохраняет информацию об измерениях. </param>
    /// <param name="args"> Аргументы события. </param>
    private void OnInteractUsing(EntityUid uid, SkullMeterComponent comp, InteractUsingEvent args)
    {
        // Если взаимодействует объект без компонента инструмента измерителя черепа, то ничего не делаем
        if (!_entityManager.TryGetComponent(args.Used, out SkullMeterInstrumentComponent? skullMeterInstrumentComp))
            return;

        switch (comp.ResultTesting)
        {
            case OperationResult.None: // измерения не проводились
                {
                    if (_mindSystem.TryGetMind(uid, out var mindId, out var mind)) // пытаемся получить роль данного гуманоида,
                    {                                                              // чтобы все члены 4-го рейха, получали правильные результаты.
                        foreach (var role in mind.MindRoleContainer.ContainedEntities)
                        {
                            if (TryComp<MindRoleComponent>(role, out var roleComp))
                            {
                                string roleId = "";

                                if (roleComp.JobPrototype is not null)
                                    roleId = roleComp.JobPrototype.Value;
                                else if (roleComp.AntagPrototype is not null)
                                    roleId = roleComp.AntagPrototype.Value;

                                if (comp.AlwaysHumanRoles.Contains(roleId))
                                {
                                    comp.ResultTesting = OperationResult.Human;
                                    _popupSystem.PopupEntity(Loc.GetString(skullMeterInstrumentComp.PhraseYouHuman), uid);
                                    return;
                                }
                            }
                        }
                    }

                    OperationResult[] values = Enum.GetValues<OperationResult>();

                    int randomIndex = _random.Next(1, 3);
                    comp.ResultTesting = values[randomIndex];

                    if (comp.ResultTesting == OperationResult.Mutant)
                        _popupSystem.PopupEntity(Loc.GetString(skullMeterInstrumentComp.PhraseYouMutant), uid);
                    else
                        _popupSystem.PopupEntity(Loc.GetString(skullMeterInstrumentComp.PhraseYouHuman), uid);

                    break;
                }
            case OperationResult.Mutant: // По результатам измерения - мутант
                _popupSystem.PopupEntity(Loc.GetString(skullMeterInstrumentComp.PhraseYouMutant), uid);
                break;
            case OperationResult.Human: // По результатам измерения - человек
                _popupSystem.PopupEntity(Loc.GetString(skullMeterInstrumentComp.PhraseYouHuman), uid);
                break;
            default:
                _popupSystem.PopupEntity(Loc.GetString("skull-meter-measurements-failed-phrase"), uid);
                break;
        }

        return;
    }
}
