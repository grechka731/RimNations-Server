using System;
using System.Linq;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Services
{
    internal sealed class ServiceMobilizeMilitia : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request68MobilizeMilitia;
        public int ResponseTypePackage => (int)PackageType.Response69MobilizeMilitia;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = MobilizeMilitia((PacketMobilizeMilitia)request.Packet, context);
            return result;
        }

        private ModelStatus MobilizeMilitia(PacketMobilizeMilitia packet, ServiceContext context)
        {
            if (packet == null || packet.FromTile < 0 || packet.TargetTile < 0)
            {
                return new ModelStatus() { Status = 1, Message = "Invalid mobilization parameters." };
            }

            var player = context.Player;
            var data = Repository.GetData;

            lock (data)
            {
                int effectiveSquadSize = Math.Max(2, packet.SquadSize);

                // Fortress March archetype modifier
                if (!string.IsNullOrEmpty(packet.ArchetypeDefName) &&
                    packet.ArchetypeDefName.IndexOf("Fortress", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    effectiveSquadSize = (int)(effectiveSquadSize * 1.5f); // +50% battalion size
                }

                // Notify target or record in defense logs
                Loger.Log($"[RimNations Military] {player.Public.Login} mobilized provincial militia ({effectiveSquadSize} troopers) from Tile {packet.FromTile} to Tile {packet.TargetTile}!");
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = $"Militia squad successfully mobilized ({packet.SquadSize} troopers) towards target."
            };
        }
    }
}
