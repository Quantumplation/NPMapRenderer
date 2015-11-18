using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json;

namespace NPMapRenderer
{
    public class Fleet
    {
        [JsonProperty("uid")]
        public int Id { get; set; }
        [JsonProperty("n")]
        public string Name { get; set; }
        [JsonProperty("puid")]
        public int Owner { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public PointF WorldPosition => new PointF(X, Y);
        public float LX { get; set; }
        public float LY { get; set; }
        public PointF WorldLPosition => new PointF(LX, LY);
        [JsonProperty("st")]
        public int Ships { get; set; }

        [JsonIgnore]
        public IEnumerable<Order> Orders { get; set; } = Enumerable.Empty<Order>();

        [JsonProperty("o")]
        public int[][] CompressedOrders
        {
            get { return Orders.Select(o => new[] {o.Delay, o.DestinationStar, (int) o.Command, o.CommandParameter}).ToArray(); }
            set
            {
                Orders =
                    value.Select(
                        xs =>
                            new Order
                            {
                                Delay = xs[0],
                                DestinationStar = xs[1],
                                Command = (FleetCommand) xs[2],
                                CommandParameter = xs[3]
                            });
            }
        } 
    }


    public enum FleetCommand
    {
        DoNothing,
        CollectAll,
        DropAll,
        Collect,
        Drop,
        CollectAllBut,
        DropAllBut,
        GarrisonStar
    }

    public class Order
    {
        public int Delay { get; set; }
        public int DestinationStar { get; set; }
        public FleetCommand Command { get; set; }
        public int CommandParameter { get; set; }
    }


}