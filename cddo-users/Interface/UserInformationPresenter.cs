namespace cddo_users.Interface;

internal class UserInformationPresenter(
    IHttpContextAccessor httpContextAccessor) : IUserInformationPresenter
{
    string? IUserInformationPresenter.GetUserNameOfInitiatingUser()
    {
        return httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "display_name")?.Value;
    }
}