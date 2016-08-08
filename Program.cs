﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
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

            passengerActor.Tell(new FloorMessage(1));


            actorSystem.AwaitTermination();
        }
    }

    // MESSAGES

    public class BuildingOpenMessage { }

    public class InvalidFloorMessage { }

    public class NotOpenMessage { }

    public class FloorMessage
    {
        public int Floor { get; }

        public FloorMessage(int floorNumber)
        {
            Floor = floorNumber;
        }
    }

    public class LiftSuccessMessage
    {
        public int Floor { get; }

        public LiftSuccessMessage(int floorNumber)
        {
            Floor = floorNumber;
        }
    }

    // CLASSES

    public class Building : UntypedActor
    {
        private bool buildingOpen;
        private readonly int floors;
        private readonly int lifts;

        private IActorRef liftManager;

        public Building(int floors, int lifts)
        {
            this.floors = floors;
            this.lifts = lifts;
        }

        protected override void OnReceive(object message)
        {
            if (message is BuildingOpenMessage)
            {
                liftManager = Context.ActorOf(Props.Create(() => new LiftManager(floors, lifts)));

                buildingOpen = true;

                ConsoleExtensions.WriteLineColor(ConsoleColor.DarkRed, "Building Open");
            }
            else if (message is FloorMessage)
            {
                if (!buildingOpen)
                {
                    Sender.Tell(new NotOpenMessage());
                    return;
                }

                liftManager.Tell(message);
            }

        }
    }

    public class Passenger : UntypedActor
    {
        private readonly IActorRef liftManager;

        public Passenger(IActorRef liftManager)
        {

            this.liftManager = liftManager;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as FloorMessage;
            if (msg != null)
            {
                ConsoleExtensions.WriteLineColor(ConsoleColor.Cyan, "I'm a passenger and I want to go to floor " + msg.Floor);

                liftManager.Tell(new FloorMessage(msg.Floor));
            }
            if (message is InvalidFloorMessage)
            {
                ConsoleExtensions.WriteLineColor(ConsoleColor.Cyan, "Ah, I asked for a floor that doesn't exist.");
            }
            if (message is NotOpenMessage)
            {
                ConsoleExtensions.WriteLineColor(ConsoleColor.Cyan, "Building is not open yet!");
            }
        }
    }

    public class Lift : UntypedActor
    {
        private readonly int liftNumber;
        private int currentFloor;

        public Lift(int liftNumber, int initialFloorNumber = 0)
        {
            this.liftNumber = liftNumber;
            currentFloor = initialFloorNumber;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as FloorMessage;
            if (msg != null)
            {
                ConsoleExtensions.WriteLineColor(ConsoleColor.Green, "Moving lift #" + liftNumber + " to floor " + msg.Floor);

                currentFloor = msg.Floor;
            }
            Sender.Tell(new LiftSuccessMessage(currentFloor));
        }
    }



    public class LiftManager : UntypedActor
    {
        private readonly int floors;
        private int lifts;
        private List<LiftStatus> liftList;

        private class LiftStatus
        {
            public int LiftNumber { get; set; }

            public IActorRef Actor { get; set; }

            public bool InTransit { get; set; }

            public bool Available { get; set; }
        }

        public LiftManager(int floors, int lifts)
        {
            this.floors = floors;
            this.lifts = lifts;

            liftList = new List<LiftStatus>();

            for (var i = 0; i < lifts; i++)
            {
                var liftNumber = i;
                liftList.Add(new LiftStatus { LiftNumber = liftNumber, Actor = Context.ActorOf(Props.Create(() => new Lift(liftNumber, 0)), "lift" + liftNumber), Available = true, InTransit = false });
            }
        }

        protected override void OnReceive(object message)
        {
            if (message is FloorMessage)
            {
                var msg = message as FloorMessage;
                if (msg?.Floor > floors)
                {
                    Console.WriteLine("That floor doesn't exist!");

                    Sender.Tell(new InvalidFloorMessage());
                }
                else
                {
                    liftList.FirstOrDefault(x => x.Available)?.Actor.Tell(message);
                }
            }
            if (message is LiftSuccessMessage)
            {
                var msg = message as LiftSuccessMessage;

                ConsoleExtensions.WriteLineColor(ConsoleColor.Green, "Lift Succeeded! " + msg.Floor);
                //if (liftList.FirstOrDefault(x => x.LiftNumber == ) )
                //{

                //}
            }
        }
    }
}
