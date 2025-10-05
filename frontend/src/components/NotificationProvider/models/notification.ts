import { createEvent, createStore } from "effector";
import { NotificationProps } from "../NotificationProvider.types";

/* events */
const showNotification = createEvent<NotificationProps>();
const notificationShown = createEvent();

/* stores */
const $notifications = createStore<NotificationProps[]>([]);

$notifications.on(showNotification, (notifications, newNotification) => {
  return [...notifications, newNotification];
});

$notifications.on(notificationShown, (notifications) => {
  return notifications.slice(1);
});

const $currentNotification = $notifications.map((notifications) =>
  notifications.length > 0 ? notifications[0] : null
);

export const events = {
  showNotification,
  notificationShown,
};

export const stores = {
  $notifications,
  $currentNotification,
};
