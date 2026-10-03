using System;
using System.Linq;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class ServiceCreateNation : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request60CreateNation;
        public int ResponseTypePackage => (int)PackageType.Response61CreateNation;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = CreateNation((PacketCreateNation)request.Packet, context);
            return result;
        }

        private ModelStatus CreateNation(PacketCreateNation packet, ServiceContext context)
        {
            if (packet == null || string.IsNullOrEmpty(packet.StateName))
            {
                return new ModelStatus() { Status = 1, Message = "State name cannot be empty." };
            }

            var login = context.Player.Public.Login;
            var stateName = packet.StateName.Trim();

            var data = Repository.GetData;
            lock (data)
            {
                // Verify uniqueness if it's a new state or modifying own state
                var existing = data.States.FirstOrDefault(s => string.Equals(s.Name, stateName, StringComparison.OrdinalIgnoreCase));
                if (existing != null && context.Player.Public.StateName != existing.Name)
                {
                    return new ModelStatus() { Status = 1, Message = $"State '{stateName}' already exists!" };
                }

                if (existing == null)
                {
                    existing = new State()
                    {
                        Name = stateName,
                        Color = string.IsNullOrEmpty(packet.Color) ? "#FFD700" : packet.Color,
                        Description = packet.Description ?? "Sovereign Nation",
                        CapitalTile = packet.CapitalTile,
                        BorderRadius = packet.BorderRadius > 0 ? packet.BorderRadius : 4
                    };
                    data.States.Add(existing);
                }
                else
                {
                    existing.Color = string.IsNullOrEmpty(packet.Color) ? existing.Color : packet.Color;
                    existing.Description = packet.Description ?? existing.Description;
                    if (packet.CapitalTile > 0) existing.CapitalTile = packet.CapitalTile;
                    if (packet.BorderRadius > 0) existing.BorderRadius = packet.BorderRadius;
                }

                context.Player.Public.StateName = existing.Name;
                data.UpdateStatesDic();
                data.StateUpdateTime = DateTime.UtcNow;

                Loger.Log($"[RimNations Server] Player {login} proclaimed/updated state: {existing.Name} (Capital: {existing.CapitalTile})");
            }

            return new ModelStatus() { Status = 0, Message = $"Sovereign nation '{stateName}' established successfully." };
        }
    }
}
