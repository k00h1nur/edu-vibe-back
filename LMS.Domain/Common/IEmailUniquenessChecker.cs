namespace LMS.Domain.Common;

public interface IEmailUniquenessChecker
{
    bool IsUnique(string email);
}