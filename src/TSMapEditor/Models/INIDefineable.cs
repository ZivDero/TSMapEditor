using Rampastring.Tools;
using System;
using System.Collections.Generic;

namespace TSMapEditor.Models
{
    [AttributeUsage(AttributeTargets.Property)]
    public class INIAttribute : Attribute
    {
        public bool INIDefined;

        public INIAttribute(bool iniDefined)
        {
            INIDefined = iniDefined;
        }
    }

    public abstract class INIDefineable
    {
        /// <summary>
        /// The boolean string style to use when writing this object's properties to an INI file.
        /// </summary>
        [INI(false)]
        public BooleanStringStyle BooleanStringStyle { get; set; } = BooleanStringStyle.YESNO_LOWERCASE;

        public void ReadPropertiesFromIniSection(IniSection iniSection)
        {
            var type = GetType();
            var propertyInfos = type.GetProperties();

            foreach (var property in propertyInfos)
            {
                var propertyType = property.PropertyType;

                var iniAttribute = (INIAttribute)Attribute.GetCustomAttribute(property, typeof(INIAttribute));
                if (iniAttribute != null)
                {
                    if (!iniAttribute.INIDefined)
                        continue;
                }

                if (propertyType.IsEnum)
                {
                    if (!property.CanWrite)
                        continue;

                    string value = iniSection.GetStringValue(property.Name, string.Empty);

                    if (string.IsNullOrEmpty(value))
                        continue;

                    property.SetValue(this, Enum.Parse(propertyType, value), null);
                    continue;
                }

                var setter = property.GetSetMethod();

                if (setter == null)
                    continue;

                if (propertyType.Equals(typeof(int)))
                    setter.Invoke(this, new object[] { iniSection.GetIntValue(property.Name, (int)property.GetValue(this)) });
                else if (propertyType.Equals(typeof(double)))
                    setter.Invoke(this, new object[] { iniSection.GetDoubleValue(property.Name, (double)property.GetValue(this)) });
                else if (propertyType.Equals(typeof(float)))
                    setter.Invoke(this, new object[] { iniSection.GetSingleValue(property.Name, (float)property.GetValue(this)) });
                else if (propertyType.Equals(typeof(bool)))
                    setter.Invoke(this, new object[] { iniSection.GetBooleanValue(property.Name, (bool)property.GetValue(this)) });
                else if (propertyType.Equals(typeof(string)))
                    setter.Invoke(this, new object[] { iniSection.GetStringValue(property.Name, (string)property.GetValue(this)) });
                else if (propertyType.Equals(typeof(byte)))
                    setter.Invoke(this, new object[] { (byte)Math.Min(byte.MaxValue, iniSection.GetIntValue(property.Name, (byte)property.GetValue(this))) });
                else if (propertyType.Equals(typeof(char)))
                    setter.Invoke(this, new object[] { iniSection.GetStringValue(property.Name, ((char)property.GetValue(this)).ToString())[0] });
                else if (propertyType.Equals(typeof(int?)))
                {
                    int value;
                    bool success = int.TryParse(iniSection.GetStringValue(property.Name, ""), out value);
                    setter.Invoke(this, new object[] { success ? (int?)value : property.GetValue(this) });

                }
                else if (propertyType.Equals(typeof(double?)))
                {
                    double value;
                    bool success = double.TryParse(iniSection.GetStringValue(property.Name, ""), out value);
                    setter.Invoke(this, new object[] { success ? (double?)value : property.GetValue(this) });

                }
                else if (propertyType.Equals(typeof(float?)))
                {
                    float value;
                    bool success = float.TryParse(iniSection.GetStringValue(property.Name, ""), out value);
                    setter.Invoke(this, new object[] { success ? (float?)value : property.GetValue(this) });

                }
                else if (propertyType.Equals(typeof(bool?)))
                {
                    bool? value = null;
                    string valueString = iniSection.GetStringValue(property.Name, "");
                    if (valueString.ToLowerInvariant().Trim() == "true" || valueString.ToLowerInvariant().Trim() == "yes" || valueString.Trim() == "1")
                        value = true;
                    else if (valueString.ToLowerInvariant().Trim() == "false" || valueString.ToLowerInvariant().Trim() == "no" || valueString.Trim() == "0")
                        value = false;

                    setter.Invoke(this, new object[] { value ?? property.GetValue(this) });
                }
                else if (propertyType.Equals(typeof(List<string>)))
                    setter.Invoke(this, new object[] { iniSection.GetListValue(property.Name, ',', (s) => s) });
            }
        }

        public void WritePropertiesToIniSection(IniSection iniSection)
        {
            var type = GetType();
            var propertyInfos = type.GetProperties();

            foreach (var property in propertyInfos)
            {
                var propertyType = property.PropertyType;

                var getter = property.GetMethod;
                if (getter == null)
                    continue;

                var iniAttribute = (INIAttribute)Attribute.GetCustomAttribute(property, typeof(INIAttribute));
                if (iniAttribute != null)
                {
                    if (!iniAttribute.INIDefined)
                        continue;
                }

                if (propertyType.IsEnum)
                {
                    iniSection.SetStringValue(property.Name, property.GetValue(this).ToString());
                    continue;
                }

                var setter = property.GetSetMethod();

                if (setter == null)
                    continue;

                if (propertyType.Equals(typeof(int)))
                    iniSection.SetIntValue(property.Name, (int)getter.Invoke(this, null));
                else if (propertyType.Equals(typeof(byte)))
                    iniSection.SetIntValue(property.Name, (int)getter.Invoke(this, null));
                else if (propertyType.Equals(typeof(double)))
                    iniSection.SetDoubleValue(property.Name, (double)getter.Invoke(this, null));
                else if (propertyType.Equals(typeof(float)))
                    iniSection.SetFloatValue(property.Name, (float)getter.Invoke(this, null));
                else if (propertyType.Equals(typeof(bool)))
                    iniSection.SetBooleanValue(property.Name, (bool)getter.Invoke(this, null), BooleanStringStyle);
                else if (propertyType.Equals(typeof(string)))
                {
                    string value = (string)getter.Invoke(this, null);

                    if (value != null)
                        iniSection.SetStringValue(property.Name, value);
                    else
                        iniSection.RemoveKey(property.Name);
                }
                else if (propertyType.Equals(typeof(int?)))
                {
                    int? value = (int?)getter.Invoke(this, null);

                    if (value != null)
                        iniSection.SetIntValue(property.Name, (int)value);
                    else
                        iniSection.RemoveKey(property.Name);
                }
                else if (propertyType.Equals(typeof(double?)))
                {
                    double? value = (double?)getter.Invoke(this, null);

                    if (value != null)
                        iniSection.SetDoubleValue(property.Name, (double)value);
                    else
                        iniSection.RemoveKey(property.Name);
                }
                else if (propertyType.Equals(typeof(float?)))
                {
                    float? value = (float?)getter.Invoke(this, null);

                    if (value != null)
                        iniSection.SetFloatValue(property.Name, (float)value);
                    else
                        iniSection.RemoveKey(property.Name);
                }
                else if (propertyType.Equals(typeof(bool?)))
                {
                    bool? value = (bool?)getter.Invoke(this, null);

                    if (value != null)
                        iniSection.SetBooleanValue(property.Name, (bool)value, BooleanStringStyle);
                    else
                        iniSection.RemoveKey(property.Name);
                }
            }
        }
    }
}
