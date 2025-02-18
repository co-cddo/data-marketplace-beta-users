using cddo_users.DTOs;
using cddo_users.models;
using Dapper;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography.Xml;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace cddo_users.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task CreateUserApproval(UserRoleApproval approval)
        {
            var sql = @"INSERT INTO UserRoleApprovals 
                            (UserID, 
                            DomainID, 
                            OrganisationID, 
                            RoleID, 
                            ApprovalStatus,
                            RequestReason,
                            RejectionComment,
                            CreatedAt, 
                            UpdatedAt) 
                    VALUES (@UserID, 
                            @DomainID, 
                            @OrganisationID, 
                            @RoleID, 
                            @ApprovalStatus,
                            @RequestReason,
                            @RejectionComment,
                            GETDATE(), 
                            GETDATE()); 
                    SELECT CAST(SCOPE_IDENTITY() as int);";
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteScalarAsync<int>(sql, new
                {
                    approval.UserID,
                    approval.DomainID,
                    approval.OrganisationID,
                    approval.RoleID,
                    ApprovalStatus = approval.ApprovalStatus.ToString(),
                    RequestReason = approval.RequestReason,
                    RejectionComment = approval.RejectionComment
                });
            }
        }

        //Not the best solution, but we will take the pending requests out of this call
        public async Task<(IEnumerable<UserRoleApprovalDetail> Approvals, int TotalCount)> GetUserApprovalsAsync(UserRoleApprovalRequest request)
        {
            var offset = (request.PageNumber - 1) * request.PageSize;
            var parameters = new DynamicParameters(new { DomainId = request.DomainId, OrganisationId = request.OrganisationId, SearchTerm = $"%{request.SearchTerm}%", Offset = offset, PageSize = request.PageSize });
            var conditions = new List<string>();

            if (request.DomainId.HasValue)
            {
                conditions.Add("ura.DomainID = @DomainId");
            }
            if (request.OrganisationId.HasValue)
            {
                conditions.Add("ura.OrganisationID = @OrganisationId");
            }
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                conditions.Add("(u.Username LIKE @SearchTerm OR o.OrganisationName LIKE @SearchTerm OR d.DomainName LIKE @SearchTerm)");
            }
            if (request.NoPending)
            {
                conditions.Add("ura.ApprovalStatus != 'Pending'");
            }

            var baseSql = @"
        SELECT ura.ApprovalID, ura.UserID, u.Username, ura.DomainID, d.DomainName AS DomainName, 
               ura.OrganisationID, o.OrganisationName AS OrganisationName, ura.RoleID, r.RoleName AS RoleName, 
               ura.ApprovalStatus, ura.ApprovedByUserID, approver.Username AS ApprovedByUsername, 
               approverDomain.DomainName AS ApprovedByDomainName, approverOrg.OrganisationName AS ApprovedByOrganisationName, 
               ura.CreatedAt, ura.UpdatedAt, ura.RejectionComment, ura.RequestReason
        FROM UserRoleApprovals ura
        JOIN Users u ON ura.UserID = u.UserID
        JOIN Organisations o ON ura.OrganisationID = o.OrganisationID
        JOIN Domains d ON ura.DomainID = d.DomainID
        JOIN Roles r ON ura.RoleID = r.RoleID
        LEFT JOIN Users approver ON ura.ApprovedByUserID = approver.UserID
        LEFT JOIN Domains approverDomain ON approver.DomainID = approverDomain.DomainID
        LEFT JOIN Organisations approverOrg ON approver.OrganisationID = approverOrg.OrganisationID";

            if (conditions.Count > 0)
            {
                baseSql += " WHERE " + string.Join(" AND ", conditions);
            }

            var paginatedSql = baseSql + $" ORDER BY {request.SortBy} {request.SortOrder} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
            var countSql = "SELECT COUNT(*) FROM (" + baseSql + ") AS CountQuery";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var approvals = await connection.QueryAsync<UserRoleApprovalDetail>(paginatedSql, parameters);
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
                return (approvals.ToList(), totalCount);
            }
        }

        public async Task<IEnumerable<UserRoleApprovalDetail>> GetUserApprovalsAsync(int userId)
        {
            var query = @"
                            SELECT *
                            FROM [dbo].[UserRoleApprovals]
                            Where UserID = @UserID
                            ORDER BY CreatedAt DESC
                        ";


            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var queryResults = await connection.QueryAsync<UserRoleApprovalDetail>(query, new { UserID = userId });

                return queryResults;
            }

        }


        public async Task<IEnumerable<UserRoleApprovalDetail>> GetUserPendingApprovalsAsync(
     int? domainId = null,
     int? organisationId = null)
        {
            var parameters = new DynamicParameters(new { DomainId = domainId, OrganisationId = organisationId });
            var conditions = new List<string>();

            if (domainId.HasValue)
            {
                conditions.Add("ura.DomainID = @DomainId");
            }
            if (organisationId.HasValue)
            {
                conditions.Add("ura.OrganisationID = @OrganisationId");
            }

            //For pending
            conditions.Add("ura.ApprovalStatus = 'Pending'");

            var baseSql = @"
        SELECT ura.ApprovalID, ura.UserID, u.Username, ura.DomainID, d.DomainName AS DomainName, 
               ura.OrganisationID, o.OrganisationName AS OrganisationName, ura.RoleID, r.RoleName AS RoleName, 
               ura.ApprovalStatus, ura.ApprovedByUserID, approver.Username AS ApprovedByUsername, 
               approverDomain.DomainName AS ApprovedByDomainName, approverOrg.OrganisationName AS ApprovedByOrganisationName, 
               ura.CreatedAt, ura.UpdatedAt, ura.RejectionComment, ura.RequestReason
        FROM UserRoleApprovals ura
        JOIN Users u ON ura.UserID = u.UserID
        JOIN Organisations o ON ura.OrganisationID = o.OrganisationID
        JOIN Domains d ON ura.DomainID = d.DomainID
        JOIN Roles r ON ura.RoleID = r.RoleID
        LEFT JOIN Users approver ON ura.ApprovedByUserID = approver.UserID
        LEFT JOIN Domains approverDomain ON approver.DomainID = approverDomain.DomainID
        LEFT JOIN Organisations approverOrg ON approver.OrganisationID = approverOrg.OrganisationID";


            baseSql += " WHERE " + string.Join(" AND ", conditions);

            var finalQuery = baseSql + $" ORDER BY ura.CreatedAt DESC";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var approvals = await connection.QueryAsync<UserRoleApprovalDetail>(finalQuery, parameters);
                return (approvals.ToList());
            }
        }

        public async Task<UserRoleApprovalDetail> Approve(int id)
        {
            var sql = @" SELECT ura.ApprovalID, ura.UserID, u.Username, ura.DomainID, d.DomainName AS DomainName, 
                        ura.OrganisationID, o.OrganisationName AS OrganisationName, ura.RoleID, r.RoleName AS RoleName, 
                        ura.ApprovalStatus, ura.ApprovedByUserID, approver.Username AS ApprovedByUsername, 
                        approverDomain.DomainName AS ApprovedByDomainName, approverOrg.OrganisationName AS ApprovedByOrganisationName, 
                        ura.CreatedAt, ura.UpdatedAt, ura.RejectionComment, ura.RequestReason
                         FROM UserRoleApprovals ura
                         JOIN Users u ON ura.UserID = u.UserID
                         JOIN Organisations o ON ura.OrganisationID = o.OrganisationID
                         JOIN Domains d ON ura.DomainID = d.DomainID
                         JOIN Roles r ON ura.RoleID = r.RoleID
                         LEFT JOIN Users approver ON ura.ApprovedByUserID = approver.UserID
                         LEFT JOIN Domains approverDomain ON approver.DomainID = approverDomain.DomainID
                         LEFT JOIN Organisations approverOrg ON approver.OrganisationID = approverOrg.OrganisationID
                         Where ApprovalID = @Id";
            using (var connection = new SqlConnection(_connectionString))
            {
                var result = await connection.QuerySingleOrDefaultAsync<UserRoleApprovalDetail>(sql, new { Id = id });
                return result;
            }
        }

        public async Task<(bool, int)> ApprovalDecision(UserRoleApproval approval)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Begin transaction
                    var transaction = await connection.BeginTransactionAsync();

                    try
                    {
                        // Update the approval status
                        await connection.ExecuteAsync(@"
                UPDATE UserRoleApprovals
                SET ApprovalStatus = @ApprovalStatus, 
                    ApprovedByUserID = @ApprovedByUserID, 
                    UpdatedAt = GETUTCDATE(),
                    RejectionComment = @RejectionComment
                WHERE ApprovalID = @ApprovalID",
                        new
                        {
                            ApprovalStatus = approval.ApprovalStatus.ToString(),
                            approval.ApprovedByUserID,
                            approval.RejectionComment,
                            approval.ApprovalID
                        },
                        transaction);

                        // Fetch the UserID from the approval request
                        var userID = await connection.QuerySingleOrDefaultAsync<int>(@"
                SELECT UserID 
                FROM UserRoleApprovals 
                WHERE ApprovalID = @ApprovalID",
                        new { approval.ApprovalID },
                        transaction);

                        // Grant user the publisher role if approved
                        if (approval.ApprovalStatus == ApprovalStatus.Approved && userID > 0 && approval.RoleID != null)
                        {
                            await AddUserToRoleAsync(userID, (int)approval.RoleID);
                        }

                        // Commit transaction
                        await transaction.CommitAsync();
                        return (true, userID);
                    }
                    catch (Exception)
                    {
                        // Rollback transaction on failure
                        await transaction.RollbackAsync();
                        return (false, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine($"Error updating user approval: {ex.Message}");
                return (false, 0);
            }
        }

        public async Task<DomainInfoDto?> GetOrganisationIdByDomainAsync(string? domainName)
        {
            var query = @"SELECT 
                                    d.DomainId, 
                                    d.OrganisationId, 
                                    d.OrganisationType, 
                                    d.OrganisationFormat, 
                                    d.AllowList, 
                                    d.Visible,
                                    o.OrganisationName
                                FROM 
                                    Domains d
                                INNER JOIN 
                                    Organisations o ON d.OrganisationId = o.OrganisationId
                                WHERE 
                                    d.DomainName = @DomainName;";

            var domainInfo = await new SqlConnection(_connectionString).QueryFirstOrDefaultAsync<DomainInfoDto>(query, new { DomainName = domainName });

            if (domainInfo == null)
            {
                return null;
            }

            return domainInfo;
        }

        public async Task<bool> AddUserToRoleAsync(int userId, int roleId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"
                            IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
                            BEGIN
                                INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)
                            END";

                int rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });
                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdatePreferences(int? user, bool? EmailNotification)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand(@" UPDATE [dbo].[Users]
                                                SET [EmailNotification] = @EmailNotification
                                                WHERE [UserID] = @UserId", connection);

                command.Parameters.AddWithValue("@UserId", user);
                command.Parameters.AddWithValue("@EmailNotification", (EmailNotification.HasValue && EmailNotification == true) ? 1 : 0); // Converts bool to bit

                await connection.OpenAsync();
                var result = await command.ExecuteNonQueryAsync();

                if (result > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<bool> UpdateUserRoleRequestAsync(int userId, int roleId, int approverUserId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand(@" UPDATE [dbo].[UserRoleApprovals]
                                                SET [ApprovalStatus] = 'Approved',
                                                [ApprovedByUserID] = @ApproverUserId,
                                                [UpdatedAt] = GETUTCDATE(),
                                                [RejectionComment] = 'Added by administrator'
                                                WHERE [UserID] = @UserId
                                                AND [RoleID] = @RoleID"
                                                , connection);

                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@RoleID", roleId);
                command.Parameters.AddWithValue("@ApproverUserId", approverUserId);

                await connection.OpenAsync();
                var result = await command.ExecuteNonQueryAsync();

                if (result > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<bool> RemoveUserFromRoleAsync(int userId, int roleId)
        {
            bool removed = false;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "DELETE FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId";
                await connection.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });
                
                removed = true;
            }
            return removed;
        }

        public async Task<UserProfile?> GetUserByEmailAsync(string email)
        {
            var userProfileSql = @"
                                    SELECT u.*, d.*, o.* 
                                    FROM Users u
                                    INNER JOIN Domains d ON u.DomainID = d.DomainID
                                    INNER JOIN Organisations o ON u.OrganisationID = o.OrganisationID
                                    WHERE u.Email = @Email;
    
                                    SELECT r.* 
                                    FROM Roles r
                                    INNER JOIN UserRoles ur ON r.RoleID = ur.RoleID
                                    INNER JOIN Users u ON ur.UserID = u.UserID
                                    WHERE u.Email = @Email
                                    AND r.[Visible] = 1;
                                    ";

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var queryResults = await connection.QueryMultipleAsync(userProfileSql, new { Email = email });

                    var userProfileData = await queryResults.ReadSingleOrDefaultAsync<dynamic>();
                    if (userProfileData == null) return null;

                    var roles = (await queryResults.ReadAsync<DTOs.Role>()).ToList();

                    var utcLastLogin = userProfileData.LastLogin;

                    var userProfile = new UserProfile
                    {
                        LastLogin = utcLastLogin,
                        User = new DTOs.UserInfo
                        {
                            UserId = userProfileData.UserID,
                            UserEmail = userProfileData.Email,
                            UserName = userProfileData.UserName,
                            // Map other UserInfo properties as needed
                        },
                        Domain = new UserDomain
                        {
                            DomainId = userProfileData.DomainID,
                            DomainName = userProfileData.DomainName,
                            IsEnabled = userProfileData.AllowList, // Assuming AllowList maps to IsEnabled
                            DataShareRequestMailboxAddress = userProfileData.DataShareRequestMailboxAddress
                            // Map other UserDomain properties as needed
                        },
                        Organisation = new UserOrganisation
                        {
                            OrganisationId = userProfileData.OrganisationID,
                            OrganisationName = userProfileData.OrganisationName,
                            IsEnabled = userProfileData.Visible, // Assuming Visible maps to IsEnabled
                            // Map other UserOrganisation properties as needed
                        },
                        Roles = roles,
                        EmailNotification = userProfileData.EmailNotification,
                        WelcomeNotification = userProfileData.WelcomeNotification
                    };
                    return userProfile;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                return null;
            }
        }

        public async Task<UserProfile> GetUserByIdAsync(string id)
        {
            var userProfileSql = @"
                                    SELECT u.*, d.*, o.* 
                                    FROM Users u
                                    INNER JOIN Domains d ON u.DomainID = d.DomainID
                                    INNER JOIN Organisations o ON u.OrganisationID = o.OrganisationID
                                    WHERE u.UserId = @id;
    
                                    SELECT r.* 
                                    FROM Roles r
                                    INNER JOIN UserRoles ur ON r.RoleID = ur.RoleID
                                    INNER JOIN Users u ON ur.UserID = u.UserID
                                    WHERE u.UserId = @id
                                    AND r.Visible = 1;
                                    ";

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var queryResults = await connection.QueryMultipleAsync(userProfileSql, new { id });

                    var userProfileData = await queryResults.ReadSingleOrDefaultAsync<dynamic>();
                    if (userProfileData == null) return null;

                    var roles = (await queryResults.ReadAsync<DTOs.Role>()).ToList();

                    var utcLastLogin = userProfileData.LastLogin;
                    var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
                    var localLastLogin = TimeZoneInfo.ConvertTimeFromUtc(utcLastLogin, localTimeZone);

                    var userProfile = new UserProfile
                    {
                        LastLogin = localLastLogin,
                        User = new DTOs.UserInfo
                        {
                            UserId = userProfileData.UserID,
                            UserEmail = userProfileData.Email,
                            UserName = userProfileData.UserName,
                            // Map other UserInfo properties as needed
                        },
                        Domain = new UserDomain
                        {
                            DomainId = userProfileData.DomainID,
                            DomainName = userProfileData.DomainName,
                            IsEnabled = userProfileData.AllowList,
                            DataShareRequestMailboxAddress = userProfileData.DataShareRequestMailboxAddress
                        },
                        Organisation = new UserOrganisation
                        {
                            OrganisationId = userProfileData.OrganisationID,
                            OrganisationName = userProfileData.OrganisationName,
                            IsEnabled = userProfileData.Visible, // Assuming Visible maps to IsEnabled
                                                                 // Map other UserOrganisation properties as needed
                        },
                        Roles = roles,
                        EmailNotification = userProfileData.EmailNotification,
                        WelcomeNotification = userProfileData.WelcomeNotification
                    };
                    return userProfile;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                return new UserProfile { };
            }
        }

        public async Task<int> CreateUserAsync(UserProfile userProfile)
        {
            var sql = @"
    INSERT INTO Users (
        Email, 
        LastLogin, 
        TwoFactorEnabled, 
        TwoFactorSecretKey, 
        BackupCodes, 
        OrganisationID, 
        DomainID, 
        UserName, 
        Visible,
        EmailNotification, 
        WelcomeNotification
    ) 
    VALUES (
        @Email, 
        @LastLogin, 
        @TwoFactorEnabled, 
        @TwoFactorSecretKey, 
        @BackupCodes, 
        @OrganisationID, 
        @DomainID, 
        @UserName, 
        @Visible,
        @EmailNotification, 
        @WelcomeNotification
    );
    SELECT CAST(SCOPE_IDENTITY() as int);
    ";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new
                {
                    Email = userProfile.User.UserEmail,
                    LastLogin = DateTime.UtcNow, // Assuming the creation time is the last login
                    TwoFactorEnabled = false, // Default value, adjust as necessary
                    TwoFactorSecretKey = string.Empty, // Adjust as necessary
                    BackupCodes = string.Empty, // Adjust as necessary
                    UserPreferences = "{}", // Assuming JSON format, adjust as necessary
                    OrganisationID = userProfile.Organisation.OrganisationId,
                    DomainID = userProfile.Domain.DomainId,
                    userProfile.User.UserName,
                    Visible = true, // Adjust based on your logic
                    userProfile.EmailNotification,
                    userProfile.WelcomeNotification
                };

                var userId = await connection.QuerySingleAsync<int>(sql, parameters);
                return userId;
            }
        }

        public async Task UpdateLastLogin(string email)
        {
            var sql = @"
                        UPDATE Users SET 
                            LastLogin = GETUTCDATE()
                        WHERE Email = @Email";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(sql, new { Email = email });
            }
        }

        public async Task DeleteUserAsync(models.User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "DELETE FROM Users WHERE UserId = @UserId";
                await connection.ExecuteAsync(sql, new { user.UserId });
            }
        }

        public async Task<ApprovalStatus> CheckPublisherApproval(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
            SELECT TOP 1 ApprovalStatus 
            FROM UserRoleApprovals 
            WHERE UserID = @UserId AND RoleID = @RoleId 
            ORDER BY CreatedAt DESC";

                // Execute the query and get the status
                var result = await connection.QueryFirstOrDefaultAsync(sql, new { UserId = userId, RoleId = 4 });

                dynamic dynamicResult = result!;
                var status = dynamicResult?.ApprovalStatus;

                if (Enum.TryParse(status, true, out ApprovalStatus approvalstatus))
                {
                    return approvalstatus;
                }
                else
                {
                    // If no status is found, return "NotRequested"
                    return ApprovalStatus.NotRequested;
                }
            }
        }

        public async Task<List<DTOs.Role>> GetAllRolesAsync()
        {
            var rolesAllQuery = @"
                                    SELECT r.* 
                                    FROM Roles r
                                    WHERE r.Visible = 1
                                    ";

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var queryResults = await connection.QueryMultipleAsync(rolesAllQuery);

                    var roles = (await queryResults.ReadAsync<DTOs.Role>()).ToList();

                    return roles;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            return new List<DTOs.Role>();
        }

        public async Task<IEnumerable<EmailUserName>>? GetOrgAdminsByOrgId(int organisationId)
        {
            var query = @"
                            SELECT [Email]
                                  ,[UserName]
                                  ,[EmailNotification]
                            FROM [dbo].[Users] U
                            inner join UserRoles UR on UR.UserID = U.UserID
                            Where U.OrganisationID = @OrganisationId
                            AND UR.RoleID = 3
                        ";


            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var queryResults = await connection.QueryAsync<EmailUserName>(query, new { OrganisationId = organisationId });

                return queryResults;
            }
        }

       
        public async Task<UserRoleApprovalDetail?> GetUserApprovalAsync(UserRoleApproval approval)
        {

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                            SELECT TOP 1 ApprovalStatus 
                            FROM UserRoleApprovals 
                            WHERE UserID = @UserId 
                            AND RoleID = @RoleId
                            AND DomainID = @DomainID
                            AND OrganisationID = @OrganisationID
                            AND ApprovalStatus = @ApprovalStatus
                            ORDER BY CreatedAt DESC";

                // Execute the query and get the status
                var result = await connection.QueryFirstOrDefaultAsync<UserRoleApprovalDetail>(sql, new
                {
                    UserId = approval.UserID,
                    RoleId = approval.RoleID,
                    DomainID = approval.DomainID,
                    OrganisationID = approval.OrganisationID,
                    ApprovalStatus = approval.ApprovalStatus.ToString(),
                });

                return result;
            }
        }

        public async Task<IEnumerable<UserAdminDto>> GetAllUsersByRoleTypeAsync(string roleType)
        {
            var query = @"
        SELECT DISTINCT
            u.UserID, u.Email, u.LastLogin, u.TwoFactorEnabled, u.EmailNotification,
            u.WelcomeNotification, u.OrganisationID, o.OrganisationName, u.DomainID, 
            u.UserName, u.Visible,
            STUFF((SELECT ', ' + r.RoleName 
                   FROM UserRoles ur
                   JOIN Roles r ON ur.RoleID = r.RoleID
                   WHERE ur.UserID = u.UserID
                   AND r.Visible = 1
                   FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS RolesList
        FROM 
            Users u
        LEFT JOIN 
            Organisations o ON u.OrganisationID = o.OrganisationID
        JOIN 
            UserRoles ur ON u.UserID = ur.UserID
        JOIN 
            Roles r ON ur.RoleID = r.RoleID
        WHERE 
            r.RoleName = @RoleType
            AND u.Visible = 1;
    ";

            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new { RoleType = roleType };

                var userRecords = await connection.QueryAsync<UserAdminDto>(query, parameters);

                foreach (var user in userRecords)
                {
                    user.Roles = !string.IsNullOrEmpty(user.RolesList)
                        ? user.RolesList.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(roleName => new Role { RoleName = roleName })
                                        .ToList()
                        : new List<Role>();
                }

                return userRecords;
            }
        }

        public async Task DeleteUserApprovalAsync(UserRoleApprovalDetail userApproval)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM UserRoleApprovals WHERE UserID = @UserId 
                            AND RoleID = @RoleId
                            AND DomainID = @DomainID
                            AND OrganisationID = @OrganisationID";
                await connection.ExecuteAsync(sql, new
                {
                    UserId = userApproval.UserID,
                    RoleId = userApproval.RoleID,
                    DomainID = userApproval.DomainID,
                    OrganisationID = userApproval.OrganisationID
                });
            }
        }

        public async Task<UserResponseDto> GetFilteredUsers(UserQueryParameters queryParams)
        {
 
            var parameters = new
            {
                SearchTerm = queryParams.SearchTerm,
                OrganisationID = queryParams.OrganisationID,
                DomainID = queryParams.DomainID,
                Visible = queryParams.Visible,
                PageStart = (queryParams.PageNumber - 1) * queryParams.PageSize,
                PageSize = queryParams.PageSize
            };
            var sortOptions = string.Format($"{queryParams.SortBy} {queryParams.SortOrder}");

            var query = string.Format(@"
                SELECT DISTINCT
                    u.UserID, 
                    u.Email, 
                    u.LastLogin, 
                    u.TwoFactorEnabled, 
                    u.EmailNotification,
                    u.WelcomeNotification, 
                    u.OrganisationID, 
                    o.OrganisationName, 
                    u.DomainID, 
                    u.UserName, 
                    u.Visible,
                    STUFF((SELECT ', ' + r.RoleName 
                           FROM UserRoles ur
                           JOIN Roles r ON ur.RoleID = r.RoleID
                           WHERE ur.UserID = u.UserID
                           AND r.Visible = 1
                           FOR XML PATH('')), 1, 2, '') AS RolesList
                FROM 
                    Users u
                LEFT JOIN 
                    Organisations o ON u.OrganisationID = o.OrganisationID
                WHERE 
                    (@SearchTerm IS NULL OR u.Email LIKE '%' + @SearchTerm + '%' OR u.UserName LIKE '%' + @SearchTerm + '%')
                    AND (@OrganisationID IS NULL OR u.OrganisationID = @OrganisationID)
                    AND (@DomainID IS NULL OR u.DomainID = @DomainID)
                    AND (@Visible IS NULL OR u.Visible = @Visible)
                ORDER BY 
                    {0}
                OFFSET @PageStart ROWS FETCH NEXT @PageSize ROWS ONLY;
                ", sortOptions);

            var countQuery = @"
                SELECT COUNT(DISTINCT u.UserID)
                FROM Users u
                LEFT JOIN Organisations o ON u.OrganisationID = o.OrganisationID
                WHERE (@SearchTerm IS NULL OR u.Email LIKE '%' + @SearchTerm + '%' OR u.UserName LIKE '%' + @SearchTerm + '%')
                    AND (@OrganisationID IS NULL OR u.OrganisationID = @OrganisationID)
                    AND (@DomainID IS NULL OR u.DomainID = @DomainID)
                    AND (@Visible IS NULL OR u.Visible = @Visible);
                ";

            using (var connection = new SqlConnection(_connectionString))
            {
                // Execute the count query first
                var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

                // Execute the main query to fetch users
                var intermediateUsers = (await connection.QueryAsync<UserAdminDto>(query, parameters)).ToList();

                var users = intermediateUsers.Select(user => new UserAdminDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    LastLogin = user.LastLogin,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    EmailNotification = user.EmailNotification,
                    WelcomeNotification = user.WelcomeNotification,
                    OrganisationID = user.OrganisationID,
                    OrganisationName = user.OrganisationName,
                    DomainID = user.DomainID,
                    UserName = user.UserName,
                    Visible = user.Visible,
                    Roles = !string.IsNullOrEmpty(user.RolesList)
                        ? user.RolesList.Split(new[] { ", " }, StringSplitOptions.None)
                                    .Select(roleName => new DTOs.Role { RoleName = roleName })
                                    .ToList()
                        : new List<DTOs.Role>()
                }).ToList();

                var response = new UserResponseDto
                {
                    Users = users,
                    TotalCount = totalCount
                };

                return response;
            }
        }
    }
}

