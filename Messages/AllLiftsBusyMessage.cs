namespace Akka_lift.Messages
{
    public class AllLiftsBusyMessage
    {
        public int FromFloor { get; }

        public int ToFloor { get; }

        public AllLiftsBusyMessage(int fromFloor, int toFloor)
        {
            FromFloor = fromFloor;
            ToFloor = toFloor;
        }
    }
}