import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Form, Input, Select, Typography, theme } from "antd";
import { useState } from "react";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { OrgUnitSelect } from "../org-structure/OrgUnitSelect";
import { CatalogModal, FormActions } from "./CatalogModal";
import {
  type CareerTrack,
  invalidateCatalogs,
  levelsQuery,
  orderLevels,
  saveNamedEntry,
} from "./catalogApi";
import { createLevel } from "./LevelModal";
import { arrangeLevels, type PendingLevel, TrackLevels } from "./TrackLevels";
import { useCatalogRights } from "./useCatalogRights";

const NAME_MAX_LENGTH = 200;
const FIELDS = ["name", "ownerOrgUnitId"] as const;

interface TrackValues {
  name: string;
  ownerOrgUnitId?: string;
}

interface TrackModalProps {
  directionId: string;
  /** Track being edited as the cache holds it now; omitted when one is created. */
  track?: CareerTrack;
  onClose: () => void;
}

/**
 * Dialog that creates or edits a track together with its levels. «Сохранить» saves the track first, then each level
 * added here, one after another, and then the order of the levels if it was changed. If a step fails, the steps before
 * it stay saved: the dialog stays open on the track as saved, keeps the order, lists as pending only the levels that are
 * left, and a second «Сохранить» retries the rest.
 */
export function TrackModal({
  directionId,
  track: latest,
  onClose,
}: TrackModalProps) {
  const queryClient = useQueryClient();
  const { token } = theme.useToken();
  const rights = useCatalogRights();
  const [form] = Form.useForm<TrackValues>();
  const { base, noteSaveFailure } = useEditBase(latest);
  const [saved, setSaved] = useState<CareerTrack>();
  const [pending, setPending] = useState<PendingLevel[]>([]);
  const [order, setOrder] = useState<string[]>();
  const current = saved ?? base;
  const levels = useQuery({
    ...levelsQuery(current?.id),
    enabled: current !== undefined,
  });
  const rows = arrangeLevels(
    current ? (levels.data?.items ?? []) : [],
    pending,
    order,
  );
  const save = useMutation({
    mutationFn: async (values: TrackValues) => {
      let track = current;
      if (!track || values.name !== track.name) {
        track = (await saveNamedEntry(
          "tracks",
          { ...values, directionId },
          track,
        )) as CareerTrack;
        setSaved(track);
      }
      const createdIds = new Map<string, string>();
      let rest = pending;
      for (const level of pending) {
        const created = await createLevel(track.id, level.values);
        createdIds.set(level.key, created.id);
        rest = rest.slice(1);
        setPending(rest);
        setOrder((keys) =>
          keys?.map((key) => (key === level.key ? created.id : key)),
        );
      }
      if (order) {
        setSaved(
          await orderLevels(
            track,
            rows.map((row) => createdIds.get(row.key) ?? row.key),
          ),
        );
      }
    },
    onSuccess: onClose,
    onError: noteSaveFailure,
    onSettled: () => invalidateCatalogs(queryClient),
  });
  const { isSubmitting, submit } = useFormSubmit(form, save.mutateAsync);

  return (
    <CatalogModal
      title={latest ? "Изменение трека" : "Новый трек"}
      error={save.error}
      fields={FIELDS}
      onClose={onClose}
    >
      <Form
        key={base?.version}
        form={form}
        name="career-track"
        layout="vertical"
        initialValues={{ name: current?.name ?? "" }}
        onFinish={submit}
      >
        {!current && (
          <Form.Item
            name="ownerOrgUnitId"
            label="Подразделение"
            rules={[{ required: true }]}
          >
            {rights.isAdmin ? (
              <OrgUnitSelect isActiveRequired />
            ) : (
              <Select
                options={rights.ownerUnits.map((unit) => ({
                  value: unit.id,
                  label: unit.name,
                }))}
              />
            )}
          </Form.Item>
        )}
        <Form.Item
          name="name"
          label="Название"
          rules={[
            { required: true, whitespace: true, message: "Укажите название" },
            {
              max: NAME_MAX_LENGTH,
              message: `Не длиннее ${NAME_MAX_LENGTH} символов`,
            },
          ]}
        >
          <Input autoComplete="off" />
        </Form.Item>
        <section style={{ marginBottom: token.marginLG }}>
          <Typography.Title level={5}>Уровни</Typography.Title>
          <TrackLevels
            track={current}
            rows={rows}
            isLoading={levels.isFetching}
            error={levels.error}
            onRetry={() => void levels.refetch()}
            pending={pending}
            onPendingChange={setPending}
            onOrderChange={setOrder}
          />
        </section>
        <FormActions isSubmitting={isSubmitting} onCancel={onClose} />
      </Form>
    </CatalogModal>
  );
}
