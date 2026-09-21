namespace TankGame.Input
{
    public sealed class FirePressGate
    {
        bool waitingForRelease = true;
        public bool Read(bool pressedThisFrame, bool held)
        {
            bool requested = !waitingForRelease && pressedThisFrame;
            if (!held) waitingForRelease = false;
            return requested;
        }
    }
}
