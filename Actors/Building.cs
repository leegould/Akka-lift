using Akka.Actor;
using Akka.Event;
using Akka_lift.Messages;

namespace Akka_lift.Actors
{
    public class Building : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();
        
        private bool buildingOpen;

        private IActorRef liftManager;

        public Building(int floors, int lifts)
        {
            Receive<BuildingOpenMessage>(msg =>
            {
                liftManager = Context.ActorOf(Props.Create(() => new LiftManager(floors, lifts)));

                buildingOpen = true;

                log.Info("Building Open");
            });

            Receive<RequestLiftMessage>(msg =>
            {
                if (!buildingOpen)
                {
                    Sender.Tell(new NotOpenMessage());
                    return;
                }

                liftManager.Tell(new RequestLiftMessage(msg.FromFloor, msg.ToFloor, Sender));
            });
        }
    }
}