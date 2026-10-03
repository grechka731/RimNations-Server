using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCUnion
{
    public class AttackUtils
    {
        public static float MaxCostAttackerCaravan(float costTarget, bool isSettlement)
        {
            if (isSettlement)
            {
                //Очень грубо упрощенная средняя формула получения цены атакующих рейдов из исходника игры
                //  4 / (25/1000000 + 10/богатство колонии) + 2000 = богатство нападающих   (4 от сложности и коэф. времени, + 2000 на еду каравана)

                return 4f / (25f / 1000000f + 10f / costTarget) + 2000f;
            }
            else
            {
                //Если будет атака на караваны, то атакуемые могут быть сильнее на 15%
                return costTarget * 1.15f;
            }
        }

        public static string CheckPossibilityAttack(IPlayerEx attacker, IPlayerEx host, long attackerWOServerId, long hostWOServerId
            , bool protectingNovice, List<OCUnion.Transfer.Model.WarInfo> wars = null)
        {
            try
            {
                if (!attacker.Online) return "Вы не в сети!";
                if (!host.Online) return "Защитник оффлайн! Нападение запрещено, пока правитель не в сети.";

                // Проверка дипломатического статуса (война / мир)
                if (wars != null)
                {
                    var aLogin = attacker.Public.Login;
                    var hLogin = host.Public.Login;
                    var aState = attacker.Public.StateName ?? "";
                    var hState = host.Public.StateName ?? "";

                    var war = wars.FirstOrDefault(w => 
                        (w.AttackerLogin == aLogin && w.DefenderLogin == hLogin) ||
                        (w.DefenderLogin == aLogin && w.AttackerLogin == hLogin) ||
                        (!string.IsNullOrEmpty(aState) && !string.IsNullOrEmpty(hState) &&
                         ((w.AttackerState == aState && w.DefenderState == hState) ||
                          (w.DefenderState == aState && w.AttackerState == hState)))
                    );

                    if (war == null || war.Status == OCUnion.Transfer.Model.WarStatus.Peace)
                    {
                        return "Мирное время! Вы не можете напасть: сначала объявите войну (/declarewar).";
                    }

                    if (war.IsInPreparation)
                    {
                        var remainingMin = Math.Max(1, (int)Math.Ceiling((war.ActiveTimeUtc - DateTime.UtcNow).TotalMinutes));
                        return $"Фаза подготовки к войне! Штурм будет доступен через {remainingMin} мин.";
                    }

                    if (war.IsInTruce)
                    {
                        var remainingMin = Math.Max(1, (int)Math.Ceiling((war.ExpireTimeUtc - DateTime.UtcNow).TotalMinutes));
                        return $"Действует перемирие! Нападение запрещено еще {remainingMin} мин.";
                    }

                    if (!war.IsActiveNow)
                    {
                        return "Срок действия войны истек. Заключите мир или объявите войну заново.";
                    }
                }
                else
                {
                    if (!attacker.Public.EnablePVP) return "У вас отключен PVP режим!";
                    if (!host.Public.EnablePVP) return "У защитника отключен PVP режим!";
                }

                if (!protectingNovice) return null;

                var hostCosts = host.CostWorldObjects(hostWOServerId);
                var hostCost = MaxCostAttackerCaravan(hostCosts.MarketValueTotal, true);

                var attCosts = attacker.CostWorldObjects(attackerWOServerId);
                var attCost = attCosts.MarketValueTotal;

                string res =
                    //стоимость колонии больше стоимости каравана
                    attCost > hostCost
                    ? //"The cost of the attackers is higher than the cost of the colony, this is not fair"
                    "The cost of the attacker must be less than " + ((long)hostCost).ToString()
                    //колонии больше 1 года
                    //to do  : host.Public.LastTick < 3600000 ? "You must not attack the game for less than a year"

                    //колонию атаковали недавно 
                    : (DateTime.UtcNow - host.Public.LastPVPTime).TotalMinutes < host.MinutesIntervalBetweenPVP
                    ? "It was recently attacked. Wait to " + host.Public.LastPVPTime.ToGoodUtcString()
                    : null;

                /*
                if (res != null) Loger.Log("CheckPossibilityAttack: " + res
                    + " LastOnlineTime=" + attacker.Public.LastOnlineTime.ToString("o")
                    + " UtcNow=" + DateTime.UtcNow.ToString("o")
                    );
                */
                return res;
            }
            catch (Exception exp)
            {
                Loger.Log("CheckPossibilityAttack Exception" + exp.ToString(), Loger.LogLevel.ERROR);
                return "Error calc";
            }
        }
    }
}
