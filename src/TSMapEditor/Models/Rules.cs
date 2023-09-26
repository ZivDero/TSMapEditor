using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public List<HouseType> HouseTypes = new List<HouseType>();

        public List<string> Sides = new List<string>();
        public List<InfantrySequence> InfantrySequences = new List<InfantrySequence>();
        public List<RulesColor> Colors = new List<RulesColor>();
        public List<TiberiumType> TiberiumTypes = new List<TiberiumType>();
        public List<AnimType> AnimTypes = new List<AnimType>();
        public List<GlobalVariable> GlobalVariables = new List<GlobalVariable>();
        public List<Weapon> Weapons = new List<Weapon>();

        public Dictionary<string, string> PlayerHouseColors = new Dictionary<string, string>();
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
                InitFromTypeSection(iniFile, Constants.UseCountries ? "Countries" : "Houses", HouseTypes);
                HouseTypes.ForEach(ot => initializer.ReadObjectTypePropertiesFromINI(ot, iniFile));
                HouseTypes.ForEach(ot => InitHouseType(ot, isMapIni));
            }

            InitTiberiums(iniFile, isMapIni);
            InitSides(iniFile, isMapIni);

            // Don't load local variables defined in the map as globals
            if (!isMapIni)
                InitGlobalVariables(iniFile);
        }

        /// <summary>
        /// Makes a deep copy of the HouseTypes list.
        /// The original list of HouseTypes is not modified.
        /// </summary>
        /// <returns>List of standard HouseTypes</returns>
        public List<HouseType> GetStandardHouseTypes()
        {
            var houseTypes = new List<HouseType>();

            foreach (HouseType houseType in HouseTypes)
            {
                HouseType newHouseType = new HouseType(houseType, houseType.ININame);
                houseTypes.Add(newHouseType);
            }

            return houseTypes;
        }

        /// <summary>
        /// Returns a copy of the HouseType with the specified INI name.
        /// If no HouseType with such a name is found, null is returned.
        /// </summary>
        /// <param name="iniName"></param>
        /// <returns>Standard HouseType</returns>
        public HouseType GetStandardHouseType(string iniName)
        {
            var houseType = HouseTypes.Find(c => c.ININame == iniName);
            if (houseType == null)
                return null;

            return new HouseType(houseType, houseType.ININame);
        }

        /// <summary>
        /// Returns a deep copy of the list of houses based on HouseTypes from the rules.
        /// </summary>
        /// <returns>List of standard Houses</returns>
        public List<House> GetStandardHouses()
        {
            var houses = new List<House>();

            foreach (HouseType houseType in HouseTypes)
            {
                houses.Add(GetStandardHouse(houseType));
            }

            return houses;
        }

        /// <summary>
        /// Creates a new House object based on the specified HouseType.
        /// </summary>
        /// <param name="houseType"></param>
        /// <returns>Standard House</returns>
        public House GetStandardHouse(HouseType houseType)
        {
            House house = new House(houseType.ININame)
            {
                HouseType = houseType,
                Allies = houseType.ININame,
                Color = houseType.Color,
                XNAColor = houseType.XNAColor,
                Credits = 0,
                Edge = "North",
                IQ = 0,
                PercentBuilt = 100,
                PlayerControl = false,
                TechLevel = 10
            };

            if (Constants.UseCountries)
            {
                // RA2/YR Houses have a country field
                house.Country = houseType.ININame;
            }
            else
            {
                // TS Houses contain ActsLike
                house.ActsLike = houseType.Index;
            }

            return house;
        }

        public List<House> GetPlayerHouses()
        {
            List<House> houses = new List<House>();
            foreach (var houseType in GetPlayerHouseTypes())
            {
                var house = GetStandardHouse(houseType);
                house.IsPlayerHouse = true;
                houses.Add(house);
            }

            return houses;
        }

        public List<HouseType> GetPlayerHouseTypes()
        {
            List<HouseType> houseTypes = new List<HouseType>();
            int baseIndex = Constants.UseCountries ? 4475 : 50;
            const string letters = "ABCDEFGH";

            for (int i = 0; i < 8; i++)
            {
                string houseTypeName = Constants.UseCountries ? $"<Player @ {letters[i]}>" : $"Spawn{i + 1}";
                string colorName = PlayerHouseColors.TryGetValue(houseTypeName, out var c) ? c : "Grey";
                Color color = FindColor(colorName);

                HouseType houseType = new HouseType(houseTypeName)
                {
                    Index = baseIndex + i,
                    IsPlayerHouse = true,
                    Color = colorName,
                    XNAColor = color
                };

                houseTypes.Add(houseType);
            }

            return houseTypes;
        }

        private void InitHouseType(HouseType houseType, bool isMapIni = false)
        {
            houseType.XNAColor = FindColor(houseType.Color);
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
                foreach (var kvp in variableNamesSection.Keys)
                {
                    GlobalVariables.Add(new GlobalVariable(int.Parse(kvp.Key, CultureInfo.InvariantCulture), kvp.Value));
                }
            }
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

        public void InitEditorOverrides(IniFile iniFile)
        {
            List<GameObjectType> gameObjectTypes = new List<GameObjectType>();
            gameObjectTypes.AddRange(UnitTypes);
            gameObjectTypes.AddRange(InfantryTypes);
            gameObjectTypes.AddRange(BuildingTypes);
            gameObjectTypes.AddRange(AircraftTypes);
            gameObjectTypes.AddRange(TerrainTypes);
            gameObjectTypes.AddRange(OverlayTypes);
            gameObjectTypes.AddRange(SmudgeTypes);

            var section = iniFile.GetSection("ObjectCategoryOverrides");
            if (section != null)
            {
                foreach (var keyValuePair in section.Keys)
                {
                    var obj = gameObjectTypes.Find(o => o.ININame == keyValuePair.Key);
                    if (obj != null)
                        obj.EditorCategory = keyValuePair.Value;
                }
            }

            section = iniFile.GetSection("IgnoreTypes");
            if (section != null)
            {
                foreach (var keyValuePair in section.Keys)
                {
                    var obj = gameObjectTypes.Find(o => o.ININame == keyValuePair.Key);
                    if (obj != null)
                        obj.EditorVisible = !section.GetBooleanValue(keyValuePair.Key, !obj.EditorVisible);
                }
            }
        }

        public void InitPlayerHouseColors(IniFile iniFile)
        {
            var section = iniFile.GetSection("SpawnColors");
            if (section != null)
            {
                foreach (var keyValuePair in section.Keys)
                {
                    PlayerHouseColors.Add(keyValuePair.Key, keyValuePair.Value);
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

                T objectInstance = targetList.Find(o => o.GetType().GetProperty("ININame").GetValue(o).ToString() == typeName) ?? (T)constructor.Invoke(new object[] { typeName });

                if (!targetList.Contains(objectInstance))
                {
                    // Set the index property if one exists
                    var indexProperty = objectType.GetProperty("Index");
                    if (indexProperty != null)
                        indexProperty.SetValue(objectInstance, i);

                    targetList.Add(objectInstance);
                }

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

            if (type.Turret && !type.TurretAnimIsVoxel)
            {
                var turretAnim = AnimTypes.Find(at => at.ININame == type.TurretAnim);
                if (turretAnim != null)
                {
                    turretAnim.ArtConfig.IsBuildingAnim = true;
                    type.ArtConfig.TurretAnim = turretAnim;
                }
            }
        }

        public Color FindColor(string name)
        {
            var color = Colors.Find(c => c.Name == name);
            if (color == null)
                return Color.Black;
            return color.XNAColor;
        }
    }
}
