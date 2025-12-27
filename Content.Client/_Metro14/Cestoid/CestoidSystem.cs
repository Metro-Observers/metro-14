using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared._Metro14.Cestoid;

namespace Content.Client._Metro14.Cestoid;

/// <summary>
/// Класс, содержащий логику установки иконки для ленточников.
/// </summary>
public sealed class CestoidSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CestoidComponent, GetStatusIconsEvent>(GetCestoidIcon);
    }

    private void GetCestoidIcon(Entity<CestoidComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}
