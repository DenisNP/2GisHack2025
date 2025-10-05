import { ShowNotificationProps } from "../components/NotificationProvider";
import { events } from "../components/NotificationProvider/models/notification";

export const showNotification = ({
  variant,
  autoHideDuration,
  message,
}: ShowNotificationProps) => {
  events.showNotification({
    message,
    options: { variant, autoHideDuration },
  });
};
