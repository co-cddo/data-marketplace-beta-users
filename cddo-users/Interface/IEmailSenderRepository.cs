namespace cddo_users.Core
{
    public interface IEmailSenderRepository
    {
        /// <summary>
        /// Sends an email asynchronously.
        /// </summary>
        /// <param name="to">The recipient's email address.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body of the email.</param>
        /// <param name="isBodyHtml">Indicates whether the body is HTML (true) or plain text (false).</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task SendEmailAsync(string to, string subject, string body, bool isBodyHtml = false);
    }

}
