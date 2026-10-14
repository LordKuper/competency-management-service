import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Button,
  Flex,
  Form,
  Input,
  Radio,
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
  MAX_PAGE_SIZE,
  readTrack,
  scalesQuery,
  type TrackPositions,
  trackPositionsQuery,
  trackQuery,
} from "../career/catalogApi";
import { employees } from "../career/LevelPositionsModal";
import { MatrixErrorAlert } from "../career/MatrixErrorAlert";
import { MatrixPositionSelect } from "../career/MatrixPositionSelect";
import {
  createMatrix,
  createMatrixVersion,
  invalidateMatrices,
  type MatrixVersion,
  matricesQuery,
  matrixPositionsQuery,
  matrixQuery,
} from "../career/matrixApi";
import { ScaleModal } from "../career/ScaleModal";
import { ScaleSelect } from "../career/ScaleSelect";
import { TrackModal } from "../career/TrackModal";
import { useCatalogRights } from "../career/useCatalogRights";
import { NoPositionTag, PositionGapAction } from "./CompletenessBlock";
import { deskPaths } from "./deskTexts";
import { ActionBar, CompactAlert, FormColumn } from "./ScenarioContext";
import "./scenario.css";

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
            originName="Построить матрицу компетенций"
            unavailable={unavailable}
          />
        ),
    },
  ];
  return (
    <section>
      <Typography.Title level={4}>
        {`Уровни трека «${trackName}» и их должности`}
      </Typography.Title>
      <Table<LevelRow>
        size="small"
        rowKey="id"
        columns={columns}
        dataSource={read.data?.levels}
        loading={read.isFetching}
        pagination={false}
      />
    </section>
  );
}

interface FormValues {
  orgUnitId?: string;
  scaleId?: string;
  trackId?: string;
  positionId?: string;
}

/** What a matrix is built on: a career track, or a position that has no track. */
export type Basis = "track" | "position";

const BASIS_OPTIONS = [
  { value: "track", label: "По треку" },
  { value: "position", label: "По должности" },
] as const;

interface BuilderTrackStepProps {
  /** The version already created; its basis, unit and scale can no longer be changed. */
  version?: MatrixVersion;
  preselected: {
    unitId?: string;
    trackId?: string;
    positionId?: string;
    positionName?: string;
  };
  basis: Basis;
  onBasisChange: (basis: Basis) => void;
  onNext: () => void;
  onUnsavedChange: (isUnsaved: boolean) => void;
}

/** Step 1: the basis, the unit, the track or the position, and the scale. Saving it creates the draft version, or the next version of a published matrix. */
export function BuilderTrackStep({
  version,
  preselected,
  basis,
  onBasisChange,
  onNext,
  onUnsavedChange,
}: BuilderTrackStepProps) {
  if (version) return <CreatedVersion version={version} onNext={onNext} />;
  return (
    <NewVersionForm
      preselected={preselected}
      basis={basis}
      onBasisChange={onBasisChange}
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
  const matrix = useQuery(matrixQuery(version.matrixId));
  const { track, position, scale } = version.entries;
  return (
    <FormColumn>
      <Form layout="vertical" disabled className="compact-form">
        <Flex vertical gap="middle">
          <Form.Item label="Основа матрицы">
            <Radio.Group
              value={position ? "position" : "track"}
              options={[...BASIS_OPTIONS]}
            />
          </Form.Item>
          <Form.Item label="Подразделение">
            <Input value={matrix.data?.orgUnitName} />
          </Form.Item>
          <Form.Item label={position ? "Должность" : "Трек"}>
            <Input value={position?.name ?? track?.name} />
          </Form.Item>
          <Form.Item label="Шкала">
            <ScaleSelect
              value={scale?.id}
              known={scale ?? undefined}
              disabled
            />
          </Form.Item>
        </Flex>
      </Form>
      {track && <LevelsTable trackId={track.id} trackName={track.name} />}
      <ActionBar>
        <Button type="primary" onClick={onNext}>
          Далее
        </Button>
      </ActionBar>
    </FormColumn>
  );
}

function NewVersionForm({
  preselected,
  basis,
  onBasisChange,
  onUnsavedChange,
}: Pick<
  BuilderTrackStepProps,
  "preselected" | "basis" | "onBasisChange" | "onUnsavedChange"
>) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form] = Form.useForm<FormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const [isTrackCreating, setIsTrackCreating] = useState(false);
  const [isScaleCreating, setIsScaleCreating] = useState(false);
  const rights = useCatalogRights();
  const roles = useProcessRoles();
  const defaultScale = useQuery(
    scalesQuery({ page: 1, pageSize: 1, isDefault: true }),
  );
  const orgUnitId = Form.useWatch("orgUnitId", form);
  const trackId = Form.useWatch("trackId", form);
  const positionId = Form.useWatch("positionId", form);
  const isPosition = basis === "position";
  const headedUnits = roles.data?.headedUnits ?? [];
  const unit = headedUnits.find((item) => item.id === orgUnitId);
  const track = useQuery({
    ...trackQuery(trackId ?? ""),
    enabled: !isPosition && trackId !== undefined,
  });
  const subject = isPosition ? positionId : trackId;
  const existing = useQuery({
    ...matricesQuery({
      orgUnitId: orgUnitId ?? "",
      ...(isPosition ? { positionId } : { trackId }),
      page: 1,
      pageSize: 1,
    }),
    enabled: orgUnitId !== undefined && subject !== undefined,
  });
  const positions = useQuery({
    ...matrixPositionsQuery(orgUnitId ?? "", "", 1, MAX_PAGE_SIZE),
    enabled: isPosition && orgUnitId !== undefined,
  });
  const isNoPositionAvailable =
    positions.data !== undefined &&
    positions.data.total <= MAX_PAGE_SIZE &&
    positions.data.items.every((item) => item.isLinked);
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
      const subjectId = isPosition ? values.positionId : values.trackId;
      if (!values.orgUnitId || !subjectId) {
        throw new Error("The unit and the track or position are required");
      }
      if (matrix?.inWorkVersion) return matrix.inWorkVersion.id;
      if (matrix?.publishedVersion) {
        return (await createMatrixVersion(matrix.id)).id;
      }
      if (!values.scaleId) throw new Error("The scale is required");
      const basisFields = isPosition
        ? { positionId: subjectId }
        : {
            directionId: (await readTrack(subjectId)).directionId,
            trackId: subjectId,
          };
      const created = await createMatrix({
        orgUnitId: values.orgUnitId,
        ...basisFields,
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
      className="compact-form"
      disabled={isLocked}
      initialValues={{
        orgUnitId: preselected.unitId,
        trackId: preselected.trackId,
        positionId: preselected.positionId,
      }}
      onValuesChange={() => setIsDirty(true)}
      onFinish={submit}
    >
      <FormColumn>
        {roles.isSuccess && headedUnits.length === 0 && (
          <CompactAlert
            type="info"
            lead="Построить матрицу может руководитель подразделения."
            text="Вы не руководите ни одним подразделением, поэтому создать матрицу здесь нельзя. Обратитесь к руководителю подразделения."
          />
        )}
        {create.error != null && (
          <MatrixErrorAlert
            title="Не удалось сохранить шаг"
            error={create.error}
          />
        )}
        <Form.Item label="Основа матрицы">
          <Radio.Group
            value={basis}
            options={[...BASIS_OPTIONS]}
            onChange={(event) => {
              onBasisChange(event.target.value as Basis);
              form.resetFields(["trackId", "positionId"]);
              setIsDirty(true);
            }}
          />
        </Form.Item>
        {isPosition && (
          <CompactAlert
            type="info"
            lead="Матрица должности обычно нужна для управленческих должностей, которые не связаны напрямую с карьерными треками специалистов."
          />
        )}
        <Form.Item
          name="orgUnitId"
          label="Подразделение"
          rules={[{ required: true }]}
        >
          <Select
            options={headedUnits.map((item) => ({
              value: item.id,
              label: item.name,
            }))}
            onChange={() => form.resetFields(["trackId", "positionId"])}
          />
        </Form.Item>
        {isPosition ? (
          isNoPositionAvailable ? (
            <Form.Item label="Должность">
              <CompactAlert
                type="warning"
                lead={`Все должности подразделения «${unit?.name ?? ""}» и вышестоящих связаны с уровнями треков или не входят в перечень.`}
                text="Добавьте должность в перечень в «Справочники → Оргструктура»."
              />
            </Form.Item>
          ) : (
            <Form.Item
              name="positionId"
              label="Должность"
              rules={[{ required: true, message: "Выберите должность" }]}
            >
              <MatrixPositionSelect
                orgUnitId={orgUnitId}
                knownName={preselected.positionName}
              />
            </Form.Item>
          )
        ) : (
          <Form.Item label="Трек" required>
            <Flex gap="small">
              <Form.Item
                name="trackId"
                noStyle
                rules={[{ required: true, message: "Выберите трек" }]}
              >
                <CatalogSelect
                  segment="tracks"
                  aria-label="Трек"
                  style={{ flex: 1, minWidth: 0 }}
                  filters={{ ...IN_WORK, forOrgUnitId: orgUnitId }}
                  disabled={!orgUnitId}
                />
              </Form.Item>
              <Button disabled={!unit} onClick={() => setIsTrackCreating(true)}>
                Добавить трек
              </Button>
            </Flex>
          </Form.Item>
        )}
        <Form.Item label="Шкала" required>
          <Flex gap="small">
            <Form.Item
              name="scaleId"
              noStyle
              rules={[{ required: true, message: "Выберите шкалу" }]}
            >
              <ScaleSelect
                aria-label="Шкала"
                style={{ flex: 1, minWidth: 0 }}
                disabled={isScaleFixed}
              />
            </Form.Item>
            {rights.canCreate && (
              <Button
                disabled={isScaleFixed}
                onClick={() => setIsScaleCreating(true)}
              >
                Добавить шкалу
              </Button>
            )}
          </Flex>
        </Form.Item>
        {matrix?.inWorkVersion ? (
          <CompactAlert
            type="info"
            lead={
              isPosition
                ? `У подразделения уже есть матрица на должность «${matrix.positionName}»: версия ${matrix.inWorkVersion.number} в работе.`
                : `По этому треку уже есть версия ${matrix.inWorkVersion.number} в работе.`
            }
            text="Продолжим с неё."
          />
        ) : matrix?.publishedVersion ? (
          <CompactAlert
            type="info"
            lead={
              isPosition
                ? `У подразделения уже есть матрица на должность «${matrix.positionName}»: версия ${matrix.publishedVersion.number} опубликована.`
                : `У подразделения по этому треку уже есть опубликованная матрица (версия ${matrix.publishedVersion.number}).`
            }
            text={`Будет создана версия ${matrix.publishedVersion.number + 1} на её основе.`}
          />
        ) : (
          !isPosition &&
          track.data?.status === "Draft" && (
            <CompactAlert
              type="info"
              lead={`Трек «${track.data.name}» — черновой.`}
              text="Он опубликуется вместе с версией, если ответственный HR подразделения вправе его утвердить; иначе публикацию отклонят, а версия останется неопубликованной."
            />
          )
        )}
        {!isPosition && trackId && track.data && (
          <LevelsTable
            trackId={trackId}
            trackName={track.data.name}
            unavailable="Сохраните шаг, чтобы связать должности"
          />
        )}
        {existing.error != null && (
          <MatrixErrorAlert
            title="Не удалось проверить существующую матрицу"
            error={existing.error}
          />
        )}
        <ActionBar>
          {isDirty && !isSubmitting && (
            <>
              <Typography.Text type="secondary">
                Есть несохранённые правки. Сохраните их, чтобы перейти на другой
                шаг.
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
          <Button
            type="primary"
            htmlType="submit"
            loading={isSubmitting}
            disabled={existing.isFetching}
          >
            Сохранить и далее
          </Button>
        </ActionBar>
      </FormColumn>
      {isScaleCreating && (
        <ScaleModal
          onSaved={(created) => {
            form.setFieldValue("scaleId", created.id);
            setIsDirty(true);
          }}
          onClose={() => setIsScaleCreating(false)}
        />
      )}
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
