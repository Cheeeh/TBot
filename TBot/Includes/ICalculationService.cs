using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TBot.Model;
using TBot.Ogame.Infrastructure;
using TBot.Ogame.Infrastructure.Enums;
using TBot.Ogame.Infrastructure.Models;

namespace Tbot.Includes {
	public interface ICalculationService {
		bool AreThereIncomingResources(Celestial celestial, List<Fleet> fleets);
		long CalcCrystalProduction(Buildings buildings, int position, int speedFactor, float ratio = 1, Researches researches = null, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, int crawlers = 0, float crawlerRatio = 1);
		long CalcCrystalProduction(int level, int position, int speedFactor, float ratio = 1, int plasma = 0, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, int crawlers = 0, float crawlerRatio = 1);
		long CalcCrystalProduction(Planet planet, int speedFactor, float ratio = 1, Researches researches = null, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, int crawlers = 0, float crawlerRatio = 1);
		int CalcCumulativeLabLevel(List<Celestial> celestials, Researches researches);
		float CalcDaysOfInvestmentReturn(Planet planet, Buildables buildable, Researches researches = null, int speedFactor = 1, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false);
		long CalcDepositCapacity(int level);
		long CalcDeuteriumProduction(Buildings buildings, Temperature temp, int speedFactor, float ratio = 1, Researches researches = null, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, int crawlers = 0, float crawlerRatio = 1);
		long CalcDeuteriumProduction(int level, float temp, int speedFactor, float ratio = 1, int plasma = 0, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, int crawlers = 0, float crawlerRatio = 1);
		long CalcDeuteriumProduction(Planet planet, int speedFactor, float ratio = 1, Researches researches = null, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, int crawlers = 0, float crawlerRatio = 1);
		int CalcDistance(Coordinate origin, Coordinate destination, int galaxiesNumber, int systemsNumber = 499, bool donutGalaxy = true, bool donutSystem = true);
		int CalcDistance(Coordinate origin, Coordinate destination, ServerData serverData);
		long CalcEnergyProduction(Buildables buildable, int level, int energyTechnology = 0, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasEngineer = false, bool hasStaff = false);
		long CalcEnergyProduction(Buildables buildable, int level, Researches researches, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasEngineer = false, bool hasStaff = false);
		Ships CalcExpeditionShips(Ships fleet, Buildables primaryShip, int expeditionsNumber, int hyperspaceTech, float expeditionResourcesBonus, Dictionary<int, LFBonusesShip> shipsBonus, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass, int probeCargo = 0);
		Ships CalcExpeditionShips(Ships fleet, Buildables primaryShip, int expeditionsNumber, ServerData serverdata, Researches researches, LFBonuses LFBonuses, CharacterClass playerClass = CharacterClass.NoClass, int probeCargo = 0);
		long CalcFleetCapacity(Ships fleet, ServerData serverData, int hyperspaceTech = 0, CharacterClass playerClass = CharacterClass.NoClass, int probeCargo = 0);
		long CalcFleetFuelCapacity(Ships fleet, ServerData serverData, int hyperspaceTech = 0, CharacterClass playerClass = CharacterClass.NoClass, int probeCargo = 0);
		FleetPrediction CalcFleetPrediction(Celestial origin, Coordinate destination, Ships ships, Missions mission, decimal speed, Researches researches, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass);
		FleetPrediction CalcFleetPrediction(Coordinate origin, Coordinate destination, Ships ships, Missions mission, decimal speed, Researches researches, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass);
		int CalcFleetSpeed(Ships fleet, int combustionDrive, int impulseDrive, int hyperspaceDrive, CharacterClass playerClass = CharacterClass.NoClass);
		int CalcFleetSpeed(Ships fleet, Researches researches, CharacterClass playerClass = CharacterClass.NoClass);
		long CalcFlightTime(Coordinate origin, Coordinate destination, Ships ships, decimal speed, int combustionDrive, int impulseDrive, int hyperspaceDrive, int numberOfGalaxies, int numberOfSystems, bool donutGalaxies, bool donutSystems, int fleetSpeed, CharacterClass playerClass = CharacterClass.NoClass);
		long CalcFlightTime(Coordinate origin, Coordinate destination, Ships ships, Missions mission, decimal speed, Researches researches, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass);
		long CalcFuelConsumption(Coordinate origin, Coordinate destination, Ships ships, long flightTime, int combustionDrive, int impulseDrive, int hyperspaceDrive, int numberOfGalaxies, int numberOfSystems, bool donutGalaxies, bool donutSystems, int fleetSpeed, float deuteriumSaveFactor, CharacterClass playerClass = CharacterClass.NoClass);
		long CalcFuelConsumption(Coordinate origin, Coordinate destination, Ships ships, Missions mission, long flightTime, Researches researches, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass);
		Ships CalcFullExpeditionShips(Ships fleet, Buildables primaryShip, int expeditionsNumber, ServerData serverdata, Researches researches, LFBonuses LFBonuses, CharacterClass playerClass = CharacterClass.NoClass, int probeCargo = 0);
		Ships CalcIdealExpeditionShips(Buildables buildable, int hyperspaceTech, float expeditionResourcesBonus, Dictionary<int, LFBonusesShip> shipsBonus, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass, int probeCargo = 0);
		long CalcMaxBuildableNumber(Buildables buildable, Resources resources);
		int CalcMaxCrawlers(Planet planet, CharacterClass userClass, bool hasGeologist);
		int CalcMaxExpeditions(int astrophysics, bool isDiscoverer, bool hasAdmiral);
		int CalcMaxExpeditions(Researches researches, CharacterClass playerClass, Staff staff);
		int CalcMaxPlanets(int astrophysics);
		int CalcMaxPlanets(Researches researches);
		Resources CalcMaxTransportableResources(Ships ships, Resources resources, int hyperspaceTech, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass, long deutToLeave = 0, int probeCargo = 0);
		long CalcMetalProduction(Buildings buildings, int position, int speedFactor, float ratio = 1, Researches researches = null, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, int crawlers = 0, float crawlerRatio = 1);
		long CalcMetalProduction(int level, int position, int speedFactor, float ratio = 1, int plasma = 0, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, int crawlers = 0, float crawlerRatio = 1);
		long CalcMetalProduction(Planet planet, int speedFactor, float ratio = 1, Researches researches = null, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, int crawlers = 0, float crawlerRatio = 1);
		Buildables CalcMilitaryShipForExpedition(Ships fleet, int expeditionsNumber);
		int CalcNeededSolarSatellites(Planet planet, long requiredEnergy = 0, bool isCollector = false, bool hasEngineer = false, bool hasFullStaff = false);
		float CalcNextAstroDOIR(List<Planet> planets, Researches researches, int speedFactor = 1, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false);
		float CalcNextDaysOfInvestmentReturn(Planet planet, Researches researches = null, int speedFactor = 1, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false);
		float CalcNextPlasmaTechDOIR(List<Planet> planets, Researches researches, int speedFactor = 1, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false);
		int CalcOptimalCrawlers(Planet planet, CharacterClass userClass, Staff staff, Researches researches, ServerData serverData);
		decimal CalcOptimalFarmSpeed(Celestial origin, Coordinate destination, Ships ships, Resources loot, decimal ratio, long maxFlightTime, Researches researches, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass);
		decimal CalcOptimalFarmSpeed(Coordinate origin, Coordinate destination, Ships ships, Resources loot, decimal ratio, long maxFlightTime, Researches researches, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass);
		Resources CalcPlanetHourlyProduction(Planet planet, int speedFactor, float ratio = 1, Researches researches = null, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, int crawlers = 0, float crawlerRatio = 1);
		Resources CalcPrice(Buildables buildable, int level);
		Resources CalcPrice(LFBuildables buildable, int level, double costReduction = 0, double energyCostReduction = 0, double populationCostReduction = 0);
		Resources CalcPrice(LFTechno buildable, int level, double costReduction = 0);
		long CalcProductionTime(Buildables buildable, int level, int speed = 1, Facilities facilities = null, int cumulativeLabLevel = 0, bool isDiscoverer = false, bool hasTechnocrat = false);
		long CalcProductionTime(Buildables buildable, int level, ServerData serverData, Facilities facilities, int cumulativeLabLevel = 0);
		long CalcProductionTime(LFBuildables buildable, int level, int speed = 1, Facilities facilities = null);
		long CalcProductionTime(LFBuildables buildable, int level, ServerData serverData, Facilities facilities = null);
		long CalcProductionTime(LFTechno buildable, int level, int speed = 1, double costReduction = 0);
		long CalcProductionTime(LFTechno buildable, int level, ServerData serverData, double costReduction = 0);
		float CalcROI(Planet planet, Buildables buildable, Researches researches = null, int speedFactor = 1, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false);
		int CalcShipCapacity(Buildables buildable, int hyperspaceTech, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass, int probeCargo = 0);
		int CalcShipCapacity(Buildables buildable, int hyperspaceTech, float buildableCargoBonus, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass, int probeCargo = 0);
		int CalcShipConsumption(Buildables buildable, int impulseDrive, int hyperspaceDrive, double deuteriumSaveFactor, CharacterClass playerClass = CharacterClass.NoClass);
		int CalcShipConsumption(Buildables buildable, Researches researches, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass);
		int CalcShipFuelCapacity(Buildables buildable, ServerData serverData,  int hyperspaceTech = 0, CharacterClass playerClass = CharacterClass.NoClass, int probeCargo = 0);
		long CalcShipNumberForPayload(Resources payload, Buildables buildable, int hyperspaceTech, ServerData serverData, CharacterClass playerClass = CharacterClass.NoClass, int probeCapacity = 0);
		int CalcShipSpeed(Buildables buildable, int combustionDrive, int impulseDrive, int hyperspaceDrive, CharacterClass playerClass = CharacterClass.NoClass);
		int CalcShipSpeed(Buildables buildable, Researches researches, CharacterClass playerClass = CharacterClass.NoClass);
		int CalcSlowestSpeed(Ships fleet, int combustionDrive, int impulseDrive, int hyperspaceDrive, CharacterClass playerClass = CharacterClass.NoClass);
		int CalcSlowestSpeed(Ships fleet, Researches researches, CharacterClass playerClass = CharacterClass.NoClass);
		Fleet GetFirstReturningEspionage(Coordinate origin, List<Fleet> fleets);
		Fleet GetFirstReturningEspionage(List<Fleet> fleets);
		Fleet GetLastReturningEspionage(List<Fleet> fleets);
		Fleet GetFirstReturningExpedition(Coordinate coord, List<Fleet> fleets);
		List<Fleet> GetIncomingFleets(Celestial celestial, List<Fleet> fleets);
		List<Fleet> GetIncomingFleetsWithResources(Celestial celestial, List<Fleet> fleets);
		LFTechno GetLessExpensiveLFTechToBuild(Celestial celestial, Resources currentcost, int MaxReasearchLevel, double costReduction = 0);
		Buildings GetMaxBuildings(int maxMetalMine, int maxCrystalMine, int maxDeuteriumSynthetizer, int maxSolarPlant, int maxFusionReactor, int maxMetalStorage, int maxCrystalStorage, int maxDeuteriumTank);
		Facilities GetMaxFacilities(int maxRoboticsFactory, int maxShipyard, int maxResearchLab, int maxMissileSilo, int maxNaniteFactory, int maxTerraformer, int maxSpaceDock);
		Facilities GetMaxLunarFacilities(int maxLunarBase, int maxLunarShipyard, int maxLunarRoboticsFactory, int maxSensorPhalanx, int maxJumpGate);
		List<Fleet> GetMissionsInProgress(Coordinate origin, Missions mission, List<Fleet> fleets);
		List<Fleet> GetMissionsInProgress(Missions mission, List<Fleet> fleets);
		Buildables GetNextBuildingToBuild(Planet planet, Researches researches, Buildings maxBuildings, Facilities maxFacilities, CharacterClass playerClass, Staff staff, ServerData serverData, AutoMinerSettings settings, float ratio = 1);
		Buildables GetNextDepositToBuild(Planet planet, Researches researches, Buildings maxBuildings, CharacterClass playerClass, Staff staff, ServerData serverData, AutoMinerSettings settings, float ratio = 1);
		Buildables GetNextEnergySourceToBuild(Planet planet, int maxSolarPlant, int maxFusionReactor, bool buildSolarSatellites = true);
		Buildables GetNextFacilityToBuild(Planet planet, Researches researches, Buildings maxBuildings, Facilities maxFacilities, CharacterClass playerClass, Staff staff, ServerData serverData, AutoMinerSettings settings, float ratio = 1, bool force = false);
		int GetNextLevel(Celestial planet, Buildables buildable, bool isCollector = false, bool hasEngineer = false, bool hasFullStaff = false);
		int GetNextLevel(Celestial planet, LFBuildables buildable);
		int GetNextLevel(Celestial planet, LFTechno buildable);
		int GetNextLevel(Researches researches, Buildables buildable);
		LFBuildables GetNextLFBuildingToBuild(Celestial planet, int maxPopuFactory = 100, int maxFoodFactory = 100, int maxTechFactory = 20, bool preventIfMoreExpensiveThanNextMine = false);
		LFBuildables GetPopulationBuilding(LFTypes LFtype);
		LFBuildables GetFoodBuilding(LFTypes LFtype);
		LFBuildables GetTechBuilding(LFTypes LFtype);
		LFBuildables GetT2Building(LFTypes LFtype);
		LFBuildables GetT3Building(LFTypes LFtype);
		LFBuildables GetLeastExpensiveLFBuilding(Celestial planet);
		List<LFBuildables> GetOtherBuildings(LFTypes LFtype);
		long CalcFoodProduction(Planet planet);
		long CalcFoodProduction(LFBuildables foodFactory, int level, double bonus = 0);
		double CalcFoodProductionBonus(Planet planet);
		long CalcLivingSpace(Planet planet);
		long CalcLivingSpace(LFBuildables populationFactory, int level, double bonus = 0);
		double CalcLivingSpaceBonus(Planet planet);
		long CalcFoodConsumption(Planet planet);
		long CalcFoodConsumption(LFBuildables populationFactory, int level, double bonus = 0);
		double CalcFoodConsumptionBonus(Planet planet);
		long CalcSatisfied(Planet planet);
		long CalcSatisfied(LFBuildables populationFactory, int populationFactoryLevel, LFBuildables foodFactory, int foodFactoryLevel, double populationBonus = 0, double foodProductionBonus = 0, double foodConsumptionBonus = 0);
		LFTechno GetNextLFTechToBuild(Celestial celestial, int MaxReasearchLevel);
		Buildables GetNextLunarFacilityToBuild(Moon moon, Researches researches, int maxLunarBase = 8, int maxRoboticsFactory = 8, int maxSensorPhalanx = 6, int maxJumpGate = 1, int maxShipyard = 0);
		Buildables GetNextLunarFacilityToBuild(Moon moon, Researches researches, Facilities maxLunarFacilities);
		Buildables GetNextMineToBuild(Planet planet, int maxMetalMine = 100, int maxCrystalMine = 100, int maxDeuteriumSynthetizer = 100, bool optimizeForStart = true);
		Buildables GetNextMineToBuild(Planet planet, Researches researches = null, int speedFactor = 1, int maxMetalMine = 100, int maxCrystalMine = 100, int maxDeuteriumSynthetizer = 100, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, bool optimizeForStart = true, float maxDaysOfInvestmentReturn = 36500);
		Buildables GetNextMineToBuild(Planet planet, Researches researches, Buildings maxBuildings, Facilities maxFacilities, CharacterClass playerClass, Staff staff, ServerData serverData, AutoMinerSettings settings, float ratio = 1);
		Buildables GetNextResearchToBuild(Planet celestial, Researches researches, bool prioritizeRobotsAndNanitesOnNewPlanets = false, Slots slots = null, int maxEnergyTechnology = 20, int maxLaserTechnology = 12, int maxIonTechnology = 5, int maxHyperspaceTechnology = 20, int maxPlasmaTechnology = 20, int maxCombustionDrive = 19, int maxImpulseDrive = 17, int maxHyperspaceDrive = 15, int maxEspionageTechnology = 8, int maxComputerTechnology = 20, int maxAstrophysics = 23, int maxIntergalacticResearchNetwork = 12, int maxWeaponsTechnology = 25, int maxShieldingTechnology = 25, int maxArmourTechnology = 25, bool optimizeForStart = true, bool ensureExpoSlots = true, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasAdmiral = false);
		long GetProductionEnergyDelta(Buildables buildable, int level, int energyTechnology = 0, float ratio = 1, CharacterClass userClass = CharacterClass.NoClass, bool hasEngineer = false, bool hasStaff = false);
		long GetRequiredEnergyDelta(Buildables buildable, int level);
		int GetSolarSatelliteOutput(Planet planet, bool isCollector = false, bool hasEngineer = false, bool hasFullStaff = false);
		List<decimal> GetValidSpeedsForClass(CharacterClass playerClass);
		bool IsThereTransportTowardsCelestial(Celestial celestial, List<Fleet> fleets);
		bool isUnlocked(Celestial celestial, LFBuildables buildable);
		bool MayAddShipToExpedition(Ships fleet, Buildables buildable, int expeditionsNumber);
		List<Celestial> ParseCelestialsList(dynamic source, List<Celestial> currentCelestials);
		bool ShouldBuildCrystalStorage(Planet planet, int maxLevel, int speedFactor, int hours = 12, float ratio = 1, Researches researches = null, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, bool forceIfFull = false);
		bool ShouldBuildDeuteriumTank(Planet planet, int maxLevel, int speedFactor, int hours = 12, float ratio = 1, Researches researches = null, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, bool forceIfFull = false);
		bool ShouldBuildEnergySource(Planet planet);
		bool ShouldBuildJumpGate(Moon moon, int maxLevel = 1, Researches researches = null);
		bool ShouldBuildLunarBase(Moon moon, int maxLevel = 8);
		bool ShouldBuildMetalStorage(Planet planet, int maxLevel, int speedFactor, int hours = 12, float ratio = 1, Researches researches = null, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, bool forceIfFull = false);
		bool ShouldBuildMissileSilo(Planet celestial, int maxLevel = 6, Researches researches = null, int speedFactor = 1, int maxMetalMine = 100, int maxCrystalMine = 100, int maxDeuteriumSynthetizer = 100, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, bool optimizeForStart = true, float maxDaysOfInvestmentReturn = 36500, bool force = false);
		bool ShouldBuildNanites(Planet celestial, int maxLevel = 10, Researches researches = null, int speedFactor = 1, int maxMetalMine = 100, int maxCrystalMine = 100, int maxDeuteriumSynthetizer = 100, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, bool optimizeForStart = true, float maxDaysOfInvestmentReturn = 36500, bool force = false);
		bool ShouldBuildResearchLab(Planet celestial, int maxLevel = 12, Researches researches = null, int speedFactor = 1, float researchDurationDivisor = 2, int maxMetalMine = 100, int maxCrystalMine = 100, int maxDeuteriumSynthetizer = 100, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, bool optimizeForStart = true, float maxDaysOfInvestmentReturn = 36500, bool force = false);
		bool ShouldBuildRoboticFactory(Celestial celestial, int maxLevel = 10, Researches researches = null, int speedFactor = 1, int maxMetalMine = 100, int maxCrystalMine = 100, int maxDeuteriumSynthetizer = 100, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, bool optimizeForStart = true, float maxDaysOfInvestmentReturn = 36500, bool force = false);
		bool ShouldBuildSensorPhalanx(Moon moon, int maxLevel = 7);
		bool ShouldBuildShipyard(Celestial celestial, int maxLevel = 12, Researches researches = null, int speedFactor = 1, int maxMetalMine = 100, int maxCrystalMine = 100, int maxDeuteriumSynthetizer = 100, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, bool optimizeForStart = true, float maxDaysOfInvestmentReturn = 36500, bool force = false);
		bool ShouldBuildSpaceDock(Planet celestial, int maxLevel = 10, Researches researches = null, int speedFactor = 1, int maxMetalMine = 100, int maxCrystalMine = 100, int maxDeuteriumSynthetizer = 100, float ratio = 1, CharacterClass playerClass = CharacterClass.NoClass, bool hasGeologist = false, bool hasStaff = false, bool optimizeForStart = true, float maxDaysOfInvestmentReturn = 36500, bool force = false);
		bool ShouldBuildTerraformer(Planet celestial, Researches researches, int maxLevel = 10);
		bool ShouldResearchEnergyTech(List<Planet> planets, int energyTech, int maxEnergyTech = 25, CharacterClass playerClass = CharacterClass.NoClass, bool hasEngineer = false, bool hasStaff = false);
		bool ShouldResearchEnergyTech(List<Planet> planets, Researches researches, int maxEnergyTech = 25, CharacterClass playerClass = CharacterClass.NoClass, bool hasEngineer = false, bool hasStaff = false);
		bool ShouldAbandon(Planet celestial, int maxCases, int Temperature, Fields fieldsSettings, Temperature temperaturesSettings);
		int CountPlanetsInRange(List<Planet> planets, int galaxy, int minSystem, int maxSystem, int minPosition, int maxPositions, int minSlots, int minTemperature, int maxTemperature);
		int CountPlanetsInRange(List<Celestial> planets, int galaxy, int minSystem, int maxSystem, int minPosition, int maxPositions, int minSlots, int minTemperature, int maxTemperature);

	}

}
