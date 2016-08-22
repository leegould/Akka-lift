namespace Akka_lift.Messages
{
    public class FloorMessage
    {
        public int FromFloor { get; }
        public int ToFloor { get; }
        
        public FloorMessage(int fromFloor, int toFloor)
        {
            FromFloor = fromFloor;
            ToFloor = toFloor;
        }
    }
}