using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Design;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Game;

internal static class MonoGameRewriter
{
    public static void Rewrite(string dllPath)
    {
        //not support now
        return;

        if (File.Exists(dllPath) is false)
        {
            Console.WriteLine("not found file: " + dllPath);
            return;
        }

        try
        {

            Console.WriteLine("start rewrite MonoGame.Framework.dll");

            //read dll
            var dllSearch = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(dll => dll.FullName.Contains("MonoGame.Framework"));
            Console.WriteLine("check before read assembly dll: " + dllSearch);
            using var dllStream = File.Open(dllPath, FileMode.Open, FileAccess.ReadWrite);
            var asmDef = StardewGameRewriter.ReadAssembly(dllStream);

            //add attribute in to class Vector2.cs
            //[TypeConverter(typeof(Vector2TypeConverter))]
            AddTypeConverter(asmDef, "Microsoft.Xna.Framework.Vector2", "Microsoft.Xna.Framework.Design.Vector2TypeConverter");

            //save file
            asmDef.Write();

            //done
            Console.WriteLine("done rewrite");

            dllSearch = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(dll => dll.FullName.Contains("MonoGame.Framework"));
            Console.WriteLine("check dll: " + dllSearch);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
    static void AddTypeConverter(AssemblyDefinition asmDef, string destClassTypeFullName, string typeConverterToAddFullName)
    {
        var module = asmDef.MainModule;
        var typeConverterToAdd = module.GetType(typeConverterToAddFullName);
        var destClassType = module.GetType(destClassTypeFullName);
        Console.WriteLine("try add type converter to add: " + typeConverterToAdd + ", on class: " + destClassType);

        var TypeConverterAttribute_Ctor = typeof(TypeConverterAttribute).GetConstructor([typeof(Type)]);
        Console.WriteLine("TypeConverterAttribute_Ctor: " + TypeConverterAttribute_Ctor);
        var TypeConverterAttribute_MethodRef = module.ImportReference(TypeConverterAttribute_Ctor);
        Console.WriteLine("imported TypeConverterAttribute: " + TypeConverterAttribute_MethodRef);


        var customAttribute = new CustomAttribute(TypeConverterAttribute_MethodRef);
        customAttribute.ConstructorArguments.Add(
            new CustomAttributeArgument(
                module.ImportReference(typeof(Type)),
                module.ImportReference(typeConverterToAdd)
            )
        );
        Console.WriteLine("created customAttribute: " + customAttribute);

        // เพิ่ม Attribute ให้กับ Target Type
        destClassType.CustomAttributes.Add(customAttribute);
        Console.WriteLine("done add customAttribute");
    }
}
