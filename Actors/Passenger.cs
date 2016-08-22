using System.Threading;
using Akka.Actor;
using Akka.Event;
using Akka_lift.Messages;

namespace Akka_lift.Actors
{
    public class Passenger : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();
        
        public Passenger(IActorRef building)
        {
            Receive<FloorMessage>(msg =>
            {
                log.Info("I'm a passenger and I want to go from floor " + msg.FromFloor + " to floor " + msg.ToFloor);
                building.Tell(new RequestLiftMessage(msg.FromFloor, msg.ToFloor, Self));
            });

            Receive<InvalidFloorMessage>(msg =>
            {
                log.Info("Ah, I asked for a floor that doesn't exist.");
            });

            Receive<NotOpenMessage>(msg =>
            {
                log.Info("Building is not open yet!");
            });

            Receive<LiftArrivedMessage>(msg =>
            {
                log.Info("Boarding lift");
                Sender.Tell(new BoardedLiftMessage(msg.FromFloor, msg.ToFloor, msg.Lift, Self));
            });

            Receive<ExitLiftMessage>(msg =>
            {
                log.Info("Exiting lift on floor " + msg.Floor + ". Thanks!");
            });

            Receive<AllLiftsBusyMessage>(msg =>
            {
                log.Info("Waiting for a lift..");
                Thread.Sleep(5000);
                Self.Tell(new FloorMessage(msg.FromFloor, msg.ToFloor));
            });
        }
    }
}