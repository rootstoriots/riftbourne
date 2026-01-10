namespace Riftbourne.Skills
{
    /// <summary>
    /// Defines the pattern type for AOE skills.
    /// </summary>
    public enum AOEPatternType
    {
        None,               // No AOE - single target only
        LineLimited,        // Straight line - only closest unit affected
        LinePassthrough,   // Straight line - all cells/units in line affected
        Cloud,              // Circular/cloud pattern around target point
        Fan                 // Fan/cone pattern expanding from source (1, 3, 5, 7... cells per distance)
    }
}
