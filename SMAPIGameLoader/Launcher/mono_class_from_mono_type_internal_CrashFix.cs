using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Java.Util.Concurrent;
using SMAPIGameLoader.Tool;

namespace SMAPIGameLoader.Launcher;

internal static class mono_class_from_mono_type_internal_CrashFix
{
    [DllImport("libdl.so")]
    static extern IntPtr dlopen(string filename, int flags);

    [DllImport("libdl.so")]
    static extern nint dlsym(nint handle, string symbol);

    [DllImport("libdl.so")]
    static extern nint dlerror();

    [DllImport(BypassAccessException.libName, CallingConvention = CallingConvention.Cdecl)]
    static extern void PatchBytes(IntPtr targetAddress, byte[] bytes, IntPtr bytesLength);

    public static void Apply()
    {
        try
        {
            if (ArchitectureTool.IsIntel())
            {
                throw new NotImplementedException("can't apply mono_class_from_mono_type_internal_CrashFix on x86_x64");
            }
            else
            {
                Apply_Arm64();
            }
            Console.WriteLine("successfully patch mono_class_from_mono_type_internal_CrashFix");
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }

    static void Apply_Arm64()
    {
        Console.WriteLine("Init mono_class_from_mono_type_internal Crash Fix");

        var libHandle = dlopen("libmonosgen-2.0.so", 0x1);
        unsafe
        {
            Console.WriteLine("try patch disable assert crash mono_class_from_mono_type_internal");
            IntPtr methodAddress = dlsym(libHandle, "mono_class_from_mono_type_internal");
            IntPtr targetAddress = methodAddress + 0x23c;
            Console.WriteLine("try patch target: " + targetAddress);

            //patch code 'g_assert_not_reached ();'

            // to

            //if (currentMonoType != 0)
            //    returnValueKlassType = currentMonoType;

            //return returnValueKlassType;

            // TODO
            //always 'return null';
            //because we not set any variable
            byte[] patchBytes = {
                0x1f ,0x01, 0x00, 0xf1,
                0x20, 0x01, 0x88, 0x9a,
                0xfd, 0x7b, 0xc1, 0xa8,
                0xc0, 0x03, 0x5f, 0xd6,
            };
            Console.WriteLine("try patch bytes...");
            PatchBytes(targetAddress, patchBytes, patchBytes.Length);
        }
    }

    public static void InitDebug(Harmony hp)
    {
        //Is it disable debug??
#if true
        return;
#endif
        Console.WriteLine("try fix MonoMethodInfo.GetParametersInfo");
        var MonoMethodInfo = AccessTools.TypeByName("System.Reflection.MonoMethodInfo");
        var GetParametersInfo = AccessTools.Method(MonoMethodInfo, "GetParametersInfo");
        Console.WriteLine("GetParametersInfo: " + GetParametersInfo);
        hp.Patch(
            original: GetParametersInfo,
            postfix: new(typeof(mono_class_from_mono_type_internal_CrashFix),
                nameof(Postfix_GetParametersInfo))
        );
    }
    static void Postfix_GetParametersInfo(ParameterInfo[] __result, IntPtr handle, MemberInfo member)
    {
        if (__result == null || member.ReflectedType.Name.Contains("DMD"))
        {
            var msg = $"On Postfix_GetParametersInfo(), " +
                $"result: {__result}, nint: {handle}, " +
                $"memInfo.Name: {member.Name}, Type: {member.MemberType}";
            //logger.Log(msg, LogLevel.Error);
            Console.WriteLine(msg);
        }
    }
    static void Log(object msg) => Console.WriteLine(msg);

    private static bool ShowInStackTrace(MethodBase method)
    {
        Console.WriteLine("On ShowInStackTrace: method: " + method);
        if ((method.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) != 0)
        {
            Console.WriteLine("ShowInStackTrace return false 1");
            return false;
        }
        try
        {
            if (method.IsDefined(typeof(StackTraceHiddenAttribute), false))
            {
                Console.WriteLine("ShowInStackTrace return false 2");
                return false;
            }
            Type declaringType = method.DeclaringType;
            if (declaringType != null && declaringType.IsDefined(typeof(StackTraceHiddenAttribute), false))
            {
                Console.WriteLine("ShowInStackTrace return false 3");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error on ShowInStackTrace");
        }

        Console.WriteLine("ShowInStackTrace return true");
        return true;
    }

    static void My_StackTrace_ToString(StackTrace stack, object traceFormat, StringBuilder sb)
    {
        Console.WriteLine("On My Fix StackTrace.ToString(format, sb)");
        Console.WriteLine("trace format: " + traceFormat);

        int _numOfFrames = stack.FrameCount;
        string value = "at";
        string text = "in {0}:line {1}";
        string text2 = "in {0}:token 0x{1:x}+0x{2:x}";
        bool flag = true;
        for (int i = 0; i < _numOfFrames; i++)
        {
            Console.WriteLine("");
            Console.WriteLine($"On Frame: {i}");
            StackFrame frame = stack.GetFrame(i);
            MethodBase methodBase = frame?.GetMethod();
            Console.WriteLine($"method base: {methodBase}");

            if (!(methodBase != null) || (!ShowInStackTrace(methodBase) && i != _numOfFrames - 1))
            {
                //Console.WriteLine("continue this frame");
                continue;
            }
            if (flag)
            {
                flag = false;
                //Console.WriteLine("set flag to false");
            }
            else
            {
                sb.AppendLine();
                //Console.WriteLine("new line empty");
            }

            sb.Append("   ").Append(value).Append(' ');
            bool flag2 = false;
            Type declaringType = methodBase.DeclaringType;
            string name = methodBase.Name;
            bool flag3 = false;
            //todo
            //if (declaringType != null && declaringType.IsDefined(typeof(CompilerGeneratedAttribute), false))
            //{
            //    flag2 = declaringType.IsAssignableTo(typeof(IAsyncStateMachine));
            //    if (flag2 || declaringType.IsAssignableTo(typeof(IEnumerator)))
            //    {
            //        flag3 = TryResolveStateMachineMethod(ref methodBase, out declaringType);
            //    }
            //}

            if (declaringType != null)
            {
                string fullName = declaringType.FullName;
                //Console.WriteLine("try append on declaringType & full name");
                foreach (char c in fullName)
                {
                    sb.Append((c == '+') ? '.' : c);
                }
                sb.Append('.');
                //Console.WriteLine("done append it");
            }
            sb.Append(methodBase.Name);
            //Console.WriteLine("append method.Name");
            if (methodBase is MethodInfo { IsGenericMethod: not false } methodInfo)
            {
                Type[] genericArguments = methodInfo.GetGenericArguments();
                sb.Append('[');
                int k = 0;
                bool flag4 = true;
                for (; k < genericArguments.Length; k++)
                {
                    if (!flag4)
                    {
                        sb.Append(',');
                    }
                    else
                    {
                        flag4 = false;
                    }
                    sb.Append(genericArguments[k].Name);
                }
                sb.Append(']');
            }
            ParameterInfo[] paramArray = null;
            //Console.WriteLine("try get params to array");
            try
            {
                //Console.WriteLine("try  methodBase.GetParameters()");
                //error here
                //fixme
                //array = methodBase.GetParameters();
                paramArray = MyFix_GetParameters(methodBase);
                //Console.WriteLine("done get array");
            }
            catch (Exception ex)
            {
                Console.WriteLine("error on try methodBase.GetParameters()");
            }
            finally
            {
                Console.WriteLine("finally methodBase.GetParameters()");
            }

            //Console.WriteLine("params: " + paramArray);
            if (paramArray != null)
            {
                sb.Append('(');
                bool flag5 = true;
                for (int l = 0; l < paramArray.Length; l++)
                {
                    if (!flag5)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        flag5 = false;
                    }
                    string value2 = "<UnknownType>";
                    if (paramArray[l].ParameterType != null)
                    {
                        value2 = paramArray[l].ParameterType.Name;
                    }
                    sb.Append(value2);
                    string name2 = paramArray[l].Name;
                    if (name2 != null)
                    {
                        sb.Append(' ');
                        sb.Append(name2);
                    }
                }
                sb.Append(')');
            }
            //Console.WriteLine("flag 3: " + flag3);
            if (flag3)
            {
                sb.Append('+');
                sb.Append(name);
                sb.Append('(').Append(')');
            }
            int ilOffset = frame.GetILOffset();
            //Console.WriteLine("il offset: " + ilOffset);
            if (ilOffset != -1)
            {
                string fileName = frame.GetFileName();
                if (fileName != null)
                {
                    sb.Append(' ');
                    sb.AppendFormat(CultureInfo.InvariantCulture, text, fileName, frame.GetFileLineNumber());
                }
                //todo
                //else if (LocalAppContextSwitches.ShowILOffsets && methodBase.ReflectedType != null)
                //{
                //    string scopeName = methodBase.ReflectedType.Module.ScopeName;
                //    try
                //    {
                //        int metadataToken = methodBase.MetadataToken;
                //        P_1.Append(' ');
                //        P_1.AppendFormat(CultureInfo.InvariantCulture, text2, scopeName, metadataToken, frame.GetILOffset());
                //    }
                //    catch (InvalidOperationException)
                //    {
                //    }
                //}
            }
            var isLastFrame_FieldInfo = AccessTools.Field(
                typeof(StackFrame),
                "_isLastFrameFromForeignExceptionStackTrace");

            bool isLastFrame = (bool)isLastFrame_FieldInfo.GetValue(frame);
            //Console.WriteLine("_isLastFrame: " + isLastFrame);
            if (isLastFrame && !flag2)
            {
                sb.AppendLine();
                if (1 == 0)
                {
                }
                sb.Append("--- End of stack trace from previous location ---");
            }

            //Console.WriteLine($"end line for frame: {i}");
        }
        //Console.WriteLine("trace format: " + traceFormat.ToString());
        if (traceFormat.ToString() == "TrailingNewLine")
        {
            sb.AppendLine();
        }

        Console.WriteLine("Done Fix My StackTrace.ToString(format, sb)");
    }
    //TODO, should beware loop infinity when crash or exception in this method
    static bool StackTrace_ToString3(StackTrace __instance, object traceFormat, ref StringBuilder sb)
    {
        //Console.WriteLine("Prefix StackTrace_ToString3(format, stringBuilder)");
        var format = traceFormat;
        //Console.WriteLine("format: " + format);
        //Console.WriteLine("try call my StackTrace.ToString()");
        My_StackTrace_ToString(__instance, traceFormat, sb);
        if (sb.Length > 0)
        {
            //Console.WriteLine("Fixed crash on StackTrace_ToString3");
            return false;
        }

        return true;
    }
    static void Dump(this object obj)
    {
        if (obj == null)
        {
            Console.WriteLine("Object is null.");
            return;
        }

        Type type = obj.GetType();
        Console.WriteLine("===== Dumpper Object =====");
        Console.WriteLine($"Type: {type.FullName}");

        Console.WriteLine("Fields:");
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            try
            {
                object value = field.GetValue(obj);
                Console.WriteLine($"    {field.FieldType.Name} : {field.Name} = {value};");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    {field.Name} (Error: {ex.Message}), ex: {ex}");
            }
        }

        Console.WriteLine("Properties:");
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            try
            {
                if (property.CanRead)
                {
                    object value = property.GetValue(obj);
                    Console.WriteLine($"    {property.PropertyType.Name} {property.Name} = {value};");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {property.Name} (Error: {ex.Message}), innerExcep: {ex.InnerException?.Message}");
            }
        }
        Console.WriteLine("===== End Dump =====");
    }
    static ParameterInfo[] MyFix_GetParameters(MethodBase method)
    {
        Console.WriteLine("On Fix GetParameters(methodBase)");
        var handle = method.MethodHandle.Value;
        var MonoMethodInfo_Type = AccessTools.TypeByName("System.Reflection.MonoMethodInfo");
        var GetParametersInfo_MethodInfo = AccessTools.Method(MonoMethodInfo_Type, "GetParametersInfo");

        //fixme
        //try seek code at 'ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info'
        var reflectType = method.ReflectedType;
        Console.WriteLine("reflect type: " + reflectType);
        ParameterInfo[] parametersInfo = [];
        try
        {
            Console.WriteLine("try call GetParametersInfo_MethodInfo");
            parametersInfo = (ParameterInfo[])GetParametersInfo_MethodInfo.Invoke(null, [handle, method]);
            Console.WriteLine("resultParam: " + parametersInfo?.Length);
            Console.WriteLine("done GetParametersInfo_MethodInfo");
        }
        catch (Exception ex)
        {
            Console.WriteLine("error on try to get params info");
        }
        finally
        {
            Console.WriteLine("finally");
        }
        Console.WriteLine("len: " + parametersInfo.Length);
        if (parametersInfo.Length == 0)
        {
            Console.WriteLine("return params info len = 0");
            return parametersInfo;
        }

        // return with original 
        Console.WriteLine("try return with original params");
        return method.GetParameters();
    }
}

