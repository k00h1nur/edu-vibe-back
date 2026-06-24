namespace LMS.Domain.Enums;

/// <summary>How a teacher punishment reduces salary.</summary>
public enum PunishmentType
{
    /// <summary><see cref="Entities.Punishment.Value"/> is a percentage (0–100) of the computed salary.</summary>
    Percentage = 1,
    /// <summary><see cref="Entities.Punishment.Value"/> is a fixed money amount subtracted directly.</summary>
    FixedAmount = 2,
}
