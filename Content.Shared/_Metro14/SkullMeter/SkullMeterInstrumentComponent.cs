using System.Collections;
using System.Text;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Metro14.SkullMeter;

[RegisterComponent, NetworkedComponent]
public sealed partial class SkullMeterInstrumentComponent : Component
{
    [DataField("phraseYouMutant")]
    public string PhraseYouMutant = "mutant";

    [DataField("phraseYouHuman")]
    public string PhraseYouHuman = "human";
}
