using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using OCUnion.Transfer.Types;
using ServerOnlineCity.Model;
using ServerOnlineCity.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer;

namespace ServerOnlineCity.ChatService
{
    internal sealed class WarsListCmd : IChatCmd
    {
        public string CmdID => "wars";

        public Grants GrantsForRun => Grants.UsualUser;

        public string Help => ChatManager.prefix + "wars : Список всех текущих войн и дипломатических конфликтов на сервере";

        private readonly ChatManager _chatManager;

        public WarsListCmd(ChatManager chatManager)
        {
            _chatManager = chatManager;
        }

        public ModelStatus Execute(ref PlayerServer player, Chat chat, List<string> argsM, ServiceContext context)
        {
            var myLogin = player.Public.Login;
            var data = Repository.GetData;

            if (data.Wars == null || data.Wars.Count == 0)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "На планете царит полный мир. Активных войн нет.");
            }

            var now = DateTime.UtcNow;
            var sb = new StringBuilder();
            sb.AppendLine("=== ТЕКУЩИЕ ВОЙНЫ НА ПЛАНЕТЕ ===");

            int count = 0;
            foreach (var war in data.Wars)
            {
                if (war.IsInPreparation)
                {
                    count++;
                    var mins = Math.Max(1, (int)Math.Ceiling((war.ActiveTimeUtc - now).TotalMinutes));
                    sb.AppendLine($"⚔ [{war.AttackerState}] -> [{war.DefenderState}] | ФАЗА ПОДГОТОВКИ (штурм через {mins} мин.) | Причина: {war.CasusBelli}");
                }
                else if (war.IsActiveNow)
                {
                    count++;
                    var hours = Math.Max(1, (int)Math.Ceiling((war.ExpireTimeUtc - now).TotalHours));
                    sb.AppendLine($"🔥 [{war.AttackerState}] VS [{war.DefenderState}] | ИДЕТ АКТИВНАЯ ВОЙНА (осталось {hours} ч.) | Причина: {war.CasusBelli}");
                }
                else if (war.IsInTruce)
                {
                    count++;
                    var mins = Math.Max(1, (int)Math.Ceiling((war.ExpireTimeUtc - now).TotalMinutes));
                    sb.AppendLine($"🕊 [{war.AttackerState}] & [{war.DefenderState}] | ПЕРЕМИРИЕ (еще {mins} мин.)");
                }
            }

            if (count == 0)
            {
                return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, "На планете царит полный мир. Активных войн нет.");
            }

            return _chatManager.PostCommandPrivatPostActivChat(ChatCmdResult.CommandNotFound, myLogin, chat, sb.ToString().TrimEnd());
        }
    }
}
