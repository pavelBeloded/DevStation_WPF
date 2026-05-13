namespace DevStation.Data.Models;

public enum UserRole
{
    User,
    Admin
}

public enum SnippetStatus
{
    Draft,
    PendingReview,
    Published,
    Rejected
}

public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected
}
