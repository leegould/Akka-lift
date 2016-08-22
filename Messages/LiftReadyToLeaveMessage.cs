using System.Collections.Generic;
using Akka.Actor;

namespace Akka_lift.Messages
{
    public class LiftReadyToLeaveMessage
    {
        public int FromFloor { get; }

        public int ToFloor { get; }

        public List<IActorRef> Passengers { get; }
        
        public LiftReadyToLeaveMessage(int fromFloor, int toFloor, List<IActorRef> passengers)
        {
            FromFloor = fromFloor;
            ToFloor = toFloor;
            Passengers = passengers;
        }
    }
}