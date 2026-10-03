using System;
using System.Linq;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class ServiceUpdateTariff : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request64UpdateTariff;
        public int ResponseTypePackage => (int)PackageType.Response65UpdateTariff;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = UpdateTariff((PacketUpdateTariff)request.Packet, context);
            return result;
        }

        private ModelStatus UpdateTariff(PacketUpdateTariff packet, ServiceContext context)
        {
            if (packet == null) return new ModelStatus() { Status = 1, Message = "Invalid packet payload." };

            var player = context.Player;
            var stateName = string.IsNullOrEmpty(packet.StateName) ? player.Public.StateName : packet.StateName;
            if (string.IsNullOrEmpty(stateName))
            {
                return new ModelStatus() { Status = 1, Message = "No state associated with player." };
            }

            var data = Repository.GetData;
            lock (data)
            {
                var state = data.States.FirstOrDefault(s => string.Equals(s.Name, stateName, StringComparison.OrdinalIgnoreCase));
                if (state == null)
                {
                    return new ModelStatus() { Status = 1, Message = $"State '{stateName}' not found." };
                }

                // Tariff is strictly capped between 0% and 20%
                float rate = Math.Max(0.00f, Math.Min(0.20f, packet.CustomsTariff));
                state.CustomsTariff = rate;
                state.BorderBlockadeActive = packet.BorderBlockadeActive;

                data.UpdateStatesDic();
                data.StateUpdateTime = DateTime.UtcNow;

                Loger.Log($"[RimNations Tariff] State {stateName} updated: Tariff={rate * 100:F1}%, Blockade={state.BorderBlockadeActive}");
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = $"Tariff updated to {packet.CustomsTariff * 100:F1}% (Blockade: {packet.BorderBlockadeActive})."
            };
        }
    }
}
