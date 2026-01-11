namespace Riftbourne.Exploration
{
    /// <summary>
    /// Confidence level for journal entries.
    /// Determines how certain the player is about an observation or interpretation.
    /// </summary>
    public enum ConfidenceLevel
    {
        /// <summary>
        /// Certain observation: "This is a burn mark"
        /// </summary>
        Certain,
        
        /// <summary>
        /// Uncertain observation: "This might be a burn mark"
        /// </summary>
        Uncertain,
        
        /// <summary>
        /// Speculative interpretation: "Could this be a ritual site?"
        /// </summary>
        Speculative
    }
}
