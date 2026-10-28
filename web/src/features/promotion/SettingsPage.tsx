import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Flex, Skeleton, Typography } from "antd";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import { useEditBase } from "../../app/useEditBase";
import {
  type PromotionSettings,
  promotionQueryKey,
  type SettingsInput,
  saveSettings,
  settingsQuery,
  type Versioned,
} from "./promotionApi";
import { SettingsView } from "./SettingsView";

/** The promotion settings screen of global administrators. */
export function SettingsPage() {
  const { data, error, isPending, refetch } = useQuery(settingsQuery);
  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Typography.Title level={1} style={{ margin: 0 }}>
        Настройки повышения
      </Typography.Title>
      {isPending && <Skeleton active />}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить настройки"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {data && <SettingsEditor latest={data} />}
    </Flex>
  );
}

/** The form with the settings it was opened with; it takes the reread ones only after a save is refused for a stale version. */
function SettingsEditor({ latest }: { latest: Versioned<PromotionSettings> }) {
  const queryClient = useQueryClient();
  const { base = latest, noteSaveFailure, rebase } = useEditBase(latest);
  const save = useMutation({
    mutationFn: (input: SettingsInput) => saveSettings(base.version, input),
    onSuccess: rebase,
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  return (
    <>
      {save.isError && (
        <ErrorAlert title="Настройки не сохранены" error={save.error} />
      )}
      <SettingsView
        key={base.version}
        settings={base}
        onSubmit={save.mutateAsync}
      />
    </>
  );
}
