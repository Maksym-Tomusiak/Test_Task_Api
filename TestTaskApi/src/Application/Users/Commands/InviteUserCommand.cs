using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Services.Emails;
using Application.Common.Models;
using Application.Users.Exceptions;
using Domain.Invites;
using LanguageExt;
using Microsoft.Extensions.Configuration;

namespace Application.Users.Commands;

public record InviteUserCommand(string Email);

public class InviteUserCommandHandler
{
    public static async Task<Either<UserException, string>> Handle(
        InviteUserCommand command,
        IInviteRepository inviteRepository,
        IInviteQueries inviteQueries,
        IConfiguration configuration,
        IBackgroundEmailQueue emailQueue,
        CancellationToken cancellationToken)
    {
        var existingInvite = await inviteQueries.GetByTargetEmail(command.Email, cancellationToken);
        if (existingInvite != null)
        {
            return new UserAlreadyInvitedException(command.Email);
        }

        var invite = Invite.New(command.Email, DateTime.Now + TimeSpan.FromDays(7));
        await inviteRepository.Add(invite, cancellationToken);
        
        var frontendUrl = configuration.GetValue<string>("FrontendUrl") ?? "https://localhost";
        
        var message = new EmailMessage(
            ToEmail: command.Email,
            Subject: $"Invitation to join test task",
            Body: $"""<br/><a href="{frontendUrl}/invite/{invite.Code}" target="_blank">Перейти до реєстрації</a>""",
            IsHtml: true);
        
        await emailQueue.QueueEmail(message);
        
        return "User invited successfully.";
    }
}