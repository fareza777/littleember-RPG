namespace LittleEmber.Core
{
    /// <summary>Transient flags for the current app run (not persisted).</summary>
    public static class GameSession
    {
        /// <summary>True when the flow came from "New Game" (cinematic -> first spawn).</summary>
        public static bool IsNewGame;

        /// <summary>True when the flow came from "Continue" (skip cinematic, restore save).</summary>
        public static bool IsContinue;
    }
}
