using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Util.Internal;
using Microsoft.Win32;

namespace Akka_lift
{
    class Program
    {
        static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("ActorSystem");

            var buildingProps = Props.Create<Building>(1, 1);
            var buildingActor = actorSystem.ActorOf(buildingProps, "buildingActor");

            buildingActor.Tell(new BuildingOpenMessage());

            Thread.Sleep(1000);

            var passengerProps = Props.Create<Passenger>(buildingActor);
            var passengerActor = actorSystem.ActorOf(passengerProps, "Passenger1");

            passengerActor.Tell(new FloorMessage(0, 1));


            actorSystem.AwaitTermination();
        }
    }

    // MESSAGES

    public class BuildingOpenMessage { }

    public class InvalidFloorMessage { }

    public class NotOpenMessage { }

    public class RequestLiftMessage
    {
        public int FromFloor { get; }
        public int ToFloor { get; }


        public RequestLiftMessage(int fromFloor, int toFloor)
        {
            FromFloor = fromFloor;
            ToFloor = toFloor;
        }
    }

    public class FloorMessage
    {
        public int FromFloor { get; }
        public int ToFloor { get; }


        public FloorMessage(int fromFloor, int toFloor)
        {
            FromFloor = fromFloor;
            ToFloor = toFloor;
        }
    }

    public class LiftArrivedMessage
    {
        public int FromFloor { get; }
        public int ToFloor { get; }


        public LiftArrivedMessage(int fromFloor, int toFloor)
        {
            FromFloor = fromFloor;
            ToFloor = toFloor;
        }
    }

    // CLASSES

    public class Building : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();
        
        private bool buildingOpen;
        private readonly int floors;
        private readonly int lifts;

        private IActorRef liftManager;

        public Building(int floors, int lifts)
        {
            this.floors = floors;
            this.lifts = lifts;

            Receive<BuildingOpenMessage>(msg =>
            {
                liftManager = Context.ActorOf(Props.Create(() => new LiftManager(floors, lifts)));

                buildingOpen = true;

                log.Info("Building Open");
            });

            Receive<FloorMessage>(msg =>
            {
                if (!buildingOpen)
                {
                    Sender.Tell(new NotOpenMessage());
                    return;
                }

                liftManager.Tell(new RequestLiftMessage(msg.FromFloor, msg.ToFloor));
            });
        }
    }

    public class Passenger : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private readonly IActorRef liftManager;

        public Passenger(IActorRef liftManager)
        {
            this.liftManager = liftManager;

            Receive<FloorMessage>(msg =>
            {
                log.Info("I'm a passenger and I want to go from floor " + msg.FromFloor + " to floor " + msg.ToFloor);
                liftManager.Tell(new RequestLiftMessage(msg.FromFloor, msg.ToFloor));
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
            });
        }
    }

    public class Lift : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private readonly int liftNumber;
        private int currentFloor;

        public Lift(int liftNumber, int initialFloorNumber = 0)
        {
            this.liftNumber = liftNumber;
            currentFloor = initialFloorNumber;

            Receive<RequestLiftMessage>(msg =>
            {
                if (currentFloor != msg.FromFloor)
                {
                    log.Info("Moving lift #" + liftNumber + " from floor " + currentFloor + " to floor " + msg.FromFloor);
                    currentFloor = msg.FromFloor;
                }
                Sender.Tell(new LiftArrivedMessage(currentFloor, msg.ToFloor));
            });
        }
    }
    

    public class LiftManager : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private readonly int floors;
        private int lifts;
        private readonly List<LiftStatus> liftList;
        private List<PassengerWaiting> passengersWaiting;

        private class LiftStatus
        {
            public int LiftNumber { get; set; }

            public IActorRef Actor { get; set; }

            public bool InTransit { get; set; }

            public bool Available { get; set; }
        }

        private class PassengerWaiting
        {
            public IActorRef Actor { get; set; }

            public int TargetFloor { get; set; }
        }

        public LiftManager(int floors, int lifts)
        {
            this.floors = floors;
            this.lifts = lifts;

            liftList = new List<LiftStatus>();
            passengersWaiting = new List<PassengerWaiting>();

            for (var i = 0; i < lifts; i++)
            {
                var liftNumber = i;
                liftList.Add(new LiftStatus { LiftNumber = liftNumber, Actor = Context.ActorOf(Props.Create(() => new Lift(liftNumber, 0)), "lift" + liftNumber), Available = true, InTransit = false });
            }

            Receive<RequestLiftMessage>(msg =>
            {
                if (msg?.ToFloor > floors)
                {
                    log.Error("That floor doesn't exist!");
                    Sender.Tell(new InvalidFloorMessage());
                }
                else
                {
                    log.Info("Requesting a lift..");
                    liftList.FirstOrDefault(x => x.Available)?.Actor.Tell(msg);
                    passengersWaiting.Add(new PassengerWaiting { Actor = Sender, TargetFloor = msg.ToFloor});
                }
            });

            Receive<LiftArrivedMessage>(msg =>
            {
                log.Info("Lift Arrived at " + msg.FromFloor);

                var passengers = passengersWaiting.Where(x => x.TargetFloor == msg.ToFloor);

                passengers.Select(x => x.Actor).ForEach(x => x.Tell(msg));
            });
        }
    }
}
