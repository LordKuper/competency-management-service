import { createContext } from "react";

/** Lets a form inside an `AppModal` report a successful save, which clears the dialog's unsaved mark. */
export const ModalSavedContext = createContext<() => void>(() => {});
