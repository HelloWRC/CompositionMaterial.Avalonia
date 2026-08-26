using System.Reflection;
using System.Reflection.Emit;

namespace CompositionMaterial.Avalonia.Platform.Windows;

internal static class FastReflection
{
    public static Func<object, T> CreateGetter<T>(PropertyInfo property)
    {
        var getter = property.GetMethod ?? throw new MissingMethodException(property.DeclaringType?.FullName, property.Name);
        var method = new DynamicMethod($"cm_get_{property.DeclaringType?.Name}_{property.Name}", typeof(T), [typeof(object)],
            typeof(FastReflection).Module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, property.DeclaringType!);
        il.Emit(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter);
        EmitConversion(il, property.PropertyType, typeof(T));
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Func<object, T>>();
    }

    public static Func<object, T> CreateFieldGetter<T>(FieldInfo field)
    {
        var method = new DynamicMethod($"cm_field_{field.DeclaringType?.Name}_{field.Name}", typeof(T), [typeof(object)],
            typeof(FastReflection).Module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, field.DeclaringType!);
        il.Emit(OpCodes.Ldfld, field);
        EmitConversion(il, field.FieldType, typeof(T));
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Func<object, T>>();
    }

    public static Action<object, Action, bool> CreateServerJobInvoker(MethodInfo methodInfo)
    {
        var method = new DynamicMethod("cm_post_server_job", typeof(void), [typeof(object), typeof(Action), typeof(bool)],
            typeof(FastReflection).Module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, methodInfo.DeclaringType!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Callvirt, methodInfo);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Action<object, Action, bool>>();
    }

    public static Action<object, T> CreateSetter<T>(MethodInfo methodInfo)
    {
        var method = new DynamicMethod($"cm_set_{methodInfo.Name}", typeof(void), [typeof(object), typeof(T)],
            typeof(FastReflection).Module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, methodInfo.DeclaringType!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, methodInfo);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Action<object, T>>();
    }

    private static void EmitConversion(ILGenerator il, Type source, Type target)
    {
        if (source == target)
            return;
        if (target == typeof(object))
        {
            if (source.IsValueType)
                il.Emit(OpCodes.Box, source);
            return;
        }
        if (source == typeof(object) && target.IsValueType)
            il.Emit(OpCodes.Unbox_Any, target);
        else
            il.Emit(OpCodes.Castclass, target);
    }
}
