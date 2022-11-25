using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBot.Common.Logging;
using Tbot.Includes;
using TBot.Ogame.Infrastructure.Enums;
using Tbot.Services;
using Microsoft.Extensions.Logging;
using System.Threading;
using Tbot.Helpers;
using TBot.Model;
using TBot.Ogame.Infrastructure.Models;
using TBot.Ogame.Infrastructure;

namespace Tbot.Workers.Brain {
	public class LifeformsAutoResearchWorker : WorkerBase {
		private readonly IAutoMineWorker _autoMineWorker;
		private readonly IOgameService _ogameService;
		private readonly IFleetScheduler _fleetScheduler;
		private readonly ICalculationService _calculationService;
		private readonly ITBotOgamedBridge _tbotOgameBridge;
		public LifeformsAutoResearchWorker(ITBotMain parentInstance,
			IAutoMineWorker autoMineWorker,
			IOgameService ogameService,
			IFleetScheduler fleetScheduler,
			ICalculationService calculationService,
			ITBotOgamedBridge tbotOgameBridge) :
			base(parentInstance) {
			_autoMineWorker = autoMineWorker;
			_ogameService = ogameService;
			_fleetScheduler = fleetScheduler;
			_calculationService = calculationService;
			_tbotOgameBridge = tbotOgameBridge;
		}
		public override bool IsWorkerEnabledBySettings() {
			try {
				return (
					(bool) _tbotInstance.InstanceSettings.Brain.Active && (bool) _tbotInstance.InstanceSettings.Brain.LifeformAutoResearch.Active
				);
			} catch (Exception) {
				return false;
			}
		}
		public override string GetWorkerName() {
			return "LifeformsAutoResearch";
		}
		public override Feature GetFeature() {
			return Feature.BrainLifeformAutoResearch;
		}

		public override LogSender GetLogSender() {
			return LogSender.Brain;
		}

		protected override async Task Execute() {
			try {
				DoLog(LogLevel.Information, "Running Lifeform autoresearch...");

				if (((bool) _tbotInstance.InstanceSettings.Brain.Active && (bool) _tbotInstance.InstanceSettings.Brain.LifeformAutoResearch.Active)) {
					AutoMinerSettings autoMinerSettings = new() {
						DeutToLeaveOnMoons = (int) _tbotInstance.InstanceSettings.Brain.AutoMine.DeutToLeaveOnMoons
					};
					int maxResearchLevel = (int) _tbotInstance.InstanceSettings.Brain.LifeformAutoResearch.MaxResearchLevel;
					List<Celestial> celestialsToMine = new();
					LFBuildings maxLFBuildings = new();

					foreach (Celestial celestial in _tbotInstance.UserData.celestials.Where(p => p is Planet)) {
						var cel = await _tbotOgameBridge.UpdatePlanet(celestial, UpdateTypes.LFBuildings);
						cel = await _tbotOgameBridge.UpdatePlanet(celestial, UpdateTypes.LFTechs);
						cel = await _tbotOgameBridge.UpdatePlanet(celestial, UpdateTypes.Resources);

						if (cel.LFtype == LFTypes.None) {
							DoLog(LogLevel.Information, $"Skipping {cel.ToString()}: No Lifeform active on this planet.");
							continue;
						}
						var nextLFTechToBuild = _calculationService.GetNextLFTechToBuild(cel, maxResearchLevel);
						if (nextLFTechToBuild != LFTechno.None) {
							var level = _calculationService.GetNextLevel(cel, nextLFTechToBuild);
							Resources nextLFTechCost = await _ogameService.GetPrice(nextLFTechToBuild, level);
							var isLessCostLFTechToBuild = await _calculationService.GetLessExpensiveLFTechToBuild(cel, nextLFTechCost, maxResearchLevel);
							if (isLessCostLFTechToBuild != LFTechno.None) {
								level = _calculationService.GetNextLevel(cel, isLessCostLFTechToBuild);
								nextLFTechToBuild = isLessCostLFTechToBuild;
							}

							DoLog(LogLevel.Debug, $"Celestial {cel.ToString()}: Next Lifeform Research: {nextLFTechToBuild.ToString()} lv {level.ToString()}.");
							celestialsToMine.Add(celestial);
						} else {
							DoLog(LogLevel.Debug, $"Celestial {cel.ToString()}: No Next Lifeform technoDoLogy to build found. All research reached _tbotInstance.InstanceSettings MaxResearchLevel ?");
						}

					}
					foreach (Celestial celestial in celestialsToMine) {
						await LifeformAutoResearchCelestial(celestial);
					}
				} else {
					DoLog(LogLevel.Information, "Skipping: feature disabled");
				}
			} catch (Exception e) {
				DoLog(LogLevel.Error, $"Lifeform AutoMine Exception: {e.Message}");
				DoLog(LogLevel.Warning, $"Stacktrace: {e.StackTrace}");
			} finally {
				if (!_tbotInstance.UserData.isSleeping) {
					await _tbotOgameBridge.CheckCelestials();
				}
			}
		}

		private async Task LifeformAutoResearchCelestial(Celestial celestial) {
			int fleetId = (int) SendFleetCode.GenericError;
			LFTechno buildable = LFTechno.None;
			int level = 0;
			bool started = false;
			bool stop = false;
			bool delay = false;
			bool delayProduction = false;
			long delayTime = 0;
			long interval = 0;
			try {
				DoLog(LogLevel.Information, $"Running Lifeform AutoResearch on {celestial.ToString()}");
				celestial = await _tbotOgameBridge.UpdatePlanet(celestial, UpdateTypes.Fast);
				celestial = await _tbotOgameBridge.UpdatePlanet(celestial, UpdateTypes.Resources);
				celestial = await _tbotOgameBridge.UpdatePlanet(celestial, UpdateTypes.LFTechs);
				celestial = await _tbotOgameBridge.UpdatePlanet(celestial, UpdateTypes.Constructions);

				if (celestial.Constructions.LFResearchID != 0) {
					DoLog(LogLevel.Information, $"Skipping {celestial.ToString()}: there is already a Lifeform research in production.");
					delayProduction = true;
					delayTime = (long) celestial.Constructions.LFResearchCountdown * (long) 1000 + (long) RandomizeHelper.CalcRandomInterval(IntervalType.AFewSeconds);
					return;
				}
				int maxResearchLevel = (int) _tbotInstance.InstanceSettings.Brain.LifeformAutoResearch.MaxResearchLevel;
				if (celestial is Planet) {
					buildable = _calculationService.GetNextLFTechToBuild(celestial, maxResearchLevel);

					if (buildable != LFTechno.None) {
						level = _calculationService.GetNextLevel(celestial, buildable);
						Resources nextLFTechCost = await _ogameService.GetPrice(buildable, level);
						var isLessCostLFTechToBuild = await _calculationService.GetLessExpensiveLFTechToBuild(celestial, nextLFTechCost, maxResearchLevel);
						if (isLessCostLFTechToBuild != LFTechno.None) {
							level = _calculationService.GetNextLevel(celestial, isLessCostLFTechToBuild);
							buildable = isLessCostLFTechToBuild;
						}
						DoLog(LogLevel.Information, $"Best Lifeform Research for {celestial.ToString()}: {buildable.ToString()}");

						Resources xCostBuildable = await _ogameService.GetPrice(buildable, level);

						if (celestial.Resources.IsEnoughFor(xCostBuildable)) {
							DoLog(LogLevel.Information, $"Lifeform Research {buildable.ToString()} level {level.ToString()} on {celestial.ToString()}");
							try {
								await _ogameService.BuildCancelable(celestial, (LFTechno) buildable);
								celestial = await _tbotOgameBridge.UpdatePlanet(celestial, UpdateTypes.Constructions);
								if (celestial.Constructions.LFResearchID == (int) buildable) {
									started = true;
									DoLog(LogLevel.Information, "Lifeform Research succesfully started.");
								} else {
									celestial = await _tbotOgameBridge.UpdatePlanet(celestial, UpdateTypes.LFTechs);
									if (celestial.GetLevel(buildable) != level)
										DoLog(LogLevel.Warning, "Unable to start Lifeform Research construction: an unknown error has occurred");
									else {
										started = true;
										DoLog(LogLevel.Information, "Lifeform Research succesfully started.");
									}
								}

							} catch {
								DoLog(LogLevel.Warning, "Unable to start Lifeform Research: a network error has occurred");
							}
						} else {
							DoLog(LogLevel.Information, $"Not enough resources to build: {buildable.ToString()} level {level.ToString()} on {celestial.ToString()}. Needed: {xCostBuildable.LFBuildingCostResources.ToString()} - Available: {celestial.Resources.LFBuildingCostResources.ToString()}");

							if ((bool) _tbotInstance.InstanceSettings.Brain.LifeformAutoResearch.Transports.Active) {
								_tbotInstance.UserData.fleets = await _fleetScheduler.UpdateFleets();
								if (!_calculationService.IsThereTransportTowardsCelestial(celestial, _tbotInstance.UserData.fleets)) {
									Celestial origin = _tbotInstance.UserData.celestials
											.Unique()
											.Where(c => c.Coordinate.Galaxy == (int) _tbotInstance.InstanceSettings.Brain.AutoMine.Transports.Origin.Galaxy)
											.Where(c => c.Coordinate.System == (int) _tbotInstance.InstanceSettings.Brain.AutoMine.Transports.Origin.System)
											.Where(c => c.Coordinate.Position == (int) _tbotInstance.InstanceSettings.Brain.AutoMine.Transports.Origin.Position)
											.Where(c => c.Coordinate.Type == Enum.Parse<Celestials>((string) _tbotInstance.InstanceSettings.Brain.AutoMine.Transports.Origin.Type))
											.SingleOrDefault() ?? new() { ID = 0 };
									fleetId = await _autoMineWorker.HandleMinerTransport(origin, celestial, xCostBuildable);
									if (fleetId == (int) SendFleetCode.AfterSleepTime) {
										stop = true;
										return;
									}
									if (fleetId == (int) SendFleetCode.NotEnoughSlots) {
										delay = true;
										return;
									}
								} else {
									DoLog(LogLevel.Information, $"Skipping transport: there is already a transport incoming in {celestial.ToString()}");
								}
							}
						}
					} else {
						DoLog(LogLevel.Information, $"Skipping {celestial.ToString()}: nothing to build. All research reached _tbotInstance.InstanceSettings MaxResearchLevel ?");
						stop = true;
					}
				}
			} catch (Exception e) {
				DoLog(LogLevel.Error, $"LifeformAutoResearch Celestial Exception: {e.Message}");
				DoLog(LogLevel.Warning, $"Stacktrace: {e.StackTrace}");
			} finally {
				var time = await _tbotOgameBridge.GetDateTime();
				string autoMineTimer = $"LifeformsAutoResearch{celestial.ID.ToString()}";
				DateTime newTime;
				if (stop) {
					DoLog(LogLevel.Information, $"Stopping Lifeform AutoResearch check for {celestial.ToString()}.");
					if (timers.TryGetValue($"LifeformsAutoResearch{celestial.ID.ToString()}", out Timer value))
						value.Dispose();
					timers.Remove(autoMineTimer);
				} else if (delayProduction) {
					DoLog(LogLevel.Information, $"Delaying...");
					time = await _tbotOgameBridge.GetDateTime();
					newTime = time.AddMilliseconds(delayTime);
					ChangeWorkerPeriod(delayTime);
					DoLog(LogLevel.Information, $"Next Lifeform Research check for {celestial.ToString()} at {newTime.ToString()}");
				} else if (delay) {
					DoLog(LogLevel.Information, $"Delaying...");
					time = await _tbotOgameBridge.GetDateTime();
					_tbotInstance.UserData.fleets = await _fleetScheduler.UpdateFleets();
					try {
						interval = (_tbotInstance.UserData.fleets.OrderBy(f => f.BackIn).First().BackIn ?? 0) * 1000 + RandomizeHelper.CalcRandomInterval(IntervalType.SomeSeconds);
					} catch {
						interval = RandomizeHelper.CalcRandomInterval((int) _tbotInstance.InstanceSettings.Brain.LifeformAutoResearch.CheckIntervalMin, (int) _tbotInstance.InstanceSettings.Brain.LifeformAutoResearch.CheckIntervalMax);
					}
					newTime = time.AddMilliseconds(interval);
					ChangeWorkerPeriod(interval);
					DoLog(LogLevel.Information, $"Next Lifeform AutoResearch check for {celestial.ToString()} at {newTime.ToString()}");

				} else if (started) {
					interval = ((long) celestial.Constructions.LFResearchCountdown * (long) 1000) + (long) RandomizeHelper.CalcRandomInterval(IntervalType.AFewSeconds);
					if (interval == long.MaxValue || interval == long.MinValue)
						interval = RandomizeHelper.CalcRandomInterval((int) _tbotInstance.InstanceSettings.Brain.LifeformAutoResearch.CheckIntervalMin, (int) _tbotInstance.InstanceSettings.Brain.LifeformAutoResearch.CheckIntervalMax);

					newTime = time.AddMilliseconds(interval);
					ChangeWorkerPeriod(interval);
					DoLog(LogLevel.Information, $"Next Lifeform AutoResearch check for {celestial.ToString()} at {newTime.ToString()}");
				} else {
					if (fleetId != 0 && fleetId != -1 && fleetId != -2) {
						_tbotInstance.UserData.fleets = await _fleetScheduler.UpdateFleets();
						var transportfleet = _tbotInstance.UserData.fleets.Single(f => f.ID == fleetId && f.Mission == Missions.Transport);
						interval = (transportfleet.ArriveIn * 1000) + RandomizeHelper.CalcRandomInterval(IntervalType.SomeSeconds);
					} else {
						interval = RandomizeHelper.CalcRandomInterval((int) _tbotInstance.InstanceSettings.Brain.LifeformAutoMine.CheckIntervalMin, (int) _tbotInstance.InstanceSettings.Brain.LifeformAutoMine.CheckIntervalMax);
					}

					newTime = time.AddMilliseconds(interval);
					ChangeWorkerPeriod(interval);
					DoLog(LogLevel.Information, $"Next Lifeform AutoResearch check for {celestial.ToString()} at {newTime.ToString()}");
				}
			}
		}

	}
}
