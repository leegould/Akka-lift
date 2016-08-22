using Akka.Actor;

namespace Akka_lift.Messages
{
    public class LiftArrivedMessage
    {
        public int FromFloor { get; }
        public int ToFloor { get; }

        public IActorRef Lift { get; }
        
        public LiftArrivedMessage(int fromFloor, int toFloor, IActorRef lift)
        {
            FromFloor = fromFloor;
            ToFloor = toFloor;
            Lift = lift;
        }
    }
}