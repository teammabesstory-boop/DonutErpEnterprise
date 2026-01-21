#nullable enable

using DonutErp.Core.Entities;
using DonutErp.Core.Interfaces.Services;
using DonutErp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DonutErp.Infrastructure.Services.Implements
{
    /// <summary>
    /// Comprehensive audit trail service for compliance, security, and fraud detection.
    /// Every data change in the system must be logged here.
    /// </summary>
    public class AuditTrailService : IAuditTrailService
    {
        private readonly AppDbContext _dbContext;

        public AuditTrailService(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task LogDataChangeAsync(
            string entityName,
            string entityId,
            string action,
            object? oldValues,
            object? newValues,
            string username,
            string userRole,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nameof(entityName));
            ArgumentNullException.ThrowIfNull(nameof(entityId));
            ArgumentNullException.ThrowIfNull(nameof(username));

            var oldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : string.Empty;
            var newValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : string.Empty;

            var isSuspicious = DetectSuspiciousActivity(action, entityName, oldValues, newValues);
            var suspicionReason = isSuspicious ? GenerateSuspicionReason(action, entityName, oldValues, newValues) : null;

            var auditLog = new ComplianceAuditLog
            {
                Timestamp = DateTime.Now,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Username = username,
                UserRole = userRole,
                OldValues = oldValuesJson,
                NewValues = newValuesJson,
                IpAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent,
                IsDataModification = !string.IsNullOrEmpty(newValuesJson),
                IsSuspicious = isSuspicious,
                SuspicionReason = suspicionReason,
                CreatedAt = DateTime.Now
            };

            _dbContext.Set<ComplianceAuditLog>().Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task LogAuthenticationEventAsync(
            string username,
            bool isSuccessful,
            string? reason,
            string ipAddress,
            string? userAgent = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nameof(username));
            ArgumentNullException.ThrowIfNull(nameof(ipAddress));

            var auditLog = new ComplianceAuditLog
            {
                Timestamp = DateTime.Now,
                Action = isSuccessful ? "LOGIN_SUCCESS" : "LOGIN_FAILED",
                EntityName = "Authentication",
                EntityId = username,
                Username = username,
                UserRole = "Unknown",
                OldValues = string.Empty,
                NewValues = JsonSerializer.Serialize(new { success = isSuccessful, reason = reason }),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsDataModification = false,
                IsSuspicious = !isSuccessful,
                SuspicionReason = isSuccessful ? null : $"Failed authentication: {reason}",
                CreatedAt = DateTime.Now
            };

            _dbContext.Set<ComplianceAuditLog>().Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task LogSensitiveDataAccessAsync(
            string username,
            string userRole,
            string dataType,
            string recordId,
            string purpose,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nameof(username));
            ArgumentNullException.ThrowIfNull(nameof(dataType));

            var auditLog = new ComplianceAuditLog
            {
                Timestamp = DateTime.Now,
                Action = "SENSITIVE_DATA_ACCESS",
                EntityName = dataType,
                EntityId = recordId,
                Username = username,
                UserRole = userRole,
                OldValues = string.Empty,
                NewValues = JsonSerializer.Serialize(new { purpose = purpose }),
                IpAddress = ipAddress,
                IsDataModification = false,
                IsSuspicious = false,
                CreatedAt = DateTime.Now
            };

            _dbContext.Set<ComplianceAuditLog>().Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<AuditLogEntry>> GetAuditHistoryAsync(
            string entityName,
            string entityId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nameof(entityName));
            ArgumentNullException.ThrowIfNull(nameof(entityId));

            var query = _dbContext.Set<ComplianceAuditLog>()
                .Where(al => al.EntityName == entityName && al.EntityId == entityId)
                .AsNoTracking();

            if (fromDate.HasValue)
                query = query.Where(al => al.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(al => al.Timestamp <= toDate.Value);

            var logs = await query.OrderByDescending(al => al.Timestamp).ToListAsync(cancellationToken);

            return logs.Select(ConvertToAuditLogEntry).ToList();
        }

        public async Task<List<AuditLogEntry>> GetUserAuditTrailAsync(
            string username,
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nameof(username));

            var logs = await _dbContext.Set<ComplianceAuditLog>()
                .Where(al => al.Username == username &&
                           al.Timestamp >= fromDate &&
                           al.Timestamp <= toDate)
                .AsNoTracking()
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync(cancellationToken);

            return logs.Select(ConvertToAuditLogEntry).ToList();
        }

        public async Task<List<SuspiciousActivityReport>> DetectSuspiciousActivitiesAsync(
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default)
        {
            var suspiciousLogs = await _dbContext.Set<ComplianceAuditLog>()
                .Where(al => al.IsSuspicious &&
                           al.Timestamp >= dateFrom &&
                           al.Timestamp <= dateTo)
                .AsNoTracking()
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync(cancellationToken);

            var reports = new List<SuspiciousActivityReport>();

            foreach (var log in suspiciousLogs)
            {
                var riskScore = CalculateRiskScore(log);

                reports.Add(new SuspiciousActivityReport
                {
                    AuditLogId = log.Id,
                    ActivityDate = log.Timestamp,
                    Username = log.Username,
                    UserRole = log.UserRole,
                    ActivityType = log.Action,
                    EntityName = log.EntityName,
                    EntityId = log.EntityId,
                    SuspicionReason = log.SuspicionReason ?? "Unknown reason",
                    RiskScore = riskScore,
                    RecommendedAction = GenerateRecommendedAction(riskScore, log),
                    ReportedAt = DateTime.Now
                });
            }

            return reports.OrderByDescending(r => r.RiskScore).ToList();
        }

        public async Task<ComplianceReport> GenerateComplianceReportAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            var allLogs = await _dbContext.Set<ComplianceAuditLog>()
                .Where(al => al.Timestamp >= startDate && al.Timestamp <= endDate)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var dataModifications = allLogs.Count(al => al.IsDataModification);
            var suspiciousActivities = allLogs.Count(al => al.IsSuspicious);
            var authenticationLogs = allLogs.Where(al => al.Action.StartsWith("LOGIN_")).ToList();
            var failedAuths = authenticationLogs.Count(al => al.Action == "LOGIN_FAILED");

            // User activity summary
            var userActivities = allLogs
                .GroupBy(al => al.Username)
                .Select(g => new UserActivitySummary
                {
                    Username = g.Key,
                    UserRole = g.First().UserRole,
                    TotalActions = g.Count(),
                    DataModifications = g.Count(al => al.IsDataModification),
                    DataAccess = g.Count(al => al.Action == "SENSITIVE_DATA_ACCESS"),
                    FailedAuthentications = g.Count(al => al.Action == "LOGIN_FAILED"),
                    FirstActivityDate = g.Min(al => al.Timestamp),
                    LastActivityDate = g.Max(al => al.Timestamp),
                    AccessedEntityTypes = g.Select(al => al.EntityName).Distinct().ToList()
                }).ToList();

            // Entity change summary
            var entityChanges = allLogs
                .GroupBy(al => al.EntityName)
                .Select(g => new EntityChangeSummary
                {
                    EntityName = g.Key,
                    TotalChanges = g.Count(),
                    Creates = g.Count(al => al.Action == "CREATE"),
                    Updates = g.Count(al => al.Action == "UPDATE"),
                    Deletes = g.Count(al => al.Action == "DELETE"),
                    ModifiedByUsers = g.Select(al => al.Username).Distinct().ToList()
                }).ToList();

            var complianceIssues = new List<string>();
            if (suspiciousActivities > 0)
                complianceIssues.Add($"Found {suspiciousActivities} suspicious activities");
            if (failedAuths > 10)
                complianceIssues.Add($"High number of failed authentication attempts: {failedAuths}");

            var isFullyCompliant = complianceIssues.Count == 0;

            return new ComplianceReport
            {
                ReportDate = DateTime.Now,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalAuditLogEntries = allLogs.Count,
                DataModifications = dataModifications,
                UserAuthenticationEvents = authenticationLogs.Count,
                SuspiciousActivities = suspiciousActivities,
                FailedAccessAttempts = failedAuths,
                UserActivities = userActivities,
                EntityChanges = entityChanges,
                IsFullyCompliant = isFullyCompliant,
                ComplianceIssues = complianceIssues,
                OverallRating = suspiciousActivities == 0 && failedAuths <= 5 ? "Good" : 
                                suspiciousActivities < 5 && failedAuths <= 10 ? "Fair" : "Poor"
            };
        }

        public async Task<DataIntegrityCheckResult> VerifyDataIntegrityAsync(
            string entityName,
            string entityId,
            CancellationToken cancellationToken = default)
        {
            var auditLogs = await GetAuditHistoryAsync(entityName, entityId, null, null, cancellationToken);

            if (!auditLogs.Any())
            {
                return new DataIntegrityCheckResult
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    IsIntegrityValid = true,
                    CheckedAt = DateTime.Now,
                    TotalChangesFound = 0,
                    Issues = new()
                };
            }

            var issues = new List<IntegrityIssue>();

            // Check for unusual patterns
            for (int i = 0; i < auditLogs.Count - 1; i++)
            {
                var current = auditLogs[i];
                var next = auditLogs[i + 1];

                // Check if change was reverted immediately
                if (i > 0 && current.Action == "UPDATE" && next.Action == "UPDATE")
                {
                    var timeDiff = Math.Abs((current.Timestamp - next.Timestamp).TotalSeconds);
                    if (timeDiff < 60) // Changed within 1 minute
                    {
                        issues.Add(new IntegrityIssue
                        {
                            ChangeNumber = i,
                            ChangeDate = current.Timestamp,
                            ChangedBy = current.Username,
                            IssueType = "Rapid Reversion",
                            Description = "Data was changed and reverted within 1 minute - possible error correction or manipulation"
                        });
                    }
                }

                // Check for after-hours modifications
                if (current.Timestamp.Hour < 6 || current.Timestamp.Hour > 22)
                {
                    issues.Add(new IntegrityIssue
                    {
                        ChangeNumber = i,
                        ChangeDate = current.Timestamp,
                        ChangedBy = current.Username,
                        IssueType = "After-Hours Change",
                        Description = "Data was modified during non-business hours"
                    });
                }
            }

            var isIntegrityValid = issues.Count == 0;

            return new DataIntegrityCheckResult
            {
                EntityName = entityName,
                EntityId = entityId,
                IsIntegrityValid = isIntegrityValid,
                CheckedAt = DateTime.Now,
                TotalChangesFound = auditLogs.Count,
                Issues = issues
            };
        }

        public async Task<byte[]> ExportAuditTrailAsync(
            DateTime startDate,
            DateTime endDate,
            string format = "PDF",
            CancellationToken cancellationToken = default)
        {
            var logs = await _dbContext.Set<ComplianceAuditLog>()
                .Where(al => al.Timestamp >= startDate && al.Timestamp <= endDate)
                .OrderByDescending(al => al.Timestamp)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // For now, return a simple CSV export
            // In production, you'd use a library like iText for PDF or ClosedXML for Excel
            var csvContent = "Timestamp,Action,Entity,EntityId,Username,Role,Modified,Suspicious\n";

            foreach (var log in logs)
            {
                csvContent += $"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Action}\",\"{log.EntityName}\",\"{log.EntityId}\"," +
                              $"\"{log.Username}\",\"{log.UserRole}\",{log.IsDataModification},{log.IsSuspicious}\n";
            }

            return System.Text.Encoding.UTF8.GetBytes(csvContent);
        }

        public async Task<UserActivityReport> GenerateUserActivityReportAsync(
            string username,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(nameof(username));

            var logs = await GetUserAuditTrailAsync(username, startDate, endDate, cancellationToken);

            if (!logs.Any())
            {
                return new UserActivityReport
                {
                    Username = username,
                    ReportDate = DateTime.Now,
                    PeriodStart = startDate,
                    PeriodEnd = endDate
                };
            }

            var dataModifications = logs.Count(al => al.IsDataModification);
            var dataAccess = logs.Count(al => al.Action == "SENSITIVE_DATA_ACCESS");
            var authEvents = logs.Count(al => al.Action.StartsWith("LOGIN_"));
            var failedAuths = logs.Count(al => al.Action == "LOGIN_FAILED");

            var entitiesModified = logs
                .Where(al => al.IsDataModification)
                .Select(al => al.EntityName)
                .Distinct()
                .ToList();

            var ipAddresses = logs.Select(al => al.IpAddress).Distinct().ToList();

            var anomalies = new List<string>();
            if (failedAuths > 3)
                anomalies.Add($"Multiple failed authentication attempts ({failedAuths})");
            if (ipAddresses.Count > 5)
                anomalies.Add($"Access from {ipAddresses.Count} different IP addresses");

            return new UserActivityReport
            {
                Username = username,
                UserRole = logs.First().UserRole,
                ReportDate = DateTime.Now,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalActions = logs.Count,
                DataModifications = dataModifications,
                DataAccess = dataAccess,
                AuthenticationEvents = authEvents,
                FailedAuthentications = failedAuths,
                FirstActivity = logs.Min(al => al.Timestamp),
                LastActivity = logs.Max(al => al.Timestamp),
                EntitiesModified = entitiesModified,
                IpAddressesUsed = ipAddresses,
                HasAnomalies = anomalies.Any(),
                AnomalyDescriptions = anomalies
            };
        }

        // ============ HELPER METHODS ============

        private AuditLogEntry ConvertToAuditLogEntry(ComplianceAuditLog log)
        {
            Dictionary<string, object>? ParseJson(string json)
            {
                if (string.IsNullOrEmpty(json)) return null;
                try
                {
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                }
                catch
                {
                    return null;
                }
            }

            return new AuditLogEntry
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                Action = log.Action,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Username = log.Username,
                UserRole = log.UserRole,
                OldValues = ParseJson(log.OldValues),
                NewValues = ParseJson(log.NewValues),
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                IsDataModification = log.IsDataModification,
                IsSuspicious = log.IsSuspicious,
                SuspicionReason = log.SuspicionReason
            };
        }

        private bool DetectSuspiciousActivity(string action, string entityName, object? oldValues, object? newValues)
        {
            // Suspicious patterns
            var dangerousActions = new[] { "DELETE", "BULK_UPDATE", "DATA_EXPORT" };
            if (dangerousActions.Contains(action.ToUpper()))
                return true;

            // Sensitive entities
            var sensitiveEntities = new[] { "Users", "Wallets", "Transactions" };
            if (sensitiveEntities.Contains(entityName) && action == "DELETE")
                return true;

            return false;
        }

        private string? GenerateSuspicionReason(string action, string entityName, object? oldValues, object? newValues)
        {
            if (action == "DELETE" && new[] { "Transactions", "Wallets" }.Contains(entityName))
                return $"Deletion of sensitive entity: {entityName}";

            if (action == "BULK_UPDATE")
                return "Bulk data modification detected";

            return null;
        }

        private int CalculateRiskScore(ComplianceAuditLog log)
        {
            var score = 30;

            if (log.Action == "DELETE") score += 30;
            if (log.Action == "BULK_UPDATE") score += 20;
            if (new[] { "Users", "Wallets" }.Any(e => e == log.EntityName)) score += 20;

            var afterHours = log.Timestamp.Hour < 6 || log.Timestamp.Hour > 22;
            if (afterHours) score += 10;

            return Math.Min(100, score);
        }

        private string GenerateRecommendedAction(int riskScore, ComplianceAuditLog log)
        {
            return riskScore switch
            {
                >= 80 => "CRITICAL: Immediate investigation required. Consider temporarily disabling user account.",
                >= 60 => "HIGH: Review activity details. Verify with user if necessary.",
                >= 40 => "MEDIUM: Log activity for future reference. Monitor user.",
                _ => "LOW: Standard logging. No immediate action needed."
            };
        }
    }
}
