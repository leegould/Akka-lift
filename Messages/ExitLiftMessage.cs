namespace Akka_lift.Messages
{
    public class ExitLiftMessage
    {
        public int Floor { get; }

        public ExitLiftMessage(int floor)
        {
            Floor = floor;
        }
    }
}