using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.Initialization;
using TSMapEditor.Models.ArtConfig;

namespace TSMapEditor.Models
{
    public class Rules
    {
        public List<UnitType> UnitTypes = new List<UnitType>();
        public List<InfantryType> InfantryTypes = new List<InfantryType>();
        public List<BuildingType> BuildingTypes = new List<BuildingType>();
        public List<AircraftType> AircraftTypes = new List<AircraftType>();
        public List<TerrainType> TerrainTypes = new List<TerrainType>();
        public List<OverlayType> OverlayTypes = new List<OverlayType>();
        public List<SmudgeType> SmudgeTypes = new List<SmudgeType>();
        public List<HouseType> Countries = new List<HouseType>();

        public List<string> Sides = new List<string>();
        public List<InfantrySequence> InfantrySequences = new List<InfantrySequence>();
        public List<RulesColor> Colors = new List<RulesColor>();
        public List<TiberiumType> TiberiumTypes = new List<TiberiumType>();
        public List<AnimType> AnimTypes = new List<AnimType>();
        public List<GlobalVariable> GlobalVariables = new List<GlobalVariable>();
        public List<Weapon> Weapons = new List<Weapon>();

        public TutorialLines TutorialLines { get; set; }
        public Themes Themes { get; set; }

        /// <summary>
        /// Initializes rules types from an INI file.
        /// </summary>
        public void InitFromINI(IniFile iniFile, IInitializer initializer, bool isMapIni = false)
        {
            InitFromTypeSection(iniFile, "VehicleTypes", UnitTypes);
            InitFromTypeSection(iniFile, "InfantryTypes", InfantryTypes);
            InitFromTypeSection(iniFile, "BuildingTypes", BuildingTypes);
            InitFromTypeSection(iniFile, "AircraftTypes", AircraftTypes);
            InitFromTypeSection(iniFile, "TerrainTypes", TerrainTypes);
            InitFromTypeSection(iniFile, "OverlayTypes", OverlayTypes);
            InitFromTypeSection(iniFile, "SmudgeTypes", SmudgeTypes);
            InitFromTypeSection(iniFile, "Animations", AnimTypes);
            InitFromTypeSection(iniFile, "Weapons", Weapons);

            // Go through all the lists and get object properties
            UnitTypes.ForEach(ot => initializer.ReadObjectTypePropertiesFromINI(ot, iniFile));
            InfantryTypes.ForEach(ot => initializer.ReadObjectTypePropertiesFromINI(ot, iniFile));
            BuildingTypes.ForEach(ot => initializer.ReadObjectTypePropertiesFromINI(ot, iniFile));
            AircraftTypes.ForEach(ot => initializer.ReadObjectTypePropertiesFromINI(ot, iniFile));
            TerrainTypes.ForEach(ot => initializer.ReadObjectTypePropertiesFromINI(ot, iniFile));
            OverlayTypes.ForEach(ot => initializer.ReadObjectTypePropertiesFromINI(ot, iniFile));
            SmudgeTypes.ForEach(ot => initializer.ReadObjectTypePropertiesFromINI(ot, iniFile));
            Weapons.ForEach(w => initializer.ReadObjectTypePropertiesFromINI(w, iniFile));
            AnimTypes.ForEach(a => initializer.ReadObjectTypePropertiesFromINI(a, iniFile));

            InitColors(iniFile);

            if (!isMapIni)
            {
                InitFromTypeSection(iniFile, Constants.UseCountries ? "Countries" : "Houses", Countries);
                Countries.ForEach(ot => initializer.ReadObjectTypePropertiesFromINI(ot, iniFile));
                Countries.ForEach(ot => InitHouseType(ot, isMapIni));
            }

            InitTiberiums(iniFile, isMapIni);
            InitSides(iniFile, isMapIni);

            if (!isMapIni) // Don't load local variables defined in the map as globals
                InitGlobalVariables(iniFile);
        }

        public void InitArt(IniFile iniFile, IInitializer initializer)
        {
            TerrainTypes.ForEach(tt => initializer.ReadObjectTypeArtPropertiesFromINI(tt, iniFile));

            SmudgeTypes.ForEach(st => initializer.ReadObjectTypeArtPropertiesFromINI(st, iniFile));

            BuildingTypes.ForEach(bt => initializer.ReadObjectTypeArtPropertiesFromINI(bt, iniFile,
                string.IsNullOrWhiteSpace(bt.Image) ? bt.ININame : bt.Image));

            UnitTypes.ForEach(ut => initializer.ReadObjectTypeArtPropertiesFromINI(ut, iniFile,
                string.IsNullOrWhiteSpace(ut.Image) ? ut.ININame : ut.Image));

            InfantryTypes.ForEach(it => initializer.ReadObjectTypeArtPropertiesFromINI(it, iniFile,
                string.IsNullOrWhiteSpace(it.Image) ? it.ININame : it.Image));

            OverlayTypes.ForEach(ot => initializer.ReadObjectTypeArtPropertiesFromINI(ot, iniFile,
                string.IsNullOrWhiteSpace(ot.Image) ? ot.ININame : ot.Image));

            AnimTypes.ForEach(a => initializer.ReadObjectTypeArtPropertiesFromINI(a, iniFile, a.ININame));
        }

        public List<House> GetStandardHouses()
        {
            var houses = new List<House>();

            foreach (HouseType country in Countries)
            {
                houses.Add(GetStandardHouse(country));
            }

            return houses;
        }

        public House GetStandardHouse(HouseType country)
        {
            House house = new House(country.ININame)
            {
                Allies = country.ININame,
                Color = country.Color,
                XNAColor = country.XNAColor,
                Credits = 0,
                Edge = "North",
                IQ = 0,
                PercentBuilt = 100,
                PlayerControl = false,
                TechLevel = 10
            };

            if (Constants.UseCountries)
            {
                // RA2/YR Houses only have a country field
                house.Country = country.ININame;
            }
            else
            {
                // TS Houses contain ActsLike and Side
                int sideIndex = Sides.FindIndex(side => side == country.Side);
                if (sideIndex == -1)
                    sideIndex = 0;

                house.ActsLike = sideIndex;
                house.Side = country.Side;
            }

            return house;
        }

        public List<House> GetPlayerHouses()
        {
            List<House> houses = new List<House>();
            if (Constants.UseCountries)
            {
                string letters = "ABCDEFGH";
                for (int i = 0; i < 8; i++)
                {
                    House house = new House($"<Player @ {letters[i]}>")
                    {
                        Country = $"<Player @ {letters[i]}>",
                        IsPlayerHouse = true,
                        Color = "Grey",
                        XNAColor = Color.Gray
                    };

                    houses.Add(house);
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    House house = new House($"Spawn{i + 1}")
                    {
                        Country = $"Spawn{i + 1}",
                        IsPlayerHouse = true,
                        Color = "Grey",
                        XNAColor = Color.Gray
                    };

                    houses.Add(house);
                }
            }

            return houses;
        }

        public List<HouseType> GetPlayerCountries()
        {
            List<HouseType> countries = new List<HouseType>();
            if (Constants.UseCountries)
            {
                string letters = "ABCDEFGH";
                for (int i = 0; i < 8; i++)
                {
                    HouseType country = new HouseType($"<Player @ {letters[i]}>")
                    {
                        Index = 4475 + i,
                        IsPlayerHouse = true,
                        Color = "Grey",
                        XNAColor = Color.Gray
                    };

                    countries.Add(country);
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    HouseType country = new HouseType($"Spawn{i + 1}")
                    {
                        Index = 50 + i,
                        IsPlayerHouse = true,
                        Color = "Grey",
                        XNAColor = Color.Gray
                    };

                    countries.Add(country);
                }
            }

            return countries;
        }

        private void InitHouseType(HouseType houseType, bool isMapIni = false)
        {
            var color = Colors.Find(c => c.Name == houseType.Color);
            if (color == null)
                houseType.XNAColor = Color.Black;
            else
                houseType.XNAColor = color.XNAColor;
        }

        private void InitColors(IniFile iniFile)
        {
            var colorsSection = iniFile.GetSection("Colors");
            if (colorsSection != null)
            {
                foreach (var kvp in colorsSection.Keys)
                {
                    Colors.Add(new RulesColor(kvp.Key, kvp.Value));
                }
            }
        }

        private void InitTiberiums(IniFile iniFile, bool isMapIni)
        {
            var tiberiumsSection = iniFile.GetSection("Tiberiums");
            if (tiberiumsSection != null && !isMapIni)
            {
                for (int i = 0; i < tiberiumsSection.Keys.Count; i++)
                {
                    var kvp = tiberiumsSection.Keys[i];
                    var tiberiumType = new TiberiumType(kvp.Value, i);

                    var tiberiumTypeSection = iniFile.GetSection(kvp.Value);
                    if (tiberiumTypeSection != null)
                    {
                        tiberiumType.ReadPropertiesFromIniSection(tiberiumTypeSection);

                        TiberiumTypes.Add(tiberiumType);
                        var rulesColor = Colors.Find(c => c.Name == tiberiumType.Color);
                        if (rulesColor != null)
                            tiberiumType.XNAColor = rulesColor.XNAColor;
                    }
                }
            }
        }

        private void InitSides(IniFile iniFile, bool isMapIni)
        {
            var sidesSection = iniFile.GetSection("Sides");
            if (sidesSection != null)
            {
                foreach (var kvp in sidesSection.Keys)
                {
                    Sides.Add(kvp.Key);
                }
            }
        }

        private void InitGlobalVariables(IniFile iniFile)
        {
            IniSection variableNamesSection = iniFile.GetSection("VariableNames");
            if (variableNamesSection != null)
            {
                for (int i = 0; i < variableNamesSection.Keys.Count; i++)
                {
                    var kvp = variableNamesSection.Keys[i];
                    GlobalVariables.Add(new GlobalVariable(i, kvp.Value));
                }
            }
        }

        private void InitFromTypeSection<T>(IniFile iniFile, string sectionName, List<T> targetList)
        {
            var sectionKeys = iniFile.GetSectionKeys(sectionName);

            if (sectionKeys == null || sectionKeys.Count == 0)
                return;

            int i = targetList.Count;

            foreach (string key in sectionKeys)
            {
                string typeName = iniFile.GetStringValue(sectionName, key, null);

                var objectType = typeof(T);

                // We assume that the type has a constructor
                // that takes a single string (ININame) as a parameter
                var constructor = objectType.GetConstructor(new Type[] { typeof(string) });
                if (constructor == null)
                {
                    throw new InvalidOperationException(typeof(T).FullName +
                        " has no public constructor that takes a single string as an argument!");
                }

                T objectInstance = (T)constructor.Invoke(new object[] { typeName });

                // Set the index property if one exists
                var indexProperty = objectType.GetProperty("Index");
                if (indexProperty != null)
                    indexProperty.SetValue(objectInstance, i);

                targetList.Add(objectInstance);
                i++;
            }
        }

        public InfantrySequence FindOrMakeInfantrySequence(IniFile artIni, string infantrySequenceName)
        {
            var existing = InfantrySequences.Find(seq => seq.ININame == infantrySequenceName);
            if (existing == null)
            {
                existing = new InfantrySequence(infantrySequenceName);
                var section = artIni.GetSection(infantrySequenceName);
                if (section == null)
                    throw new KeyNotFoundException("Infantry sequence not found: " + infantrySequenceName);

                existing.ParseFromINISection(section);
                InfantrySequences.Add(existing);
            }

            return existing;
        }

        public TechnoType FindTechnoType(string technoTypeININame)
        {
            TechnoType returnValue = AircraftTypes.Find(at => at.ININame == technoTypeININame);

            if (returnValue != null)
                return returnValue;

            returnValue = BuildingTypes.Find(bt => bt.ININame == technoTypeININame);

            if (returnValue != null)
                return returnValue;

            returnValue = InfantryTypes.Find(it => it.ININame == technoTypeININame);

            if (returnValue != null)
                return returnValue;

            return UnitTypes.Find(ut => ut.ININame == technoTypeININame);
        }

        public void SolveDependencies()
        {
            //UnitTypes.ForEach(ot => SolveUnitTypeDependencies(ot));
            //InfantryTypes.ForEach(ot => SolveInfantryTypeDependencies(ot));
            BuildingTypes.ForEach(ot => SolveBuildingTypeDependencies(ot));
            //AircraftTypes.ForEach(ot => SolveAircraftTypeDependencies(ot));
            //TerrainTypes.ForEach(ot => SolveTerrainTypeDependencies(ot));
            //OverlayTypes.ForEach(ot => SolveOverlayTypeDependencies(ot));
            //SmudgeTypes.ForEach(ot => SolveSmudgeTypeDependencies(ot));
            //Weapons.ForEach(w => initializer.SolveWeaponDependencies(w))
            //AnimTypes.ForEach(a => SolveAnimTypeDependencies(a));
        }

        private void SolveBuildingTypeDependencies(BuildingType type)
        {
            var anims = new List<AnimType>();
            foreach (var animName in type.ArtConfig.AnimNames)
            {
                AnimType anim = AnimTypes.Find(at => at.ININame == animName);
                if (anim != null)
                {
                    anim.ArtConfig.IsBuildingAnim = true;
                    anims.Add(anim);
                }
            }
            type.ArtConfig.Anims = anims.ToArray();

            if (type.TurretAnim!= null && type.Turret && !type.TurretAnimIsVoxel)
            {
                AnimType turretAnim = AnimTypes.Find(at => at.ININame == type.TurretAnim);
                turretAnim.ArtConfig.IsBuildingAnim = true;
                type.ArtConfig.TurretAnim = turretAnim;
            }
        }
    }
}
