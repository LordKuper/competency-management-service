import { LOGIN_PATH } from "../../app/featureContract";
import { FORGOT_PASSWORD_PATH, ForgotPasswordPage } from "./ForgotPasswordPage";
import {
  ACCEPT_INVITATION_PATH,
  AcceptInvitationPage,
  RESET_PASSWORD_PATH,
  ResetPasswordPage,
} from "./LinkPasswordPage";
import { LoginPage } from "./LoginPage";

/**
 * Screens used without a session live outside the shell, which would otherwise demand one. The addresses of the
 * link screens are fixed by the e-mails the server sends.
 */
export const standaloneRoutes = [
  { path: LOGIN_PATH, Component: LoginPage },
  { path: FORGOT_PASSWORD_PATH, Component: ForgotPasswordPage },
  { path: ACCEPT_INVITATION_PATH, Component: AcceptInvitationPage },
  { path: RESET_PASSWORD_PATH, Component: ResetPasswordPage },
];
