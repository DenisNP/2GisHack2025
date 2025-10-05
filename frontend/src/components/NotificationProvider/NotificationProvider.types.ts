import { OptionsWithExtraProps, SnackbarMessage, VariantType } from "notistack";

export type NotificationProps = {
  options: OptionsWithExtraProps<VariantType>;
  message?: SnackbarMessage;
};

export type ShowNotificationProps = {
  variant: VariantType;
  message?: SnackbarMessage;
  autoHideDuration?: number;
};
