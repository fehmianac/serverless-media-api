using Amazon.SimpleNotificationService.Util;

namespace Api.Infrastructure;

public static class MessageValidator
{
    public static bool Validate(this Message message)
    {
        if (!message.IsMessageSignatureValid())
        {
            return false;
        }

        if (message.IsNotificationType)
        {
            return true;
        }

        if (!message.IsSubscriptionType)
        {
            return false;
        }

        return false;
    }
}