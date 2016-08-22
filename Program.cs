using System.Threading;
using Akka.Actor;
using Akka_lift.Actors;
using Akka_lift.Messages;

namespace Akka_lift
{
    class Program
    {
        static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("ActorSystem");

            var buildingProps = Props.Create<Building>(10, 1);
            var buildingActor = actorSystem.ActorOf(buildingProps, "buildingActor");

            buildingActor.Tell(new BuildingOpenMessage());

            Thread.Sleep(1000);

            var passengerProps = Props.Create<Passenger>(buildingActor);
            var passengerActor = actorSystem.ActorOf(passengerProps, "Passenger1");
            passengerActor.Tell(new FloorMessage(0, 1));
            
            var passenger2Props = Props.Create<Passenger>(buildingActor);
            var passenger2Actor = actorSystem.ActorOf(passenger2Props, "Passenger2");
            passenger2Actor.Tell(new FloorMessage(0, 10));
            
            actorSystem.AwaitTermination();
        }
    }
}
