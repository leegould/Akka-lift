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

    // MESSAGES

    public class BuildingOpenMessage { }

    public class InvalidFloorMessage { }

    public class NotOpenMessage { }

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

        public IActorRef Lift { get; }
        
        public LiftArrivedMessage(int fromFloor, int toFloor, IActorRef lift)
        {
            FromFloor = fromFloor;
            ToFloor = toFloor;
            Lift = lift;
        }
    }

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

    public class ExitLiftMessage
    {
        public int Floor { get; }

        public ExitLiftMessage(int floor)
        {
            Floor = floor;
        }
    }

    public class AllLiftsBusyMessage
    {
        public int FromFloor { get; }

        public int ToFloor { get; }

        public AllLiftsBusyMessage(int fromFloor, int toFloor)
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
    

    public class LiftManager : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();

        private readonly int floorcount;
        private int liftcount;
        private List<PassengerWaiting> passengersWaiting;
        private List<PassengerWaiting> passengersInLift;
        private List<LiftStatus> lifts;

        private class LiftStatus
        {
            public IActorRef Lift { get; set; }

            public bool Available { get; set; } 

            public int TargetFloor { get; set; }
        }

        private class PassengerWaiting
        {
            public IActorRef Actor { get; set; }

            public int TargetFloor { get; set; }
        }

        public LiftManager(int floorcount, int liftcount)
        {
            this.floorcount = floorcount;
            this.liftcount = liftcount;

            passengersWaiting = new List<PassengerWaiting>();
            passengersInLift = new List<PassengerWaiting>();
            lifts = new List<LiftStatus>();

            for (var i = 0; i < liftcount; i++)
            {
                var liftNumber = i;
                lifts.Add(new LiftStatus { Available = true, Lift = Context.ActorOf(Props.Create(() => new Lift(liftNumber, 0))) });
            }

            Receive<RequestLiftMessage>(msg =>
            {
                if (msg?.ToFloor > floorcount)
                {
                    log.Error("That floor doesn't exist!");
                    Sender.Tell(new InvalidFloorMessage());
                }
                else
                {
                    log.Info("Requesting a lift..");

                    var lift = lifts.FirstOrDefault(x => x.Available); // TODO : selection criteria

                    if (lift == null)
                    {
                        msg.PassengerActor.Tell(new AllLiftsBusyMessage(msg.FromFloor, msg.ToFloor));
                    }
                    else
                    {
                        lift.Lift.Tell(msg);
                        lift.Available = false;
                        lift.TargetFloor = msg.ToFloor;

                        passengersWaiting.Add(new PassengerWaiting
                        {
                            Actor = msg.PassengerActor,
                            TargetFloor = msg.ToFloor
                        });
                    }
                }
            });

            Receive<LiftArrivedMessage>(msg =>
            {
                log.Info("Lift Arrived at " + msg.FromFloor + " for floor " + msg.ToFloor);
                
                var passengers = passengersWaiting.Where(x => x.TargetFloor == msg.ToFloor);
                passengers.Select(x => x.Actor).ForEach(x => x.Tell(msg));
            });

            Receive<BoardedLiftMessage>(msg =>
            {
                log.Info("Passenger " + msg.Passenger.Path + " has boarded lift.");
                passengersInLift.Add(new PassengerWaiting { Actor = msg.Passenger, TargetFloor = msg.ToFloor });
                var passengersOnBoard = passengersInLift.Where(x => msg.ToFloor == x.TargetFloor).ToList();
                if (passengersWaiting.Count(x => x.TargetFloor == msg.ToFloor) == passengersOnBoard.Count)
                {
                    log.Info("All passengers aboard.");
                    passengersWaiting.Where(x => x.TargetFloor == msg.ToFloor).ToList().ForEach(x => passengersWaiting.Remove(x));
                    msg.Lift.Tell(new LiftReadyToLeaveMessage(msg.FromFloor, msg.ToFloor, passengersOnBoard.Select(x => x.Actor).ToList()));
                }
                else
                {
                    log.Info("Waiting for more passengers.");
                }
            });

            Receive<LiftFinishedMessage>(msg =>
            {
                log.Info("Lift journey finished.");
                passengersInLift.Where(x => x.TargetFloor == msg.Floor).ToList().ForEach(x => passengersInLift.Remove(x));
                foreach (var psg in msg.Passengers)
                {
                    psg.Tell(new ExitLiftMessage(msg.Floor));
                }
                var firstOrDefault = lifts.FirstOrDefault(x => x.Available == false && x.TargetFloor == msg.Floor);
                if (firstOrDefault != null)
                {
                    firstOrDefault.Available = true;
                }
            });
        }
    }
}
