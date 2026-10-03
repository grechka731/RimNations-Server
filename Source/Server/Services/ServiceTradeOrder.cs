using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class ServiceTradeOrder : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request66TradeOrder;
        public int ResponseTypePackage => (int)PackageType.Response67TradeOrder;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = ProcessTradeOrder((PacketTradeOrder)request.Packet, context);
            return result;
        }

        private ModelStatus ProcessTradeOrder(PacketTradeOrder packet, ServiceContext context)
        {
            if (packet == null || string.IsNullOrEmpty(packet.ItemDefName) || packet.Count <= 0)
            {
                return new ModelStatus() { Status = 1, Message = "Invalid trade order specifications." };
            }

            var player = context.Player;
            var data = Repository.GetData;

            lock (data)
            {
                if (data.Orders == null) data.Orders = new List<TradeOrder>();

                long orderId = data.Orders.Count == 0 ? 1 : data.Orders.Max(o => o.Id) + 1;

                var newOrder = new TradeOrder()
                {
                    Id = orderId,
                    Owner = new Player() { Login = player.Public.Login },
                    Tile = packet.Tile,
                    Created = DateTime.UtcNow,
                    CountReady = 1,
                    BuyThings = new List<ThingTrade>(),
                    SellThings = new List<ThingTrade>()
                };

                var itemTrade = ThingTrade.CreateTradeServer(packet.ItemDefName, packet.Count);
                itemTrade.GameCost = Math.Max(0.01f, packet.PricePerUnit);

                int totalSilver = (int)Math.Max(1, packet.Count * packet.PricePerUnit);
                var silverTrade = ThingTrade.CreateTradeServer("Silver", totalSilver);
                silverTrade.GameCost = 1f;

                if (string.Equals(packet.OrderType, "Buy", StringComparison.OrdinalIgnoreCase))
                {
                    newOrder.SellThings.Add(silverTrade);
                    newOrder.BuyThings.Add(itemTrade);
                }
                else
                {
                    newOrder.SellThings.Add(itemTrade);
                    newOrder.BuyThings.Add(silverTrade);
                }

                data.Orders.Add(newOrder);

                Loger.Log($"[RimNations Market] Order #{orderId} created by {player.Public.Login}: {packet.OrderType} {packet.Count}x {packet.ItemDefName} @ {packet.PricePerUnit} silver.");
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = $"Trade order for {packet.Count}x {packet.ItemDefName} successfully posted to market."
            };
        }
    }
}
