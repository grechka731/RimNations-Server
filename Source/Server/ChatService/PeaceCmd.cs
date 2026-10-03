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

namespace ServerOnlineCity.ChatService
{
    internal sealed class PeaceCmd : IChatCmd
    {
        public string CmdID => "peace";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "peace {UserLogin} : Заключить мирный договор и перемирие";

        private readonly ChatManager _chatManager;

        public PeaceCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            var myLogin = player.Public.Login;

            if (argsM.Count < 1)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.PlayerNameEmpty, myLogin, chat, "Укажите имя оппонента: /peace <Ник>");
            }

            bool isOffer = false;
            var targetName = argsM[0];
            if (targetName == "offer" && argsM.Count > 1)
            {
                isOffer = true;
                targetName = argsM[1];
            }

            var targetPlayer = Repository.GetPlayerByLogin(targetName);
            if (targetPlayer == null)
            {
                var targetState = Repository.GetStateByName(targetName);
                if (targetState != null)
                {
                    targetPlayer = Repository.GetData.GetStatePlayers(targetState.Name).FirstOrDefault();
                }
            }

            if (targetPlayer == null)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.UserNotFound, myLogin, chat, $"Игрок или государство '{targetName}' не найдено.");
            }

            var data = Repository.GetData;
            if (data.Wars == null || data.Wars.Count == 0)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "В данный момент между вами нет объявленной войны.");
            }

            var myState = player.Public.StateName;
            var targetLogin = targetPlayer.Public.Login;
            var targetStateName = targetPlayer.Public.StateName;

            if (isOffer)
            {
                if (!targetPlayer.Online)
                {
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, $"Оппонент {targetLogin} оффлайн. Предлагать мир можно только онлайн игрокам.");
                }

                var peaceMail = new Transfer.ModelMails.ModelMailDiplomacyProposal()
                {
                    From = player.Public,
                    To = targetPlayer.Public,
                    Kind = Transfer.ModelMails.DiplomacyProposalKind.PeaceOffer,
                    SenderLogin = myLogin,
                    SenderState = myState,
                    TargetLogin = targetLogin,
                    TargetState = targetStateName,
                    DurationDays = 7,
                    MessageText = "Предложение заключить мир и прекратить огонь."
                };
                lock (targetPlayer)
                {
                    targetPlayer.Mails.Add(peaceMail);
                }
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, $"Предложение мира отправлено правителю {targetLogin}. Ожидайте ответа.");
            }

            var war = data.Wars.FirstOrDefault(w =>
                (w.AttackerLogin == myLogin && w.DefenderLogin == targetLogin) ||
                (w.DefenderLogin == myLogin && w.AttackerLogin == targetLogin) ||
                (w.AttackerState == myState && w.DefenderState == targetStateName) ||
                (w.DefenderState == myState && w.AttackerState == targetStateName));

            if (war == null || war.Status == WarStatus.Peace)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Между вашими государствами нет активного конфликта.");
            }

            // Устанавливаем перемирие на 2 часа
            war.Status = WarStatus.Truce;
            war.ExpireTimeUtc = DateTime.UtcNow.AddHours(2);
            data.WarUpdateTime = DateTime.UtcNow;
            Repository.Get.ChangeData = true;

            var msg = $"🕊 [МИРНЫЙ ДОГОВОР]: Государства [{war.AttackerState}] и [{war.DefenderState}] заключили мир! Боевые действия прекращены. Действует перемирие на 2 часа.";
            _chatManager.AddSystemPostToPublicChat(msg);
            Loger.Log($"Peace concluded: {war.AttackerState} and {war.DefenderState}", Loger.LogLevel.INFO);

            return new ModelStatus();
        }
    }
}
