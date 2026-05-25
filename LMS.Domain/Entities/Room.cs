using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Room : BaseEntity
{
    public Room(string name, int capacity, string? meetingLink = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Room name is required.");
        if (capacity < 0) throw new DomainException("Capacity cannot be negative.");
        Name = name.Trim();
        Capacity = capacity;
        MeetingLink = meetingLink;
    }

    public string Name { get; private set; }
    public int Capacity { get; private set; }
    public string? MeetingLink { get; private set; }

    public void Update(string name, int capacity, string? meetingLink)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Room name is required.");
        if (capacity < 0) throw new DomainException("Capacity cannot be negative.");
        Name = name.Trim();
        Capacity = capacity;
        MeetingLink = meetingLink;
        Touch();
    }
}