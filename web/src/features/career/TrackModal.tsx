import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Form, Input, Select, Typography, theme } from "antd";
import { useState } from "react";
import { isStaleVersion, useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { OrgUnitSelect } from "../org-structure/OrgUnitSelect";
import { CatalogModal, FormActions } from "./CatalogModal";
import {
  type CareerTrack,
  invalidateCatalogs,
  levelsQuery,
  orderLevels,
  readTrack,
  saveNamedEntry,
} from "./catalogApi";
import { DirectionSelect } from "./DirectionSelect";
import { createLevel } from "./LevelModal";
import { arrangeLevels, type PendingLevel, TrackLevels } from "./TrackLevels";
import { useCatalogRights } from "./useCatalogRights";

const NAME_MAX_LENGTH = 200;
const FIELDS = ["name", "ownerOrgUnitId", "directionId"] as const;

interface TrackValues {
  name: string;
  ownerOrgUnitId?: string;
  directionId?: string;
}

interface TrackModalProps {
  /** Direction a new track starts with, which the user may change; omitted when the dialog asks for it. */
  directionId?: string;
  /** Track being edited as the cache holds it now; omitted when one is created. */
  track?: CareerTrack;
  /** The unit a new track is created in, when the user has no choice of it. */
  fixedOwner?: { id: string; name: string };
  /** Called with the track a save produced, as the dialog closes. */
  onSaved?: (track: CareerTrack) => void;
  onClose: () => void;
}

/**
 * Dialog that creates or edits a track together with its levels. «Сохранить» saves the track first, then each level
 * added here, one after another, and then the order of the levels if it was changed. If a step fails, the steps before
 * it stay saved: the dialog stays open on the track as saved, keeps the order, lists as pending only the levels that are
 * left, and a second «Сохранить» retries the rest without creating the track again. The track is the one saved here when
 * the dialog created it, read again after a stale-version refusal, and otherwise the newer of the one saved here and the
 * form base, which moves to the reread track after such a refusal; either way the next save sends the reread version.
 */
export function TrackModal({
  directionId,
  track: latest,
  fixedOwner,
  onSaved,
  onClose,
}: TrackModalProps) {
  const queryClient = useQueryClient();
  const { token } = theme.useToken();
  const rights = useCatalogRights();
  const [form] = Form.useForm<TrackValues>();
  const { base, noteSaveFailure } = useEditBase(latest);
  const name = Form.useWatch("name", form);
  const [saved, setSaved] = useState<CareerTrack>();
  const [pending, setPending] = useState<PendingLevel[]>([]);
  const [order, setOrder] = useState<string[]>();
  const current =
    saved && (!base || saved.version > base.version) ? saved : base;
  const levels = useQuery({
    ...levelsQuery(current?.id ?? ""),
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
      try {
        if (!track || values.name !== track.name) {
          track = (await saveNamedEntry(
            "tracks",
            values,
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
          track = await orderLevels(
            track,
            rows.map((row) => createdIds.get(row.key) ?? row.key),
          );
          setSaved(track);
        }
        return track;
      } catch (error) {
        if (!latest && track && isStaleVersion(error)) {
          await readTrack(track.id).then(setSaved, () => undefined);
        }
        throw error;
      }
    },
    onSuccess: (saved) => {
      onSaved?.(saved);
      onClose();
    },
    onError: noteSaveFailure,
    onSettled: () => invalidateCatalogs(queryClient),
  });
  const { isSubmitting, submit } = useFormSubmit(form, save.mutateAsync);

  return (
    <CatalogModal
      title={latest ? "Изменение трека" : "Новый трек"}
      error={save.error}
      fields={FIELDS}
      isUnsaved={
        pending.length > 0 ||
        order !== undefined ||
        (name !== undefined && name !== (current?.name ?? ""))
      }
      onClose={onClose}
    >
      <Form
        key={base?.version}
        form={form}
        name="career-track"
        disabled={isSubmitting}
        layout="vertical"
        initialValues={{
          name: current?.name ?? "",
          ownerOrgUnitId: fixedOwner?.id,
          directionId,
        }}
        onFinish={submit}
      >
        {!current && (
          <Form.Item
            name="ownerOrgUnitId"
            label="Подразделение"
            rules={[{ required: true }]}
            extra={fixedOwner && "Трек создаётся в вашем подразделении."}
          >
            {fixedOwner ? (
              <Select
                disabled
                options={[{ value: fixedOwner.id, label: fixedOwner.name }]}
              />
            ) : rights.isAdmin ? (
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
        {!current && (
          <Form.Item
            name="directionId"
            label="Область"
            rules={[{ required: true }]}
          >
            <DirectionSelect />
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
