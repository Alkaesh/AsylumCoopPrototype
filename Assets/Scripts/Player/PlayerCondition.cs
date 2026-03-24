namespace AsylumHorror.Player
{
    public enum PlayerCondition
    {
        Healthy = 0,
        Injured = 1,
        Knocked = 2,
        Carried = 3,
        Hooked = 4,
        Dead = 5,
        Escaped = 6
    }

    public enum PlayerLocomotionState
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        Crouch = 3
    }
}
