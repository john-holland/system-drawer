using System.Threading;
using System.Threading.Tasks;

public interface IContinuumNotificationClient
{
    Task<NotificationsResponse> GetNotificationsAsync(int limit = 20, bool unreadOnly = false, CancellationToken ct = default);
    Task MarkReadAsync(string notificationId, CancellationToken ct = default);
}
