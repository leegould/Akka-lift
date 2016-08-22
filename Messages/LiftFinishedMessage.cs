using System.Collections.Generic;
using Akka.Actor;

namespace Akka_lift.Messages
{
    public class LiftFinishedMessage
    {
        public int Floor { get; }

        public IActorRef Lift { get; }

        public List<IActorRef> Passengers { get; }

        public LiftFinishedMessage(int floor, IActorRef lift, List<IActorRef> passengers)
        {
            Floor = floor;
            Lift = lift;
            Passengers = passengers;
        }
    }
}