namespace Forager.Core.Animation;

/// <summary>Format-independent animation clips the game asks for.</summary>
public enum LogicalClip
{
    Idle,
    IdleAlt,
    Walk,
    Run,
    Jump,
    Crouch,
    CrouchIdle,
    Uncrouch,
    Flip,
    Push,
    Die,
}

/// <summary>
/// Maps a logical clip to whatever the importer actually named it. Exporters disagree: the GLB chibis
/// use <c>anim_walk</c> (and the typo <c>anim_iddle</c>), knight/ninja use <c>walkanim_</c>, FBX chibis
/// prefix <c>root_armature_student|anim_walk</c> and animals use <c>Armature|Walk</c>. Matching is
/// suffix-based so a prefix can never break it (lessons_learned.md LL-014).
/// </summary>
public static class ClipNameResolver
{
    private static readonly Dictionary<LogicalClip, string[]> Candidates = new()
    {
        [LogicalClip.Idle] = ["anim_iddle", "iddleanim_", "|iddle", "|idle", "_idle"],
        [LogicalClip.IdleAlt] = ["anim_iddle_001", "iddle_001anim_", "|iddle_001", "|idle_001"],
        [LogicalClip.Walk] = ["anim_walk", "walkanim_", "|walk"],
        [LogicalClip.Run] = ["anim_run", "runanim_", "|run"],
        [LogicalClip.Jump] = ["anim_jump", "jumpanim_", "|jump"],
        [LogicalClip.Crouch] = ["anim_crouch", "crouchanim_", "|crouch"],
        [LogicalClip.CrouchIdle] = ["anim_crouchiddle", "crouchiddleanim_", "|crouchiddle"],
        [LogicalClip.Uncrouch] = ["anim_uncrouch", "uncrouchanim_", "|uncrouch"],
        [LogicalClip.Flip] = ["anim_flip", "flipanim_", "|flip"],
        [LogicalClip.Push] = ["anim_push", "pushanim_", "|push"],
        [LogicalClip.Die] = ["anim_dying", "dyinganim_", "|death", "|dying"],
    };

    /// <summary>Suffix candidates for a clip, most specific first.</summary>
    public static IReadOnlyList<string> CandidatesFor(LogicalClip clip)
        => Candidates.TryGetValue(clip, out var patterns) ? patterns : Array.Empty<string>();

    /// <summary>
    /// Returns the best available clip name for a logical clip, or null. Within one suffix pattern the
    /// <b>shortest</b> match wins: <c>iddleanim_</c> must beat <c>crouchiddleanim_</c>, which also ends
    /// with it.
    /// </summary>
    public static string? Resolve(IReadOnlyList<string> available, LogicalClip clip)
    {
        if (available is null || available.Count == 0)
        {
            return null;
        }

        foreach (var pattern in CandidatesFor(clip))
        {
            string? best = null;
            foreach (var name in available)
            {
                if (string.IsNullOrEmpty(name) || !name.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (best is null || name.Length < best.Length)
                {
                    best = name;
                }
            }

            if (best is not null)
            {
                return best;
            }
        }

        return null;
    }
}
