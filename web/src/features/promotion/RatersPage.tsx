import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Breadcrumb, Flex, Skeleton, Typography } from "antd";
import { Link, useParams } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import { isStaleVersion, useEditBase } from "../../app/useEditBase";
import {
  applicationQuery,
  type PromotionApplication,
  promotionQueryKey,
  type RaterEntry,
  saveRaterEmail,
  saveRaters,
  type Versioned,
} from "./promotionApi";
import { RatersView } from "./RatersView";
import { promotionPaths } from "./routes";
import { useRaterOptions } from "./useRaterOptions";

/** The raters of a candidate, which the head of the unit of the target corrects. */
export function RatersPage() {
  const { applicationId } = useParams();
  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Breadcrumb
        items={[
          { title: <Link to={promotionPaths.home}>Карьерные переходы</Link> },
          { title: "Оценивающие" },
        ]}
      />
      <Typography.Title level={1} style={{ margin: 0 }}>
        Оценивающие кандидатов
      </Typography.Title>
      {applicationId && <LoadedRaters id={applicationId} />}
    </Flex>
  );
}

function LoadedRaters({ id }: { id: string }) {
  const { data, error, isPending, refetch } = useQuery(applicationQuery(id));
  if (isPending) return <Skeleton active />;
  if (error) {
    return (
      <ErrorAlert
        title="Не удалось загрузить заявку"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return <RatersEditor latest={data} />;
}

/** The list with the application it was opened with; it takes the reread one only after a save is refused for a stale version. */
function RatersEditor({ latest }: { latest: Versioned<PromotionApplication> }) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const { base = latest, noteSaveFailure, rebase } = useEditBase(latest);
  const employees = useRaterOptions("rater-picker");
  const save = useMutation({
    mutationFn: (raters: RaterEntry[]) =>
      saveRaters(base.id, base.version, raters),
    onSuccess: (saved) => {
      rebase(saved);
      message.success("Список оценивающих сохранён");
    },
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  const confirmEmail = useMutation({
    mutationFn: ({
      employeeId,
      email,
    }: {
      employeeId: string;
      email: string;
    }) => saveRaterEmail(base.id, base.version, employeeId, email),
    onSuccess: (saved) => {
      rebase(saved);
      message.success("Почта подтверждена");
    },
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  return (
    <Flex vertical gap="middle">
      {save.isError && (
        <ErrorAlert title="Список не сохранён" error={save.error} />
      )}
      {confirmEmail.isError && isStaleVersion(confirmEmail.error) && (
        <ErrorAlert title="Почта не подтверждена" error={confirmEmail.error} />
      )}
      <RatersView
        key={base.version}
        application={base}
        candidates={employees.candidates}
        isSearching={employees.isSearching}
        onSearch={employees.onSearch}
        onSubmit={save.mutateAsync}
        onConfirmEmail={(employeeId, email) =>
          confirmEmail.mutateAsync({ employeeId, email })
        }
      />
    </Flex>
  );
}
