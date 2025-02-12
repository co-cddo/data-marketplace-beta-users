namespace cddo_users.DTOs
{
    public class PaginatedUserRoleApprovalDetails
    {
        public List<UserRoleApprovalDetail> Approvals { get; set; }
        public int TotalCount { get; set; }
    }
}

