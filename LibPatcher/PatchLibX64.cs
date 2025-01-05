using ELFSharp.ELF.Sections;
using ELFSharp.ELF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace LibPatcher;
internal sealed class PatchLibX64 : BasePatchLib
{
    private PatchLibX64() : base(PlatformEnum.X64)
    {

    }

    internal static void Start()
    {
        new PatchLibX64();
    }

    protected override PatchData Patch_FieldAccessException()
        => new("mono_method_can_access_field", 0x132,
            [
              0xB8, 0x01, 0x00, 0x00, 0x00, // MOV EAX, 1
			  0x48, 0x83, 0xC4, 0x58,       // ADD RSP, 0x58
			  0x5B,                         // POP RBX
			  0x41, 0x5C,                   // POP R12
			  0x41, 0x5D,                   // POP R13
			  0x41, 0x5E,                   // POP R14
			  0x41, 0x5F,                   // POP R15
			  0x5D,                         // POP RBP
			  0xC3
            ]);

    protected override PatchData Patch_MethodAccessException()
        => new("mono_method_can_access_method", 0x30 + 0x15,
            [
                0xEB, 0x09, //jump to return;
            ]);

    protected override PatchData Patch_mono_class_from_mono_type_internalCrashFix()
        => new("mono_class_from_mono_type_internal", 0x261,
            [
                // remove code
                // FUN_0031ffd0("/__w/1/s/src/mono/mono/metadata/class.c",0x8c4)
                // just fill 'NOP'
                //0x90, 0x90, 0x90, 0x90, 0x90,
                //0x90, 0x90, 0x90, 0x90, 0x90,
                //0x90, 0x90, 0x90, 0x90, 0x90,
                //0x90, 0x90,


                //MOV  EAX, 0x0
                0xb8, 0x0, 0x0, 0x0, 0x0,
                0x59, //pop RCX
                0xc3 //ret
            ]);

    protected override string GetLibHashTarget()
        => "57caffd67717aa21dabb276e629bdfb1c5451293e0cd1e5585f1a91dea359539";

}
