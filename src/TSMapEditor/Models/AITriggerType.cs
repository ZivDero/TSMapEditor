﻿using Rampastring.Tools;
using System.Globalization;

namespace TSMapEditor.Models
{
    public enum AITriggerComparatorOperator
    {
        LessThan,
        LessThanOrEqual,
        Equal,
        MoreThanOrEqual,
        MoreThan,
        NotEqual
    }

    public struct AITriggerComparator
    {
        public AITriggerComparatorOperator ComparatorOperator;
        public int Quantity;

        public AITriggerComparator(AITriggerComparatorOperator comparatorOperator, int quantity)
        {
            ComparatorOperator = comparatorOperator;
            Quantity = quantity;
        }

        public static AITriggerComparator? Parse(string value)
        {
            if (value.Length != 64)
                return null;

            string quantityPart = value[..8];
            if (!int.TryParse(quantityPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int quantity))
                return null;

            quantity = Helpers.ReverseEndianness(quantity);

            int operatorPart = int.Parse(value[9].ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            return new AITriggerComparator((AITriggerComparatorOperator)operatorPart, quantity);
        }

        public string ToStringValue()
        {
            int quantity = Helpers.ReverseEndianness(Quantity);

            return quantity.ToString("X8") + "0" + ((int)ComparatorOperator).ToString(CultureInfo.InvariantCulture) + "000000000000000000000000000000000000000000000000000000";
        }
    }

    public class AITriggerType
    {
        // [AITriggerTypes]
        // ID=Name,Team1,OwnerHouse,TechLevel,ConditionType,ConditionObject,Comparator,StartingWeight,MinimumWeight,MaximumWeight,IsForSkirmish,unused,Side,IsBaseDefense,Team2,EnabledInE,EnabledInM,EnabledInH

        public AITriggerType(string iniName)
        {
            ININame = iniName;
        }

        public string ININame { get; private set; }

        public string Name { get; set; }
        public TeamType PrimaryTeam { get; set; }
        public string OwnerName { get; set; }
        public HouseType Owner { get; set; }
        public int TechLevel { get; set; }
        public AITriggerConditionType ConditionType { get; set; }
        public string ConditionObjectString { get; set; }

        /// <summary>
        /// The comparator string originally loaded from the map.
        /// Not used nor refreshed afterwards, 
        /// <see cref="Comparator" /> is used after loading.
        /// </summary>
        public string LoadedComparatorString { get; set; }
        public AITriggerComparator Comparator { get; set; }
        public double InitialWeight { get; set; }
        public double MinimumWeight { get; set; }
        public double MaximumWeight { get; set; }
        public bool EnabledInMultiplayer { get; set; }
        public bool Unused { get; set; }

        /// <summary>
        /// The side of the AITrigger. In vanilla Tiberian Sun, 0 = all sides, 1 = GDI, 2 = Nod.
        /// </summary>
        public int Side { get; set; }
        public bool IsBaseDefense { get; set; }
        public TeamType SecondaryTeam { get; set; }
        public bool Easy { get; set; }
        public bool Medium { get; set; }
        public bool Hard { get; set; }

        public AITriggerType Clone(string newUniqueId)
        {
            var clonedAITrigger = (AITriggerType)MemberwiseClone();
            clonedAITrigger.Name = "Clone of " + Name;
            clonedAITrigger.ININame = newUniqueId;
            return clonedAITrigger;
        }

        public void WriteToIniSection(IniSection iniSection)
        {
            var extendedStringBuilder = new ExtendedStringBuilder(true, ',');
            extendedStringBuilder.Append(Name);
            extendedStringBuilder.Append(PrimaryTeam == null ? Constants.NoneValue1 : PrimaryTeam.ININame);
            extendedStringBuilder.Append(OwnerName);
            extendedStringBuilder.Append(TechLevel);
            extendedStringBuilder.Append((int)ConditionType);
            extendedStringBuilder.Append(ConditionObjectString ?? Constants.NoneValue1);
            extendedStringBuilder.Append(Comparator.ToStringValue());
            extendedStringBuilder.Append(InitialWeight.ToString(".######", CultureInfo.InvariantCulture));
            extendedStringBuilder.Append(MinimumWeight.ToString(".######", CultureInfo.InvariantCulture));
            extendedStringBuilder.Append(MaximumWeight.ToString(".######", CultureInfo.InvariantCulture));
            extendedStringBuilder.Append("1"); // EnabledInForMultiplayer, no reason not to enable this
            extendedStringBuilder.Append("0"); // unused
            extendedStringBuilder.Append(Side);
            extendedStringBuilder.Append("0"); // called IsBaseDefense, effectively unused by the game
            extendedStringBuilder.Append(SecondaryTeam == null ? Constants.NoneValue1 : SecondaryTeam.ININame);
            extendedStringBuilder.Append(Helpers.BoolToIntString(Easy));
            extendedStringBuilder.Append(Helpers.BoolToIntString(Medium));
            extendedStringBuilder.Append(Helpers.BoolToIntString(Hard));

            iniSection.SetStringValue(ININame, extendedStringBuilder.ToString());
        }
    }
}
