using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyCafe.Interfaces;
public interface ISpaceCoreApi
{
    public void RegisterSerializerType(Type type);
    public void RegisterCustomProperty(Type declaringType, string propName, Type propType, MethodInfo getterMethod, MethodInfo setterMethod);
}
