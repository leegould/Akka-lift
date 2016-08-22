using Akka.Actor;

namespace Akka_lift.Messages
{
    public class RequestLiftMessage
    {
        public int FromFloor { get; }
        public int ToFloor { get; }

        public IActorRef PassengerActor { get; }

        public RequestLiftMessage(int fromFloor, int toFloor, IActorRef passengerActor)
        {
            FromFloor = fromFloor;
            ToFloor = toFloor;
            PassengerActor = passengerActor;
        }
    }
}