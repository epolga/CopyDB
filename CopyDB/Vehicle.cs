using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CopyDB
{
    abstract public class Vehicle
    {
        public Vehicle(string name)
        {
            this.Name = name;
        }
        
        public string Name { get; protected set; } = string.Empty;
        abstract public void Park();
    }

    public class Car : Vehicle
    {
        public Car(string name) : base(name)
        {
        }

        public override void Park()
        {
            Console.WriteLine("Parking Car " + Name);
        }
    }
    public class Bicycle : Vehicle
    {
        public Bicycle(string name) : base(name)
        {
        }

        public override void Park()
        {
            Console.WriteLine("Parking Bicycle " + Name);
        }
    }

    public class Motorcycle : Vehicle
    {
        public Motorcycle(string name) : base(name)
        {
        }

        public override void Park()
        {
            Console.WriteLine("Parking Motorcycle " + Name);
        }
    }

    public class ParkingLot
    {
        private ConcurrentDictionary<string, Vehicle> vehicles = new ConcurrentDictionary<string, Vehicle>();
        public void ParkVehicle(Vehicle vehicle)
        {
            vehicles.TryAdd(vehicle.Name, vehicle);
        }

        public void ReleaseVehicle(string name)
        {
            Vehicle? vehicle = null;

            if (vehicles.TryRemove(name, out vehicle))
            {
                Console.WriteLine($"Vehicle {name} removed");
            }
            else
            {
                Console.WriteLine($"Vehicle {name} doesn't exist");
            }
        }

        public bool Reverse(char[] chars)
        {
            if(chars == null)
            {
                Console.WriteLine("Null received");
                return false;
            }

            if(chars.Length <= 1)
            {
                return true;
            }
            char ouxChar = '\0';
            int iRight = 0, iLeft = chars.Length - 1; 
            for(iRight = 0, iLeft = chars.Length - 1; iRight > iLeft; iRight++, iLeft--)
            {
                ouxChar = chars[iRight];
                chars[iRight] = chars[iLeft];
                chars[iLeft] = ouxChar;
            }
            return true;
        }

        public bool ReverseString(string str)
        {
            if (str == null) {
                Console.WriteLine("Null received");
                return false;
            }
            char[] chars = str.ToCharArray();
            bool result = Reverse(chars);
            return result;
        }
    }
}
