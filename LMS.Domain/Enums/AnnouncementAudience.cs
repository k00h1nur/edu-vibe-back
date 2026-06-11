namespace LMS.Domain.Enums;

/// <summary>
/// Who can see a non-public announcement once signed in. Public-flag rows
/// bypass this entirely — they go to the marketing site too.
/// </summary>
public enum AnnouncementAudience
{
    /// <summary>Every signed-in user — admin, teacher, student.</summary>
    Everyone = 1,
    /// <summary>Only staff / teacher accounts.</summary>
    Teachers = 2,
    /// <summary>Only student accounts.</summary>
    Students = 3,
}
