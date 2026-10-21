import { App } from "antd";
import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useId,
  useMemo,
  useReducer,
  useRef,
} from "react";
import { useBlocker } from "react-router";
import { LOGIN_PATH } from "./featureContract";

/** What a screen or dialog tells the guard about its edits. */
export interface LeaveEdits {
  /** Edits are not saved yet. */
  isUnsaved: boolean;
  /** A save is in flight: leaving waits for it. */
  isSaving?: boolean;
}

/** Edits of a screen with nothing to lose. */
export const NO_LEAVE_EDITS: LeaveEdits = { isUnsaved: false, isSaving: false };

interface LeaveGuard {
  setEdits: (id: string, edits: LeaveEdits | undefined) => void;
  confirmLeave: (action: () => void) => void;
  leaveAfterSave: (action: () => void) => void;
}

const NO_GUARD: LeaveGuard = {
  setEdits: () => {},
  confirmLeave: (action) => action(),
  leaveAfterSave: (action) => action(),
};

const LeaveGuardContext = createContext<LeaveGuard>(NO_GUARD);
const ModalUnsavedContext = createContext(false);

/** Tells the guard whether the dialog around its children holds unsaved edits; `useLeaveGuard().confirmClose` reads it. */
export const ModalUnsavedProvider = ModalUnsavedContext.Provider;

const DIALOG = {
  title: "Перейти без сохранения?",
  content:
    "Несохранённые правки будут потеряны. Чтобы их сохранить, останьтесь на экране.",
  leave: "Перейти без сохранения",
  stay: "Остаться",
};

function warnBeforeUnload(event: BeforeUnloadEvent) {
  event.preventDefault();
  event.returnValue = true;
}

/**
 * The single guard against losing edits: asks for confirmation on router navigation, wizard steps, sign-out and closing
 * a dialog, and lets the browser warn on closing or reloading the tab. Never forbids leaving.
 */
export function LeaveGuardProvider({ children }: { children: ReactNode }) {
  const { modal } = App.useApp();
  const edits = useRef(new Map<string, LeaveEdits>());
  const [, rerender] = useReducer((tick: number) => tick + 1, 0);
  const isDialogOpen = useRef(false);
  const isPassed = useRef(false);

  const isUnsaved = useCallback(
    () => [...edits.current.values()].some((entry) => entry.isUnsaved),
    [],
  );
  const isSaving = () =>
    [...edits.current.values()].some((entry) => entry.isSaving);
  const isGuarded = isUnsaved() || isSaving();

  const setEdits = useCallback((id: string, next: LeaveEdits | undefined) => {
    if (next) edits.current.set(id, next);
    else edits.current.delete(id);
    rerender();
  }, []);

  const ask = useCallback(
    (onLeave: () => void, onStay?: () => void) => {
      if (isDialogOpen.current) return;
      isDialogOpen.current = true;
      modal.confirm({
        title: DIALOG.title,
        content: DIALOG.content,
        okText: DIALOG.leave,
        okType: "default",
        cancelText: DIALOG.stay,
        cancelButtonProps: { type: "primary" },
        autoFocusButton: "cancel",
        maskClosable: true,
        footer: (_, { OkBtn, CancelBtn }) => (
          <>
            <OkBtn />
            <CancelBtn />
          </>
        ),
        onOk: onLeave,
        onCancel: onStay,
        afterClose: () => {
          isDialogOpen.current = false;
        },
      });
    },
    [modal],
  );

  const confirmLeave = useCallback(
    (action: () => void) => (isUnsaved() ? ask(action) : action()),
    [ask, isUnsaved],
  );

  const leaveAfterSave = useCallback((action: () => void) => {
    isPassed.current = true;
    action();
    setTimeout(() => {
      isPassed.current = false;
    });
  }, []);

  const blocker = useBlocker(({ currentLocation, nextLocation }) => {
    if (isPassed.current) {
      isPassed.current = false;
      return false;
    }
    if (nextLocation.pathname === LOGIN_PATH) return false;
    if (
      nextLocation.pathname === currentLocation.pathname &&
      nextLocation.search === currentLocation.search
    ) {
      return false;
    }
    return isUnsaved() || isSaving();
  });

  const isSavingNow = isSaving();
  useEffect(() => {
    if (blocker.state !== "blocked" || isSavingNow) return;
    if (isUnsaved()) ask(blocker.proceed, blocker.reset);
    else blocker.proceed();
  }, [ask, blocker, isSavingNow, isUnsaved]);

  useEffect(() => {
    if (!isGuarded) return;
    window.addEventListener("beforeunload", warnBeforeUnload);
    return () => window.removeEventListener("beforeunload", warnBeforeUnload);
  }, [isGuarded]);

  const guard = useMemo(
    () => ({ setEdits, confirmLeave, leaveAfterSave }),
    [setEdits, confirmLeave, leaveAfterSave],
  );
  return (
    <LeaveGuardContext.Provider value={guard}>
      {children}
    </LeaveGuardContext.Provider>
  );
}

/**
 * Registers the caller's edits with the guard until it unmounts (nothing without an argument) and returns the ways to
 * leave: `confirmLeave` asks when any registered edits are unsaved, `confirmClose` when the dialog around the caller is
 * unsaved, `leaveAfterSave` runs a navigation right after the caller's own successful save without asking.
 */
export function useLeaveGuard(registered?: LeaveEdits) {
  const guard = useContext(LeaveGuardContext);
  const isModalUnsaved = useContext(ModalUnsavedContext);
  const id = useId();
  const isUnsaved = registered?.isUnsaved;
  const isSaving = registered?.isSaving;
  const { setEdits } = guard;
  useEffect(() => {
    if (isUnsaved === undefined) return;
    setEdits(id, { isUnsaved, isSaving });
    return () => setEdits(id, undefined);
  }, [id, isUnsaved, isSaving, setEdits]);

  const { confirmLeave, leaveAfterSave } = guard;
  const confirmClose = useCallback(
    (action: () => void) => (isModalUnsaved ? confirmLeave(action) : action()),
    [isModalUnsaved, confirmLeave],
  );
  return { confirmLeave, confirmClose, leaveAfterSave };
}
