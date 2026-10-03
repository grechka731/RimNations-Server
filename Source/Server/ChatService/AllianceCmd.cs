using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.ChatService
{
    internal sealed class AllianceCmd : IChatCmd
    {
        public string CmdID => "alliance";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "alliance {invite|accept|break|list} [UserLogin] : Управление альянсами и союзами";

        private readonly ChatManager _chatManager;

        public AllianceCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            var myLogin = player.Public.Login;
            var data = Repository.GetData;
            if (data.Alliances == null) data.Alliances = new List<AllianceInfo>();

            if (argsM.Count < 1)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Использование: /alliance invite <Ник> | accept <Ник> | break <Ник> | list");
            }

            var subCmd = argsM[0].ToLower();

            if (subCmd == "list")
            {
                if (data.Alliances.Count == 0)
                {
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "На планете пока нет заключенных альянсов.");
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== СУЩЕСТВУЮЩИЕ АЛЬЯНСЫ ===");
                foreach (var a in data.Alliances)
                {
                    sb.AppendLine($"🛡 [{a.Name}] | Лидер: {a.LeaderLogin} | Члены: {string.Join(", ", a.MemberStates)}");
                }
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, sb.ToString().TrimEnd());
            }

            if (argsM.Count < 2)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.PlayerNameEmpty, myLogin, chat, "Укажите имя игрока-союзника: /alliance " + subCmd + " <Ник>");
            }

            var targetName = argsM[1];
            var targetPlayer = Repository.GetPlayerByLogin(targetName);
            if (targetPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, myLogin, chat, $"Игрок '{targetName}' не найден.");
            }

            if (targetPlayer.Public.Login == myLogin)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Нельзя создать альянс с самим собой!");
            }

            var myState = player.Public.StateName ?? (player.Public.Login + " Держава");
            var targetState = targetPlayer.Public.StateName ?? (targetPlayer.Public.Login + " Держава");

            if (subCmd == "invite")
            {
                if (!targetPlayer.Online)
                {
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, $"Правитель {targetPlayer.Public.Login} сейчас оффлайн! Предлагать союзы можно только игрокам онлайн.");
                }

                // Отправляем интерактивное письмо
                var mail = new ModelMailDiplomacyProposal()
                {
                    From = player.Public,
                    To = targetPlayer.Public,
                    Kind = DiplomacyProposalKind.AllianceOffer,
                    SenderLogin = myLogin,
                    SenderState = myState,
                    TargetLogin = targetPlayer.Public.Login,
                    TargetState = targetState,
                    MessageText = $"Правитель {myLogin} предлагает заключить Военный Альянс между [{myState}] и [{targetState}]."
                };

                lock (targetPlayer)
                {
                    targetPlayer.Mails.Add(mail);
                }

                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, $"Предложение союза отправлено правителю {targetPlayer.Public.Login}. Ожидайте подтверждения.");
            }
            else if (subCmd == "accept")
            {
                // Проверяем или создаем альянс
                var alliance = data.Alliances.FirstOrDefault(a => a.ContainsPlayer(targetPlayer.Public.Login));
                if (alliance == null)
                {
                    alliance = new AllianceInfo()
                    {
                        Name = $"Альянс {myState} и {targetState}",
                        LeaderLogin = targetPlayer.Public.Login,
                        MemberLogins = new List<string> { targetPlayer.Public.Login, myLogin },
                        MemberStates = new List<string> { targetState, myState }
                    };
                    data.Alliances.Add(alliance);
                }
                else
                {
                    if (!alliance.ContainsPlayer(myLogin))
                    {
                        alliance.MemberLogins.Add(myLogin);
                        alliance.MemberStates.Add(myState);
                    }
                }

                Repository.Get.ChangeData = true;
                var msg = $"🤝 [НОВЫЙ АЛЬЯНС]: Государства [{myState}] (Правитель: {myLogin}) и [{targetState}] (Правитель: {targetPlayer.Public.Login}) заключили Военный Альянс! Они обязуются помогать друг другу в обороне.";
                _chatManager.AddSystemPostToPublicChat(msg);
                Loger.Log($"Alliance created between {myState} and {targetState}", Loger.LogLevel.INFO);
            }
            else if (subCmd == "break")
            {
                var alliance = data.Alliances.FirstOrDefault(a => a.ContainsPlayer(myLogin) && a.ContainsPlayer(targetPlayer.Public.Login));
                if (alliance == null)
                {
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Вы не состоите в альянсе с этим игроком.");
                }

                alliance.MemberLogins.Remove(myLogin);
                alliance.MemberStates.Remove(myState);
                if (alliance.MemberLogins.Count <= 1)
                {
                    data.Alliances.Remove(alliance);
                }

                Repository.Get.ChangeData = true;
                var msg = $"💔 [РАСТОРЖЕНИЕ СОЮЗА]: Государство [{myState}] расторгло союз с государством [{targetState}].";
                _chatManager.AddSystemPostToPublicChat(msg);
                Loger.Log($"Alliance broken between {myState} and {targetState}", Loger.LogLevel.INFO);
            }

            return new ModelStatus();
        }
    }
}
