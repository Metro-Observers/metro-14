using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Metro14.NightVisionDevice;

public sealed partial class NightVisionDeviceOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "GreenOverlay";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _depressiveShader;

    public NightVisionDeviceOverlay()
    {
        IoCManager.InjectDependencies(this);
        _depressiveShader = _prototypeManager.Index(Shader).InstanceUnique();
        ZIndex = 9; // draw this over the DamageOverlay, RainbowOverlay etc, but before the black and white shader
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _depressiveShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_depressiveShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
