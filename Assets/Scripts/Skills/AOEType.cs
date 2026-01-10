namespace Riftbourne.Skills
{
    /// <summary>
    /// Defines how AOE skills target - from the caster or at a location.
    /// </summary>
    public enum AOEType
    {
        None,           // No AOE
        FromSource,     // AOE expands from the casting unit
        TrueAOE         // AOE can target any location on the battlefield
    }
}
