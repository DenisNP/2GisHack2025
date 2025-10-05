import React, { useCallback, useEffect } from "react";
import { SnackbarKey, SnackbarProvider, useSnackbar } from "notistack";
import { useUnit } from "effector-react";
import { stores, events } from "./models/notification";
import { IconButton } from "@mui/material";
import { AUTO_HIDE_DURATION } from "./NotificationProvider.constants";
import CloseIcon from "@mui/icons-material/Close";

const NotificationSystem = () => {
  const { enqueueSnackbar, closeSnackbar } = useSnackbar();
  const notification = useUnit(stores.$currentNotification);
  const handleClickClose = useCallback(
    (key: SnackbarKey) => () => closeSnackbar(key),
    [closeSnackbar]
  );

  useEffect(() => {
    if (notification) {
      const { message, options } = notification;
      enqueueSnackbar({
        anchorOrigin: {
          vertical: "bottom",
          horizontal: "right",
        },
        preventDuplicate: true,
        autoHideDuration: AUTO_HIDE_DURATION,
        action: (key) => {
          return (
            <IconButton onClick={handleClickClose(key)}>
              <CloseIcon color="secondary" />
            </IconButton>
          );
        },
        message: message,
        ...options,
      });
      events.notificationShown();
    }
  }, [enqueueSnackbar, handleClickClose, notification]);

  return null;
};

export const NotificationProvider: React.FC<React.PropsWithChildren> = ({
  children,
}) => {
  return (
    <SnackbarProvider maxSnack={10}>
      <NotificationSystem />
      {children}
    </SnackbarProvider>
  );
};
