namespace Game.Gameplay.CharactersManagement.Actions
{
    /// <summary>
    /// How the players drive their robot in a level. The configurations shuffle the mode's actions on its buttons,
    /// the first ones of the Gameplay action map: A/S, Y/W, B/D, X/A, then RB/E and LB/Q.
    /// </summary>
    public enum ECharacterControlMode
    {
        // Walk forward, left or right, turn left or right, special action: 6 buttons.
        Advanced,
        // Go up, left, right or down, facing that way: the face buttons.
        Simplified,
        // The simplified directions and the special action: the face buttons and RB/E.
        SimplifiedWithSpecialAction,
    }

    public static class CharacterControlModeExtensions
    {
        public static int ButtonsCount(this ECharacterControlMode controlMode)
        {
            switch (controlMode)
            {
                case ECharacterControlMode.Simplified:
                    return 4;
                case ECharacterControlMode.SimplifiedWithSpecialAction:
                    return 5;
                case ECharacterControlMode.Advanced:
                default:
                    return 6;
            }
        }
    }
}
