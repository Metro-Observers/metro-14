using Robust.Shared.Serialization;

namespace Content.Shared.Maps;

public sealed partial class ContentTileDefinition
{
    /// <summary>
    /// BASELINE - Vanilla tile filtering
    /// </summary>
    [DataField] public bool EditorHidden { get; private set; } = true;
}
