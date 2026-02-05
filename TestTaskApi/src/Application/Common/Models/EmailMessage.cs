namespace Application.Common.Models;

public record EmailMessage(
    string ToEmail,
    string Subject,
    string Body,
    bool IsHtml);