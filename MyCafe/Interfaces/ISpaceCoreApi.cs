using System;
using System.Reflection;

namespace MyCafe.Interfaces;
public interface ISpaceCoreApi
{
    public void RegisterSerializerType(Type type);
    public void RegisterCustomProperty(Type declaringType, string propName, Type propType, MethodInfo getterMethod, MethodInfo setterMethod);
}
