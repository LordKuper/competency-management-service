import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Button,
  Descriptions,
  Flex,
  Form,
  Select,
  Table,
  type TableColumnsType,
  Typography,
} from "antd";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogSelect, IN_WORK } from "../career/CatalogSelect";
import { useProcessRoles } from "../career/careerApi";
import {
  readTrack,
  scalesQuery,
  type TrackPositions,
  trackPositionsQuery,
  trackQuery,
} from "../career/catalogApi";
import { employees } from "../career/LevelPositionsModal";
import { MatrixErrorAlert } from "../career/MatrixErrorAlert";
import {
  createMatrix,
  createMatrixVersion,
  invalidateMatrices,
  type MatrixVersion,
  matricesQuery,
  matrixQuery,
} from "../career/matrixApi";
import { TrackModal } from "../career/TrackModal";
import { NoPositionTag, PositionGapAction } from "./CompletenessBlock";
import { deskPaths } from "./deskTexts";

type LevelRow = TrackPositions["levels"][number];

interface LevelsTableProps {
  trackId: string;
  trackName: string;
  /** Why linking positions is not possible now, if it is not. */
  unavailable?: string;
}

/** The levels of a track with their positions; a level without an active one carries the word and the way to put it right. */
function LevelsTable({ trackId, trackName, unavailable }: LevelsTableProps) {
  const read = useQuery(trackPositionsQuery(trackId));
  const columns: TableColumnsType<LevelRow> = [
    {
      title: "Уровень",
      key: "level",
      render: (_, level, index) => `${index + 1}. ${level.name}`,
    },
    {
      title: "Должности",
      key: "positions",
      render: (_, level) =>
        level.hasActivePosition ? (
          level.positions
            .map(
              (position) =>
                `${position.name} · ${employees(position.employeeCount)}`,
            )
            .join("; ")
        ) : (
          <NoPositionTag />
        ),
    },
    {
      title: "Действия",
      key: "actions",
      render: (_, level) =>
        !level.hasActivePosition && (
          <PositionGapAction
            trackId={trackId}
            returnLabel="К построению матрицы"
            unavailable={unavailable}
          />
        ),
    },
  ];
  return (
    <section>
      <Typography.Title level={3}>
        {`Уровни трека «${trackName}» и их должности`}
      </Typography.Title>
      <Table<LevelRow>
        size="small"
        rowKey="id"
        columns={columns}
        dataSource={read.data?.levels}
        loading={read.isFetching}
        pagination={false}
        scroll={{ x: "max-content" }}
      />
    </section>
  );
}

interface FormValues {
  orgUnitId?: string;
  scaleId?: string;
  trackId?: string;
}

interface BuilderTrackStepProps {
  /** The version already created; its unit, track and scale can no longer be changed. */
  version?: MatrixVersion;
  preselected: { unitId?: string; trackId?: string };
  onNext: () => void;
  onUnsavedChange: (isUnsaved: boolean) => void;
}

/** Step 1: the unit, the track and the scale. Saving it creates the draft version, or the next version of a published matrix. */
export function BuilderTrackStep({
  version,
  preselected,
  onNext,
  onUnsavedChange,
}: BuilderTrackStepProps) {
  if (version) return <CreatedVersion version={version} onNext={onNext} />;
  return (
    <NewVersionForm
      preselected={preselected}
      onUnsavedChange={onUnsavedChange}
    />
  );
}

function CreatedVersion({
  version,
  onNext,
}: {
  version: MatrixVersion;
  onNext: () => void;
}) {
  const navigate = useNavigate();
  const matrix = useQuery(matrixQuery(version.matrixId));
  const { track, scale } = version.entries;
  return (
    <Flex vertical gap="large">
      <Descriptions
        column={1}
        items={[
          {
            key: "unit",
            label: "Подразделение",
            children: matrix.data?.orgUnitName,
          },
          { key: "track", label: "Трек", children: track.name },
          { key: "scale", label: "Шкала", children: scale.name },
        ]}
      />
      <LevelsTable trackId={track.id} trackName={track.name} />
      <Flex gap="small">
        <Button onClick={() => void navigate(deskPaths.desk)}>Выйти</Button>
        <Button type="primary" onClick={onNext}>
          Далее
        </Button>
      </Flex>
    </Flex>
  );
}

function NewVersionForm({
  preselected,
  onUnsavedChange,
}: Pick<BuilderTrackStepProps, "preselected" | "onUnsavedChange">) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form] = Form.useForm<FormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const [isTrackCreating, setIsTrackCreating] = useState(false);
  const roles = useProcessRoles();
  const defaultScale = useQuery(
    scalesQuery({ page: 1, pageSize: 1, isDefault: true }),
  );
  const orgUnitId = Form.useWatch("orgUnitId", form);
  const trackId = Form.useWatch("trackId", form);
  const headedUnits = roles.data?.headedUnits ?? [];
  const unit = headedUnits.find((item) => item.id === orgUnitId);
  const track = useQuery({
    ...trackQuery(trackId ?? ""),
    enabled: trackId !== undefined,
  });
  const existing = useQuery({
    ...matricesQuery({
      orgUnitId: orgUnitId ?? "",
      trackId: trackId ?? "",
      page: 1,
      pageSize: 1,
    }),
    enabled: orgUnitId !== undefined && trackId !== undefined,
  });
  const matrix = existing.data?.items[0];
  const defaultScaleId = defaultScale.data?.items[0]?.id;

  useEffect(() => onUnsavedChange(isDirty), [isDirty, onUnsavedChange]);
  useEffect(() => {
    if (defaultScaleId && !form.getFieldValue("scaleId")) {
      form.setFieldValue("scaleId", defaultScaleId);
    }
  }, [defaultScaleId, form]);
  useEffect(() => {
    if (headedUnits.length === 1 && !form.getFieldValue("orgUnitId")) {
      form.setFieldValue("orgUnitId", headedUnits[0]?.id);
    }
  }, [headedUnits, form]);

  const create = useMutation({
    mutationFn: async (values: FormValues) => {
      if (!values.orgUnitId || !values.trackId) {
        throw new Error("The unit and the track are required");
      }
      if (matrix?.inWorkVersion) return matrix.inWorkVersion.id;
      if (matrix?.publishedVersion) {
        return (await createMatrixVersion(matrix.id)).id;
      }
      if (!values.scaleId) throw new Error("The scale is required");
      const { directionId } = await readTrack(values.trackId);
      const created = await createMatrix({
        orgUnitId: values.orgUnitId,
        directionId,
        trackId: values.trackId,
        scaleId: values.scaleId,
      });
      if (!created.inWorkVersion)
        throw new Error("The version was not created");
      return created.inWorkVersion.id;
    },
    onSuccess: (versionId) => {
      setIsDirty(false);
      void navigate(deskPaths.matrixBuilderOf(versionId), { replace: true });
    },
    onSettled: () => invalidateMatrices(queryClient),
  });
  const { isSubmitting, submit } = useFormSubmit(form, create.mutateAsync);
  const isLocked = isSubmitting;
  const isScaleFixed = matrix !== undefined;

  return (
    <Form
      form={form}
      name="matrix-builder-track"
      layout="vertical"
      disabled={isLocked}
      initialValues={{
        orgUnitId: preselected.unitId,
        trackId: preselected.trackId,
      }}
      onValuesChange={() => setIsDirty(true)}
      onFinish={submit}
    >
      <Flex vertical gap="middle">
        {roles.isSuccess && headedUnits.length === 0 && (
          <Alert
            type="info"
            showIcon
            title="Построить матрицу может руководитель подразделения"
            description="Вы не руководите ни одним подразделением, поэтому создать матрицу здесь нельзя. Обратитесь к руководителю подразделения."
          />
        )}
        <Form.Item
          name="orgUnitId"
          label="Подразделение"
          rules={[{ required: true }]}
          extra="Показаны подразделения, которыми вы руководите."
        >
          <Select
            options={headedUnits.map((item) => ({
              value: item.id,
              label: item.name,
            }))}
            onChange={() => form.resetFields(["trackId"])}
          />
        </Form.Item>
        <Form.Item
          name="scaleId"
          label="Шкала"
          rules={[{ required: true }]}
          extra={
            isScaleFixed
              ? "Шкала взята из версии, на основе которой строится матрица."
              : "Подставлена шкала по умолчанию; можно выбрать другую."
          }
        >
          <CatalogSelect
            segment="scales"
            filters={IN_WORK}
            disabled={isScaleFixed}
          />
        </Form.Item>
        <Form.Item
          label="Трек"
          required
          extra="Доступны треки вашего подразделения и вышестоящих. Нужного нет — создайте черновой трек, не выходя из сценария."
        >
          <Flex gap="small">
            <Form.Item
              name="trackId"
              noStyle
              rules={[{ required: true, message: "Выберите трек" }]}
            >
              <CatalogSelect
                segment="tracks"
                aria-label="Трек"
                style={{ flex: 1 }}
                filters={{ ...IN_WORK, forOrgUnitId: orgUnitId }}
                disabled={!orgUnitId}
              />
            </Form.Item>
            <Button disabled={!unit} onClick={() => setIsTrackCreating(true)}>
              Создать трек
            </Button>
          </Flex>
        </Form.Item>
        {matrix?.publishedVersion && !matrix.inWorkVersion && (
          <Alert
            type="info"
            showIcon
            title={`У подразделения по этому треку уже есть опубликованная матрица (версия ${matrix.publishedVersion.number}). Будет создана версия ${matrix.publishedVersion.number + 1} на её основе.`}
          />
        )}
        {matrix?.inWorkVersion && (
          <Alert
            type="info"
            showIcon
            title={`По этому треку уже есть версия ${matrix.inWorkVersion.number} в работе. Продолжим с неё.`}
          />
        )}
        {track.data?.status === "Draft" && !matrix?.publishedVersion && (
          <Alert
            type="info"
            showIcon
            title={`Трек «${track.data.name}» — черновой. Он опубликуется вместе с версией, если ответственный HR подразделения вправе его утвердить; иначе публикацию отклонят с названием трека, а версия останется неопубликованной.`}
          />
        )}
        {trackId && track.data && (
          <LevelsTable
            trackId={trackId}
            trackName={track.data.name}
            unavailable="Сохраните шаг, чтобы связать должности"
          />
        )}
        {create.error != null && (
          <MatrixErrorAlert
            title="Не удалось сохранить шаг"
            error={create.error}
          />
        )}
        {existing.error != null && (
          <MatrixErrorAlert
            title="Не удалось проверить существующую матрицу"
            error={existing.error}
          />
        )}
        <Flex gap="small" align="center" wrap>
          <Button
            disabled={isDirty}
            onClick={() => void navigate(deskPaths.desk)}
          >
            Выйти
          </Button>
          <Button
            type="primary"
            htmlType="submit"
            loading={isSubmitting}
            disabled={existing.isFetching}
          >
            Сохранить и далее
          </Button>
          {isDirty && !isSubmitting && (
            <>
              <Typography.Text type="secondary">
                Есть несохранённые правки. Сохраните их, чтобы выйти.
              </Typography.Text>
              <Button
                type="link"
                onClick={() => {
                  form.resetFields();
                  form.setFieldValue("scaleId", defaultScaleId);
                  setIsDirty(false);
                }}
              >
                Отменить правки
              </Button>
            </>
          )}
        </Flex>
      </Flex>
      {isTrackCreating && unit && (
        <TrackModal
          fixedOwner={unit}
          onSaved={(created) => {
            form.setFieldValue("trackId", created.id);
            setIsDirty(true);
          }}
          onClose={() => setIsTrackCreating(false)}
        />
      )}
    </Form>
  );
}
