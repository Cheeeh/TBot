using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tbot.Services;
using Tbot.Workers;
using TBot.Ogame.Infrastructure.Enums;
using TBot.Ogame.Infrastructure.Models;

namespace Tbot.Includes {
	public interface IFleetScheduler {
		void SetTBotInstance(ITBotMain tbotInstance);
		void SetTBotOgameBridge(ITBotOgamedBridge tbotOgameBridge);
		Task SpyCrash(Celestial fromCelestial, Coordinate target = null);
		Task AutoFleetSave(Celestial celestial, bool isSleepTimeFleetSave = false, long minDuration = 0, bool forceUnsafe = false, bool WaitFleetsReturn = false, Missions TelegramMission = Missions.None, bool fromTelegram = false, bool saveall = false);
		Task<int> SendFleet(Celestial origin, Ships ships, Coordinate destination, Missions mission, decimal speed, Resources payload = null, CharacterClass playerClass = CharacterClass.NoClass, bool force = false);
		Task CancelFleet(Fleet fleet);
		Task<List<Fleet>> UpdateFleets();
		void RetireFleet(object fleet);
	}
}
