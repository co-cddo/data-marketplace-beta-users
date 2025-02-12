namespace cddo_users.DTOs
{
    public class UserResponseDto
    {
        public List<UserAdminDto> Users { get; set; }
        public int TotalCount { get; set; }
    }
}
