using System.Threading;
using Akka.Actor;
using Akka.Event;
using Akka_lift.Messages;

namespace Akka_lift.Actors
{
    public class Lift : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();
        
        private int currentFloor;

        public Lift(int liftNumber, int initialFloorNumber = 0)
        {
            currentFloor = initialFloorNumber;

            Receive<RequestLiftMessage>(msg =>
            {
                if (currentFloor != msg.FromFloor)
                {
                    log.Info("Moving lift #" + liftNumber + " from floor " + currentFloor + " to floor " + msg.FromFloor);
                    currentFloor = msg.FromFloor;
                    Thread.Sleep(2000);
                }
                Sender.Tell(new LiftArrivedMessage(currentFloor, msg.ToFloor, Self));
            });

            Receive<LiftReadyToLeaveMessage>(msg =>
            {
                log.Info("Moving lift #" + liftNumber + " from floor " + currentFloor + " to floor " + msg.ToFloor);
                currentFloor = msg.ToFloor;
                Thread.Sleep(2000);
                Sender.Tell(new LiftFinishedMessage(msg.ToFloor, Self, msg.Passengers));
            });
        }
    }
}