using Akka.Actor;

namespace Akka_lift.Messages
{
    public class BoardedLiftMessage
    {
        public int FromFloor { get; }
        public int ToFloor { get; }

        public IActorRef Passenger { get; }

        public IActorRef Lift { get; }

        public BoardedLiftMessage(int fromFloor, int toFloor, IActorRef lift, IActorRef passenger)
        {
            FromFloor = fromFloor;
            ToFloor = toFloor;
            Lift = lift;
            Passenger = passenger;
        }
    }
}