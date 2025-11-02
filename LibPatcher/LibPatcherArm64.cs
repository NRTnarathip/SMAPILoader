using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using LibPatcher;
using System.Security.Cryptography;
using System.Text;

internal class LibPatcherArm64 : LibPatcherBase
{
    protected override PatchData Patch_MethodAccessException()
    => new("mono_method_can_access_method", 0x24 + 0x4,
         [
            0x1F, 0x20, 0x03, 0xD5,
            0x1F, 0x20, 0x03, 0xD5,
         ]);

    protected override PatchData Patch_FieldAccessException()
        => new("mono_method_can_access_field", 0x130, [0x20, 0x00, 0x80, 0x52]);

    protected override PatchData Patch_mono_class_from_mono_type_internalCrashFix()
        => new("mono_class_from_mono_type_internal", 0x23c,
            [
                0x1f ,0x01, 0x00, 0xf1,
                0x20, 0x01, 0x88, 0x9a,
                0xfd, 0x7b, 0xc1, 0xa8,
                0xc0, 0x03, 0x5f, 0xd6,
            ]);

    // 8.0.21
    protected override string GetLibHashTarget()
        => "1ecb5dd49cc95dc7a3f03b235a91994643b0091ea4a09d037abb3ce511a7bf66";

}