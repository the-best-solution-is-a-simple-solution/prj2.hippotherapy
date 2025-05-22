using System.ComponentModel;
using System.Reflection;

namespace HippoApi.Models;

/// <summary>
///     A helper class so we can have an enum that has a string attached
///     get string with .GetDescription()
/// </summary>
public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());
        DescriptionAttribute? attribute = field.GetCustomAttribute<DescriptionAttribute>();
        return attribute == null ? value.ToString() : attribute.Description;
    }
}