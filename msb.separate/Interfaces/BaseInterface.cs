using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace msb.separate.Interfaces
{
    public interface BaseInterface
    {
        public void Start();
        public void Stop();
        public void PublishEvent(EventData eventToPublish);
    }

    public class BaseInterfaceUtils
    {
        public static Delegate CreateFunctionPointer(MethodInfo methodInfo, object callableObjectForMethod)
        {
            var functionParameterTypes = from parameter in methodInfo.GetParameters() select parameter.ParameterType;
            Type delgateType;
            if (methodInfo.ReturnType == typeof(void))
            {
                delgateType = System.Linq.Expressions.Expression.GetActionType(functionParameterTypes.ToArray());
            }
            else
            {
                functionParameterTypes = functionParameterTypes.Concat(new[] { methodInfo.ReturnType });
                delgateType = System.Linq.Expressions.Expression.GetFuncType(functionParameterTypes.ToArray());
            }

            return methodInfo.CreateDelegate(delgateType, callableObjectForMethod);
        }
        public static T CreateType<T>() where T : new()
        {
            return new T();
        }
    }
}
