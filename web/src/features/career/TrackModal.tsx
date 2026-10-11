import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Form, Input, Select, Typography, theme } from "antd";
import { useState } from "react";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { OrgUnitSelect } from "../org-structure/OrgUnitSelect";
import { CatalogModal, FormActions } from "./CatalogModal";
import {
  type CareerTrack,
  invalidateCatalogs,
  saveNamedEntry,
} from "./catalogApi";
import { createLevel, type LevelValues } from "./LevelModal";
import { LevelsTable, PendingLevels } from "./TrackLevels";
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
 * Dialog that creates or edits a track together with its levels. «Сохранить» saves the track first and then each level
 * added here, one after another. If a level fails, the track and the levels before it stay saved: the dialog stays open
 * on the track as saved, lists only the levels that are left, and a second «Сохранить» retries them.
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
  // A track this dialog created or renamed, as the server returned it; it stands in for `base` when none is edited.
  const [saved, setSaved] = useState<{
    id: string;
    name: string;
    version: number;
  }>();
  const [pending, setPending] = useState<LevelValues[]>([]);
  const current = saved ?? base;
  const watchedUnit = Form.useWatch("ownerOrgUnitId", form);
  const unitId = latest?.ownerOrgUnitId ?? watchedUnit;
  const save = useMutation({
    mutationFn: async (values: TrackValues) => {
      let track = current;
      if (!track || values.name !== track.name) {
        track = await saveNamedEntry(
          "tracks",
          { ...values, directionId },
          track,
        );
        setSaved(track);
      }
      let rest = pending;
      for (const level of pending) {
        await createLevel(track.id, level);
        rest = rest.slice(1);
        setPending(rest);
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
        {unitId && (
          <section style={{ marginBottom: token.marginLG }}>
            <Typography.Title level={5}>Уровни</Typography.Title>
            {latest && <LevelsTable track={latest} editable />}
            <PendingLevels
              ownerOrgUnitId={unitId}
              levels={pending}
              onChange={setPending}
            />
          </section>
        )}
        <FormActions isSubmitting={isSubmitting} onCancel={onClose} />
      </Form>
    </CatalogModal>
  );
}
