using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Util.Internal;
using Akka_lift.Messages;

namespace Akka_lift.Actors
{
    public class LiftManager : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();
        
        private readonly List<PassengerWaiting> passengersWaiting;
        private readonly List<PassengerWaiting> passengersInLift;
        private readonly List<LiftStatus> lifts;

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
                        msg?.PassengerActor.Tell(new AllLiftsBusyMessage(msg.FromFloor, msg.ToFloor));
                    }
                    else
                    {
                        lift.Lift.Tell(msg);
                        lift.Available = false;
                        if (msg != null)
                        {
                            lift.TargetFloor = msg.ToFloor;

                            passengersWaiting.Add(new PassengerWaiting
                            {
                                Actor = msg.PassengerActor,
                                TargetFloor = msg.ToFloor
                            });
                        }
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