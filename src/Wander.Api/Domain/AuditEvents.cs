namespace Wander.Api.Domain;

public static class AuditEvents
{
    public const string UserCreated = "user.created";
    public const string UserDeleted = "user.deleted";
    public const string UserDeletedBulk = "user.deleted.bulk";
    public const string UserSuspended = "user.suspended";
    public const string UserSuspendedBulk = "user.suspended.bulk";
    public const string UserReactivated = "user.reactivated";
    public const string UserReactivatedBulk = "user.reactivated.bulk";
    public const string UserUpdatedName = "user.updated.name";
    public const string UserUpdatedPassword = "user.updated.password";
    public const string UserUpdatedEmail = "user.updated.email";
    public const string UserUpdatedPrivacy = "user.updated.privacy";
    public const string UserSelfDeactivated = "user.self.deactivated";
    public const string AuthLoginFailed = "auth.login.failed";
    public const string JobSyncStarted = "job.sync.started";
    public const string JobSyncCompleted = "job.sync.completed";
    public const string JobSyncSkipped = "job.sync.skipped";
    public const string JobNotifyCleanupStarted = "job.notify-cleanup.started";
    public const string JobNotifyCleanupCompleted = "job.notify-cleanup.completed";
}

public enum AuditSeverity
{
    Info,
    Warning,
    Error
}
