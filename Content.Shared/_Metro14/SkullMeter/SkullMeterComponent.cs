using System.Collections;
using System.Text;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Metro14.SkullMeter;

[RegisterComponent, NetworkedComponent]
public sealed partial class SkullMeterComponent : Component
{
    [DataField]
    public OperationResult ResultTesting = OperationResult.None;

    [DataField]
    public List<string> AlwaysHumanRoles = new List<string>(); // список ролей рейха, которые всегда должны быть "людьми"
}

public enum OperationResult : byte
{
    /// <summary>  
    /// Измерения не проводились.
    /// </summary>  
    None = 0,

    /// <summary>  
    /// Измерения показали, что тестируемый - мутант.
    /// </summary>  
    Mutant = 1,

    /// <summary>  
    /// Измерения показали, что тестируемый - человек.
    /// </summary>  
    Human = 2
}
