using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using LibPatcher;
using System.Security.Cryptography;
using System.Text;

internal class PatchLibArm64 : BasePatchLib
{
    private PatchLibArm64() : base(PlatformEnum.Arm64)
    {
    }

    internal static void Start()
    {
        new PatchLibArm64();
    }

    protected override PatchData Patch_MethodAccessException()
    => new("mono_method_can_access_method", 0x24 + 0x1c,
         [
            0x1F, 0x20, 0x03, 0xD5,
            0x1F, 0x20, 0x03, 0xD5,
            0x1F, 0x20, 0x03, 0xD5,
            0x1F, 0x20, 0x03, 0xD5,
            0x1F, 0x20, 0x03, 0xD5,
         ]);

    protected override PatchData Patch_FieldAccessException()
        => new("mono_method_can_access_field", 0x120, [0x20, 0x00, 0x80, 0x52]);

    protected override PatchData Patch_mono_class_from_mono_type_internalCrashFix()
        => new("mono_class_from_mono_type_internal", 0x23c,
            [
                0x1f ,0x01, 0x00, 0xf1,
                0x20, 0x01, 0x88, 0x9a,
                0xfd, 0x7b, 0xc1, 0xa8,
                0xc0, 0x03, 0x5f, 0xd6,
            ]);

    protected override string GetLibHashTarget()
        => "3a2ae3237b0be6d5ed7c4bda0b2c5fa8b2836a0a6de20fc96b007fb7389571b4";

}