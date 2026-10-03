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
    internal sealed class DeclareWarCmd : IChatCmd
    {
        public string CmdID => "declarewar";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "declarewar {UserLogin} [Reason] : Объявить войну игроку или его государству (игрок должен быть онлайн!)";

        private readonly ChatManager _chatManager;

        public DeclareWarCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            var myLogin = player.Public.Login;

            if (argsM.Count < 1)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.PlayerNameEmpty, myLogin, chat, "Укажите имя игрока или название государства: /declarewar <Ник> [Причина]");
            }

            var targetName = argsM[0];
            var targetPlayer = Repository.GetPlayerByLogin(targetName);
            if (targetPlayer == null)
            {
                // Попробуем найти по названию государства
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

            if (targetPlayer.Public.Login == myLogin)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Нельзя объявить войну самому себе!");
            }

            // 1. ПРОВЕРКА ОНЛАЙНА ЗАЩИТНИКА: нельзя объявлять войну спящим!
            if (!targetPlayer.Online)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, $"Защитник {targetPlayer.Public.Login} сейчас не в сети! Объявлять войну и атаковать можно только игроков онлайн.");
            }

            // Проверка наличия государства у атакующего
            if (string.IsNullOrEmpty(player.Public.StateName))
            {
                player.Public.StateName = player.Public.Login + " Держава";
                var defaultState = new State() { Name = player.Public.StateName };
                Repository.GetData.States.Add(defaultState);
                Repository.GetData.UpdateStatesDic();
            }

            // Проверка наличия государства у защитника
            if (string.IsNullOrEmpty(targetPlayer.Public.StateName))
            {
                targetPlayer.Public.StateName = targetPlayer.Public.Login + " Держава";
                var defaultTargetState = new State() { Name = targetPlayer.Public.StateName };
                Repository.GetData.States.Add(defaultTargetState);
                Repository.GetData.UpdateStatesDic();
            }

            if (player.Public.StateName == targetPlayer.Public.StateName)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Нельзя воевать с членами своего собственного государства!");
            }

            var reason = argsM.Count > 1 ? string.Join(" ", argsM.Skip(1)) : "Территориальный спор и геополитика";

            var data = Repository.GetData;
            if (data.Wars == null) data.Wars = new List<WarInfo>();

            var myState = player.Public.StateName;
            var targetLogin = targetPlayer.Public.Login;
            var targetStateName = targetPlayer.Public.StateName;

            // Проверяем существующие войны между ними
            var existingWar = data.Wars.FirstOrDefault(w =>
                (w.AttackerLogin == myLogin && w.DefenderLogin == targetLogin) ||
                (w.DefenderLogin == myLogin && w.AttackerLogin == targetLogin) ||
                (w.AttackerState == myState && w.DefenderState == targetStateName) ||
                (w.DefenderState == myState && w.AttackerState == targetStateName));

            if (existingWar != null)
            {
                if (existingWar.IsInTruce)
                {
                    var truceRemaining = Math.Max(1, (int)Math.Ceiling((existingWar.ExpireTimeUtc - DateTime.UtcNow).TotalMinutes));
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, $"Между вашими государствами действует перемирие еще {truceRemaining} мин. Дождитесь его окончания.");
                }

                if (existingWar.IsInPreparation)
                {
                    var prepRemaining = Math.Max(1, (int)Math.Ceiling((existingWar.ActiveTimeUtc - DateTime.UtcNow).TotalMinutes));
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, $"Война уже объявлена! Идет подготовка, штурм начнется через {prepRemaining} мин.");
                }

                if (existingWar.IsActiveNow)
                {
                    return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "Ваши государства уже находятся в состоянии активной войны! Вы можете атаковать караванами.");
                }

                // Старая завершенная война - удаляем
                data.Wars.Remove(existingWar);
            }

            // Создаем новую войну: 15 минут на подготовку
            var now = DateTime.UtcNow;
            var prepMinutes = 15;
            var activeMinutes = 24 * 60; // 24 часа длится война

            var newWar = new WarInfo()
            {
                AttackerLogin = myLogin,
                AttackerState = player.Public.StateName,
                DefenderLogin = targetPlayer.Public.Login,
                DefenderState = targetPlayer.Public.StateName,
                Status = WarStatus.WarDeclared,
                DeclaredTimeUtc = now,
                ActiveTimeUtc = now.AddMinutes(prepMinutes),
                ExpireTimeUtc = now.AddMinutes(prepMinutes + activeMinutes),
                CasusBelli = reason
            };

            data.Wars.Add(newWar);
            data.WarUpdateTime = now;
            Repository.Get.ChangeData = true;

            // Отправляем интерактивное предупреждение защитнику
            var warMail = new ModelMailDiplomacyProposal()
            {
                From = player.Public,
                To = targetPlayer.Public,
                Kind = DiplomacyProposalKind.WarDeclaration,
                SenderLogin = myLogin,
                SenderState = player.Public.StateName,
                TargetLogin = targetPlayer.Public.Login,
                TargetState = targetPlayer.Public.StateName,
                MessageText = reason
            };
            lock (targetPlayer)
            {
                targetPlayer.Mails.Add(warMail);
            }

            var announce = $"⚔ [ОБЪЯВЛЕНИЕ ВОЙНЫ]: Государство [{newWar.AttackerState}] (Правитель: {myLogin}) объявило войну государству [{newWar.DefenderState}] (Правитель: {targetPlayer.Public.Login})! Причина: {reason}. Внимание! Началась 15-минутная фаза подготовки. Штурм баз будет разрешен в {newWar.ActiveTimeUtc:HH:mm} UTC.";
            _chatManager.AddSystemPostToPublicChat(announce);
            Loger.Log($"War declared: {newWar.AttackerState} -> {newWar.DefenderState}, reason: {reason}", Loger.LogLevel.INFO);

            return new ModelStatus();
        }
    }
}
