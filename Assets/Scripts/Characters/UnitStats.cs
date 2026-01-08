namespace Riftbourne.Characters
{
    /// <summary>
    /// Handles core attributes (Strength, Finesse, Focus, Speed, Luck) for a unit.
    /// Component class to reduce Unit.cs complexity.
    /// </summary>
    public class UnitStats
    {
        private int strength;
        private int finesse;
        private int focus;
        private int speed;
        private int luck;

        // Base stats (before bonuses)
        public int BaseStrength => strength;
        public int BaseFinesse => finesse;
        public int BaseFocus => focus;
        public int BaseSpeed => speed;
        public int BaseLuck => luck;

        public UnitStats(int strength, int finesse, int focus, int speed, int luck)
        {
            this.strength = strength;
            this.finesse = finesse;
            this.focus = focus;
            this.speed = speed;
            this.luck = luck;
        }

        public void SetStrength(int value) => strength = value;
        public void SetFinesse(int value) => finesse = value;
        public void SetFocus(int value) => focus = value;
        public void SetSpeed(int value) => speed = value;
        public void SetLuck(int value) => luck = value;

        public void IncreaseStrength(int amount) => strength += amount;
        public void IncreaseFinesse(int amount) => finesse += amount;
        public void IncreaseFocus(int amount) => focus += amount;
        public void IncreaseSpeed(int amount) => speed += amount;
        public void IncreaseLuck(int amount) => luck += amount;
    }
}

