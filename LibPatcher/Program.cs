using ELFSharp.Utilities;
using LibPatcher;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
internal class Program
{
    static void Main(string[] args)
    {
        //ready
        try
        {
            new LibPatcherArm64();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        //close
        Exit();
    }
    static void Exit()
    {
        Console.WriteLine("Press Any Key To Exit..");
        Console.Read();
        Environment.Exit(0);
    }
}

