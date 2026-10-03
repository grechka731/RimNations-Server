using System;
using System.Linq;
using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Services
{
    internal sealed class ServiceDeclareWar : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request62DeclareWar;
        public int ResponseTypePackage => (int)PackageType.Response63DeclareWar;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = DeclareWar((PacketDeclareWar)request.Packet, context);
            return result;
        }

        private ModelStatus DeclareWar(PacketDeclareWar packet, ServiceContext context)
        {
            if (packet == null || (string.IsNullOrEmpty(packet.DefenderLogin) && string.IsNullOrEmpty(packet.DefenderState)))
            {
                return new ModelStatus() { Status = 1, Message = "Target player or nation not specified." };
            }

            var attacker = context.Player;
            var attackerLogin = attacker.Public.Login;

            var data = Repository.GetData;
            lock (data)
            {
                PlayerServer defender = null;
                if (!string.IsNullOrEmpty(packet.DefenderLogin))
                {
                    defender = Repository.GetPlayerByLogin(packet.DefenderLogin);
                }

                if (defender == null && !string.IsNullOrEmpty(packet.DefenderState))
                {
                    var targetState = Repository.GetStateByName(packet.DefenderState);
                    if (targetState != null)
                    {
                        defender = data.GetStatePlayers(targetState.Name).FirstOrDefault();
                    }
                }

                if (defender == null)
                {
                    return new ModelStatus() { Status = 1, Message = "Target player or nation not found." };
                }

                if (defender.Public.Login == attackerLogin)
                {
                    return new ModelStatus() { Status = 1, Message = "Cannot declare war upon yourself!" };
                }

                // Hardware-enforced online status check
                if (!defender.Online)
                {
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = $"Defender {defender.Public.Login} is offline! Civilized Warfare rules strictly prohibit declaring war or raiding offline players."
                    };
                }

                // Check active peace treaty
                var treaty = data.Treaties?.FirstOrDefault(t => t.IsActive &&
                    ((t.VictorLogin == attackerLogin && t.DefeatedLogin == defender.Public.Login) ||
                     (t.VictorLogin == defender.Public.Login && t.DefeatedLogin == attackerLogin)));
                if (treaty != null)
                {
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = $"Cannot declare war! An active peace/reparations treaty is in effect until {treaty.ExpiresUtc:yyyy-MM-dd HH:mm} UTC."
                    };
                }

                // Check existing war
                var existingWar = data.Wars?.FirstOrDefault(w =>
                    (w.IsActiveNow || w.IsInPreparation) &&
                    ((w.AttackerLogin == attackerLogin && w.DefenderLogin == defender.Public.Login) ||
                     (w.AttackerLogin == defender.Public.Login && w.DefenderLogin == attackerLogin)));
                if (existingWar != null)
                {
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = $"War is already declared between {attackerLogin} and {defender.Public.Login}!"
                    };
                }

                // Check Alliance: Cannot declare war on alliance member
                var sharedAlliance = data.Alliances?.FirstOrDefault(a =>
                    a.ContainsPlayer(attackerLogin) && a.ContainsPlayer(defender.Public.Login));
                if (sharedAlliance != null)
                {
                    return new ModelStatus()
                    {
                        Status = 1,
                        Message = $"Cannot declare war! Both nations belong to the '{sharedAlliance.Name}' alliance. Break alliance pact first."
                    };
                }

                // Construct War Record with 15-minute preparation countdown
                var casusBelli = string.IsNullOrEmpty(packet.CasusBelli) ? "Territorial Expansion" : packet.CasusBelli;
                var now = DateTime.UtcNow;
                var newWar = new WarInfo()
                {
                    AttackerLogin = attackerLogin,
                    AttackerState = attacker.Public.StateName ?? attackerLogin,
                    DefenderLogin = defender.Public.Login,
                    DefenderState = defender.Public.StateName ?? defender.Public.Login,
                    Status = WarStatus.WarDeclared,
                    DeclaredTimeUtc = now,
                    ActiveTimeUtc = now.AddMinutes(15), // 15-minute preparation grace period
                    ExpireTimeUtc = now.AddDays(7),
                    CasusBelli = casusBelli
                };

                if (data.Wars == null) data.Wars = new System.Collections.Generic.List<WarInfo>();
                data.Wars.Add(newWar);
                data.WarUpdateTime = now;

                // Send high-priority Mail to Defender
                var warNotice = new ModelMailMessadge()
                {
                    From = new Player() { Login = "system" },
                    To = new Player() { Login = defender.Public.Login },
                    type = ModelMailMessadge.MessadgeTypes.ThreatBig,
                    label = "ВОЙНА ОБЪЯВЛЕНА!",
                    text = $"Государство {newWar.AttackerState} ({attackerLogin}) объявило войну вашему народу!\nCasus Belli: {casusBelli}\nНачалась 15-минутная фаза подготовки. Соберите ополчение и укрепите оборону!"
                };
                defender.Mails.Add(warNotice);

                // Article 5: Alliance Mutual Defense Trigger
                var defenderAlliance = data.Alliances?.FirstOrDefault(a => a.ContainsPlayer(defender.Public.Login));
                if (defenderAlliance != null)
                {
                    foreach (var memberLogin in defenderAlliance.MemberLogins)
                    {
                        if (memberLogin == defender.Public.Login) continue;
                        var ally = Repository.GetPlayerByLogin(memberLogin);
                        if (ally != null)
                        {
                            var callToArms = new ModelMailMessadge()
                            {
                                From = new Player() { Login = "system" },
                                To = new Player() { Login = memberLogin },
                                type = ModelMailMessadge.MessadgeTypes.ThreatBig,
                                label = $"[СТАТЬЯ 5] ПРИЗЫВ К ОРУЖИЮ: {defenderAlliance.Name}",
                                text = $"Член альянса {defender.Public.Login} подвергся агрессии со стороны {attackerLogin}! Активирован протокол коллективной обороны."
                            };
                            ally.Mails.Add(callToArms);
                        }
                    }
                }

                Loger.Log($"[RimNations War] {attackerLogin} declared war on {defender.Public.Login} with Casus Belli: '{casusBelli}'. 15m prep until {newWar.ActiveTimeUtc:HH:mm} UTC.");
            }

            return new ModelStatus()
            {
                Status = 0,
                Message = $"War successfully declared. 15-minute preparation phase initiated before active combat begins."
            };
        }
    }
}
