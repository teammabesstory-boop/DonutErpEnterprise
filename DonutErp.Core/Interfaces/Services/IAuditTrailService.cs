#nullable enable

namespace DonutErp.Core.Interfaces.Services
{
    /// <summary>
    /// Comprehensive audit trail service for compliance and fraud detection.
    /// Provides immutable logging of all data changes with detailed context.
    /// </summary>
    public interface IAuditTrailService
    {
        /// <summary>
        /// Records a data change for audit purposes.
        /// Must be called for every data modification in the system.
        /// </summary>
        Task LogDataChangeAsync(
            string entityName,
            string entityId,
            string action,
            object? oldValues,
            object? newValues,
            string username,
            string userRole,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records authentication events for security monitoring.
        /// </summary>
        Task LogAuthenticationEventAsync(
            string username,
            bool isSuccessful,
            string? reason,
            string ipAddress,
            string? userAgent = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records access to sensitive data for compliance.
        /// </summary>
        Task LogSensitiveDataAccessAsync(
            string username,
            string userRole,
            string dataType,
            string recordId,
            string purpose,
            string ipAddress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves audit history for a specific entity.
        /// </summary>
        Task<List<AuditLogEntry>> GetAuditHistoryAsync(
            string entityName,
            string entityId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all audit logs for a specific user in date range.
        /// </summary>
        Task<List<AuditLogEntry>> GetUserAuditTrailAsync(
            string username,
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects suspicious patterns in audit logs.
        /// </summary>
        Task<List<SuspiciousActivityReport>> DetectSuspiciousActivitiesAsync(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates compliance report for audit purposes.
        /// </summary>
        Task<ComplianceReport> GenerateComplianceReportAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies data integrity by checking for unauthorized modifications.
        /// </summary>
        Task<DataIntegrityCheckResult> VerifyDataIntegrityAsync(
            string entityName,
            string entityId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports audit trail to immutable format for legal compliance.
        /// </summary>
        Task<byte[]> ExportAuditTrailAsync(
            DateTime startDate,
            DateTime endDate,
            string format = "PDF", // PDF, Excel, CSV
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates user activity report with summary statistics.
        /// </summary>
        Task<UserActivityReport> GenerateUserActivityReportAsync(
            string username,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
    }

    // ============ DTOs & VALUE OBJECTS ============

    public record AuditLogEntry
    {
        public Guid Id { get; init; }
        public DateTime Timestamp { get; init; }
        public string Action { get; init; } = string.Empty;
        public string EntityName { get; init; } = string.Empty;
        public string EntityId { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string UserRole { get; init; } = string.Empty;
        public Dictionary<string, object>? OldValues { get; init; }
        public Dictionary<string, object>? NewValues { get; init; }
        public string IpAddress { get; init; } = string.Empty;
        public string? UserAgent { get; init; }
        public bool IsDataModification { get; init; }
        public bool IsSuspicious { get; init; }
        public string? SuspicionReason { get; init; }
    }

    public record SuspiciousActivityReport
    {
        public Guid AuditLogId { get; init; }
        public DateTime ActivityDate { get; init; }
        public string Username { get; init; } = string.Empty;
        public string UserRole { get; init; } = string.Empty;
        public string ActivityType { get; init; } = string.Empty;
        public string EntityName { get; init; } = string.Empty;
        public string EntityId { get; init; } = string.Empty;
        public string SuspicionReason { get; init; } = string.Empty;
        public int RiskScore { get; init; } // 0-100
        public string RecommendedAction { get; init; } = string.Empty;
        public DateTime ReportedAt { get; init; }
    }

    public record ComplianceReport
    {
        public DateTime ReportDate { get; init; }
        public DateTime PeriodStart { get; init; }
        public DateTime PeriodEnd { get; init; }
        
        // Statistics
        public int TotalAuditLogEntries { get; init; }
        public int DataModifications { get; init; }
        public int UserAuthenticationEvents { get; init; }
        public int SuspiciousActivities { get; init; }
        public int FailedAccessAttempts { get; init; }
        
        // User Activity Summary
        public List<UserActivitySummary> UserActivities { get; init; } = new();
        
        // Entity Change Summary
        public List<EntityChangeSummary> EntityChanges { get; init; } = new();
        
        // Compliance Status
        public bool IsFullyCompliant { get; init; }
        public List<string> ComplianceIssues { get; init; } = new();
        public string OverallRating { get; init; } = "Good"; // Good, Fair, Poor
    }

    public record UserActivitySummary
    {
        public string Username { get; init; } = string.Empty;
        public string UserRole { get; init; } = string.Empty;
        public int TotalActions { get; init; }
        public int DataModifications { get; init; }
        public int DataAccess { get; init; }
        public int FailedAuthentications { get; init; }
        public DateTime FirstActivityDate { get; init; }
        public DateTime LastActivityDate { get; init; }
        public List<string> AccessedEntityTypes { get; init; } = new();
    }

    public record EntityChangeSummary
    {
        public string EntityName { get; init; } = string.Empty;
        public int TotalChanges { get; init; }
        public int Creates { get; init; }
        public int Updates { get; init; }
        public int Deletes { get; init; }
        public List<string> ModifiedByUsers { get; init; } = new();
    }

    public record DataIntegrityCheckResult
    {
        public string EntityName { get; init; } = string.Empty;
        public string EntityId { get; init; } = string.Empty;
        public bool IsIntegrityValid { get; init; }
        public DateTime CheckedAt { get; init; }
        public int TotalChangesFound { get; init; }
        public List<IntegrityIssue> Issues { get; init; } = new();
    }

    public record IntegrityIssue
    {
        public int ChangeNumber { get; init; }
        public DateTime ChangeDate { get; init; }
        public string ChangedBy { get; init; } = string.Empty;
        public string IssueType { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    public record UserActivityReport
    {
        public string Username { get; init; } = string.Empty;
        public string UserRole { get; init; } = string.Empty;
        public DateTime ReportDate { get; init; }
        public DateTime PeriodStart { get; init; }
        public DateTime PeriodEnd { get; init; }
        
        public int TotalActions { get; init; }
        public int DataModifications { get; init; }
        public int DataAccess { get; init; }
        public int AuthenticationEvents { get; init; }
        public int FailedAuthentications { get; init; }
        
        public DateTime FirstActivity { get; init; }
        public DateTime LastActivity { get; init; }
        public List<string> EntitiesModified { get; init; } = new();
        public List<string> IpAddressesUsed { get; init; } = new();
        
        public bool HasAnomalies { get; init; }
        public List<string> AnomalyDescriptions { get; init; } = new();
    }
}
