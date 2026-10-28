import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Drawer, Form, Skeleton } from "antd";
import { type ReactNode, useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useLeaveGuard } from "../../app/leaveGuard";
import { ModalSavedContext } from "../../app/modalSaved";
import { isStaleVersion, useEditBase } from "../../app/useEditBase";
import { FormalView } from "./FormalView";
import {
  applicationQuery,
  type FormalInput,
  type PromotionApplication,
  promotionQueryKey,
  saveFormal,
  saveRaterEmail,
  settingsQuery,
  type Versioned,
  type Wave,
  type WaveApplication,
} from "./promotionApi";
import { formatDate, STAGE } from "./promotionTexts";

interface FormalDialogProps {
  /** The row of the list the check was opened from. */
  application: WaveApplication;
  /** The session the application belongs to, whose dates the check names. */
  wave: Wave;
  /** Opens the next application of the list; omitted when there is none. */
  onNext?: () => void;
  onClose: () => void;
}

interface UnsavedDrawerProps {
  title: ReactNode;
  onClose: () => void;
  children: ReactNode;
}

/** The side panel of the application; closing it by the cross, Escape or the scrim asks for confirmation while a form in it is changed and not saved. */
function UnsavedDrawer({ title, onClose, children }: UnsavedDrawerProps) {
  const [isTouched, setIsTouched] = useState(false);
  const { confirmLeave } = useLeaveGuard({ isUnsaved: isTouched });
  return (
    <Drawer
      open
      size="large"
      title={title}
      onClose={() => (isTouched ? confirmLeave(onClose) : onClose())}
    >
      <Form.Provider
        onFormChange={(_, { changedFields }) => {
          if (changedFields.some((field) => field.touched)) {
            setIsTouched(true);
          }
        }}
      >
        <ModalSavedContext value={() => setIsTouched(false)}>
          {children}
        </ModalSavedContext>
      </Form.Provider>
    </Drawer>
  );
}

/** Side panel with the data for the formal check of an application. Mount it only while it is shown, so every opening starts from the current record. */
export function FormalDialog({
  application,
  wave,
  onNext,
  onClose,
}: FormalDialogProps) {
  const { data, error, isPending, refetch } = useQuery(
    applicationQuery(application.id),
  );
  const settings = useQuery(settingsQuery);
  const checkEndsOn = wave.endsOn[STAGE.formalCheck - 1];
  return (
    <UnsavedDrawer
      title={`Формальная проверка${checkEndsOn ? ` · до ${formatDate(checkEndsOn)}` : ""}`}
      onClose={onClose}
    >
      {isPending && <Skeleton active />}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить заявку"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {data && (
        <FormalEditor
          latest={data}
          submittedAt={application.submittedAt}
          minIntervalMonths={settings.data?.minIntervalMonths}
          publishesOn={wave.endsOn[STAGE.review - 1]}
          checkEndsOn={checkEndsOn}
          onNext={onNext}
        />
      )}
    </UnsavedDrawer>
  );
}

interface FormalEditorProps {
  latest: Versioned<PromotionApplication>;
  submittedAt: WaveApplication["submittedAt"];
  minIntervalMonths?: number;
  publishesOn?: string;
  checkEndsOn?: string;
  onNext?: () => void;
}

/** The check with the application it was opened with, which stays open after a save to show the result the data would give; it takes the reread record only after a save is refused for a stale version. */
function FormalEditor({
  latest,
  submittedAt,
  minIntervalMonths,
  publishesOn,
  checkEndsOn,
  onNext,
}: FormalEditorProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const { base = latest, noteSaveFailure, rebase } = useEditBase(latest);
  const settle = {
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  };
  const save = useMutation({
    mutationFn: (input: FormalInput) =>
      saveFormal(base.id, base.version, input),
    ...settle,
    onSuccess: (saved) => {
      rebase(saved);
      message.success("Данные сохранены");
    },
  });
  const confirmEmail = useMutation({
    mutationFn: ({
      employeeId,
      email,
    }: {
      employeeId: string;
      email: string;
    }) => saveRaterEmail(base.id, base.version, employeeId, email),
    ...settle,
    onSuccess: (saved) => {
      rebase(saved);
      message.success("Почта подтверждена");
    },
  });
  const canEnter =
    base.roles.includes("ResponsibleHr") &&
    base.stage <= STAGE.formalCheck &&
    base.status === "Submitted";
  return (
    <>
      {save.isError && (
        <ErrorAlert title="Данные не сохранены" error={save.error} />
      )}
      {confirmEmail.isError && isStaleVersion(confirmEmail.error) && (
        <ErrorAlert title="Почта не подтверждена" error={confirmEmail.error} />
      )}
      <FormalView
        application={base}
        submittedAt={submittedAt}
        canEnter={canEnter}
        minIntervalMonths={minIntervalMonths}
        publishesOn={publishesOn}
        checkEndsOn={checkEndsOn}
        onNext={onNext}
        onSubmit={save.mutateAsync}
        onConfirmEmail={(employeeId, email) =>
          confirmEmail.mutateAsync({ employeeId, email })
        }
      />
    </>
  );
}
