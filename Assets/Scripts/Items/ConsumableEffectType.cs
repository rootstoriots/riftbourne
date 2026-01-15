namespace Riftbourne.Items
{
    /// <summary>
    /// Enumeration of consumable effect types.
    /// Defines what kind of effect a consumable item applies.
    /// </summary>
    public enum ConsumableEffectType
    {
        Heal,              // Restores HP
        Damage,            // Deals damage
        BuffStat,          // Increases a stat temporarily
        DebuffStat,        // Decreases a stat temporarily
        RemoveStatusEffect, // Removes a status effect
        ApplyStatusEffect   // Applies a status effect
    }
}
