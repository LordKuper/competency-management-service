import { LOGIN_PATH } from "../../app/featureContract";
import { LoginPage } from "./LoginPage";

/** The sign-in screen lives outside the shell, which would otherwise demand a session. */
export const standaloneRoutes = [{ path: LOGIN_PATH, Component: LoginPage }];
