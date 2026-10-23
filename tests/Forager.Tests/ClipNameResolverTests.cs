using Forager.Core.Animation;

namespace Forager.Tests;

public sealed class ClipNameResolverTests
{
    private static readonly string[] GlbChibiClips =
    [
        "anim_iddle", "anim_iddle_001", "anim_walk", "anim_run", "anim_jump",
        "anim_crouch", "anim_crouchiddle", "anim_uncrouch", "anim_flip", "anim_push", "anim_dying",
    ];

    private static readonly string[] KnightClips =
    [
        "crouchanim_", "crouchiddleanim_", "dyinganim_", "flipanim_", "iddle_001anim_",
        "iddleanim_", "jumpanim_", "pushanim_", "runanim_", "uncrouchanim_", "walkanim_",
    ];

    private static readonly string[] FbxPrefixedClips =
    [
        "root_armature_student|anim_crouch", "root_armature_student|anim_crouchiddle",
        "root_armature_student|anim_dying", "root_armature_student|anim_flip",
        "root_armature_student|anim_iddle", "root_armature_student|anim_iddle_001",
        "root_armature_student|anim_jump", "root_armature_student|anim_push",
        "root_armature_student|anim_run", "root_armature_student|anim_uncrouch",
        "root_armature_student|anim_walk",
    ];

    private static readonly string[] AnimalClips =
    [
        "Armature|Death", "Armature|Idle", "Armature|Jump", "Armature|Run", "Armature|Walk", "Armature|WalkSlow",
    ];

    [Theory]
    [InlineData(LogicalClip.Idle, "anim_iddle")]
    [InlineData(LogicalClip.IdleAlt, "anim_iddle_001")]
    [InlineData(LogicalClip.Walk, "anim_walk")]
    [InlineData(LogicalClip.Run, "anim_run")]
    [InlineData(LogicalClip.Jump, "anim_jump")]
    [InlineData(LogicalClip.Crouch, "anim_crouch")]
    [InlineData(LogicalClip.CrouchIdle, "anim_crouchiddle")]
    [InlineData(LogicalClip.Uncrouch, "anim_uncrouch")]
    [InlineData(LogicalClip.Flip, "anim_flip")]
    [InlineData(LogicalClip.Push, "anim_push")]
    [InlineData(LogicalClip.Die, "anim_dying")]
    public void ResolvesEveryGlbChibiClip(LogicalClip clip, string expected)
        => Assert.Equal(expected, ClipNameResolver.Resolve(GlbChibiClips, clip));

    [Theory]
    [InlineData(LogicalClip.Idle, "iddleanim_")]
    [InlineData(LogicalClip.Walk, "walkanim_")]
    [InlineData(LogicalClip.Run, "runanim_")]
    [InlineData(LogicalClip.CrouchIdle, "crouchiddleanim_")]
    [InlineData(LogicalClip.Die, "dyinganim_")]
    public void ResolvesKnightStyleSuffixes(LogicalClip clip, string expected)
        => Assert.Equal(expected, ClipNameResolver.Resolve(KnightClips, clip));

    [Theory]
    [InlineData(LogicalClip.Idle, "root_armature_student|anim_iddle")]
    [InlineData(LogicalClip.Walk, "root_armature_student|anim_walk")]
    [InlineData(LogicalClip.Run, "root_armature_student|anim_run")]
    public void ResolvesFbxPrefixedNames(LogicalClip clip, string expected)
        => Assert.Equal(expected, ClipNameResolver.Resolve(FbxPrefixedClips, clip));

    [Theory]
    [InlineData(LogicalClip.Idle, "Armature|Idle")]
    [InlineData(LogicalClip.Walk, "Armature|Walk")]
    [InlineData(LogicalClip.Run, "Armature|Run")]
    [InlineData(LogicalClip.Jump, "Armature|Jump")]
    [InlineData(LogicalClip.Die, "Armature|Death")]
    public void ResolvesAnimalNames(LogicalClip clip, string expected)
        => Assert.Equal(expected, ClipNameResolver.Resolve(AnimalClips, clip));

    [Fact]
    public void CrouchIdle_DoesNotResolveToPlainIdleOrCrouch()
    {
        var resolved = ClipNameResolver.Resolve(GlbChibiClips, LogicalClip.CrouchIdle);

        Assert.Equal("anim_crouchiddle", resolved);
        Assert.NotEqual("anim_iddle", resolved);
        Assert.NotEqual("anim_crouch", resolved);
    }

    [Fact]
    public void MissingClip_ReturnsNull()
    {
        Assert.Null(ClipNameResolver.Resolve(["unrelated", "anim_sneeze"], LogicalClip.Walk));
        Assert.Null(ClipNameResolver.Resolve(Array.Empty<string>(), LogicalClip.Walk));
        Assert.Null(ClipNameResolver.Resolve(null!, LogicalClip.Walk));
    }
}
