import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Descriptions,
  Flex,
  Form,
  Input,
  Select,
  Skeleton,
  Space,
  Steps,
  Tag,
  Typography,
} from "antd";
import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { AppModal } from "../../app/AppModal";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useProcessRoles } from "./careerApi";
import { MatrixActions, RETURN_LABEL } from "./MatrixActions";
import { MatrixEditor } from "./MatrixEditor";
import { MatrixErrorAlert } from "./MatrixErrorAlert";
import { MatrixHistoryModal } from "./MatrixHistory";
import {
  createMatrixVersion,
  invalidateMatrices,
  type MatrixDecision,
  type MatrixTransition,
  type MatrixVersion,
  matrixDiffQuery,
  matrixHistoryQuery,
  matrixQuery,
  matrixVersionQuery,
  matrixVersionsQuery,
  returnMatrixVersion,
  transitMatrixVersion,
} from "./matrixApi";
import {
  DECISION_TEXTS,
  STEP_TITLES,
  VERSION_STATUS_LABELS,
} from "./matrixLabels";

const STATUS_COLORS: Record<MatrixVersion["status"], string | undefined> = {
  Draft: undefined,
  OnReview: "processing",
  Returned: "warning",
  Approved: "success",
  Published: "success",
  Archived: undefined,
};

const DATE_TIME = new Intl.DateTimeFormat("ru-RU", {
  dateStyle: "short",
  timeStyle: "short",
});

type Transit = { version: MatrixVersion } & (
  | { transition: MatrixTransition }
  | { comment: string }
);

function currentStep(version: MatrixVersion): number {
  switch (version.status) {
    case "Draft":
    case "Returned":
      return 0;
    case "OnReview":
      return version.step === "Hr" ? 2 : 1;
    case "Approved":
      return 3;
    default:
      return STEP_TITLES.length;
  }
}

interface RouteStepsProps {
  version: MatrixVersion;
  decisions: readonly MatrixDecision[];
}

function RouteSteps({ version, decisions }: RouteStepsProps) {
  const noteAt = (step: "Superior" | "Hr") => {
    const decision = decisions.find(
      (item) =>
        item.step === step &&
        (item.kind === "Skipped" || item.kind === "AutoApproved"),
    );
    return decision && DECISION_TEXTS[decision.kind];
  };
  return (
    <Steps
      orientation="vertical"
      current={currentStep(version)}
      items={[
        { title: STEP_TITLES[0], content: version.authorName },
        {
          title: STEP_TITLES[1],
          content: noteAt("Superior") ?? version.superiorApprover?.name,
        },
        {
          title: STEP_TITLES[2],
          content: noteAt("Hr") ?? version.hr?.name,
        },
        {
          title: STEP_TITLES[3],
          content:
            version.publishedAt &&
            DATE_TIME.format(new Date(version.publishedAt)),
        },
      ]}
    />
  );
}

interface ReturnModalProps {
  isPending: boolean;
  onReturn: (comment: string) => void;
  onClose: () => void;
}

function ReturnModal({ isPending, onReturn, onClose }: ReturnModalProps) {
  const [form] = Form.useForm<{ comment?: string }>();
  return (
    <AppModal
      open
      title={RETURN_LABEL}
      okText={RETURN_LABEL}
      cancelText="Отмена"
      confirmLoading={isPending}
      onOk={() => form.submit()}
      onCancel={onClose}
    >
      <Form
        form={form}
        name="matrix-return"
        layout="vertical"
        onFinish={(values) => onReturn(values.comment ?? "")}
      >
        <Form.Item name="comment" label="Комментарий">
          <Input.TextArea autoSize={{ minRows: 3 }} />
        </Form.Item>
      </Form>
    </AppModal>
  );
}

/** A matrix: its versions, the route a version takes, the editor of its content and the actions the user may take. */
export function MatrixPage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const roles = useProcessRoles();
  const matrixRead = useQuery(matrixQuery(id));
  const versionsRead = useQuery(matrixVersionsQuery(id));
  const [chosenId, setChosenId] = useState<string>();
  const [session, setSession] = useState(0);
  const [dialog, setDialog] = useState<"return" | "history" | null>(null);
  const [hasUnsaved, setHasUnsaved] = useState(false);

  const matrix = matrixRead.data;
  const latestVersionId = [...(versionsRead.data ?? [])].sort(
    (a, b) => b.number - a.number,
  )[0]?.id;
  const versionId =
    chosenId ??
    matrix?.inWorkVersion?.id ??
    matrix?.publishedVersion?.id ??
    latestVersionId ??
    "";
  const versionRead = useQuery({
    ...matrixVersionQuery(versionId),
    enabled: versionId !== "",
  });
  const historyRead = useQuery({
    ...matrixHistoryQuery(versionId),
    enabled: versionId !== "",
  });
  const version = versionRead.data;
  const history = historyRead.data;
  const lastVariant = [...(history?.variants ?? [])].sort(
    (a, b) => b.number - a.number,
  )[0];
  const diffRead = useQuery({
    ...matrixDiffQuery(versionId, lastVariant?.number ?? 0),
    enabled: lastVariant?.kind === "Approver",
  });

  const showVersion = (shown: MatrixVersion) => {
    queryClient.setQueryData(matrixVersionQuery(shown.id).queryKey, shown);
    setSession((previous) => previous + 1);
  };
  const transit = useMutation({
    mutationFn: (action: Transit) =>
      "comment" in action
        ? returnMatrixVersion(action.version, action.comment)
        : transitMatrixVersion(action.version, action.transition),
    onSuccess: (saved) => {
      showVersion(saved);
      setDialog(null);
    },
    onSettled: () => invalidateMatrices(queryClient),
  });
  const startVersion = useMutation({
    mutationFn: () => createMatrixVersion(id),
    onSuccess: (created) => {
      setChosenId(created.id);
      showVersion(created);
    },
    onSettled: () => invalidateMatrices(queryClient),
  });

  const failure = matrixRead.error ?? versionsRead.error ?? versionRead.error;
  if (failure) {
    return (
      <ErrorAlert
        title="Не удалось загрузить матрицу"
        error={failure}
        onRetry={() => {
          void matrixRead.refetch();
          void versionsRead.refetch();
          void versionRead.refetch();
        }}
      />
    );
  }
  if (!matrix || !version) return <Skeleton active />;

  const isHead = !!roles.data?.headedUnits.some(
    (unit) => unit.id === matrix.orgUnitId,
  );
  const isNextVersionOffered =
    isHead && !!matrix.publishedVersion && !matrix.inWorkVersion;
  const isBusy = transit.isPending || startVersion.isPending;
  const isHeldBack =
    hasUnsaved && version.actions.some((action) => action !== "Edit");

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Link to="/career">Карьерная модель</Link>
      <Typography.Title level={1}>
        {`Матрица «${matrix.trackName}» — ${matrix.orgUnitName}`}
      </Typography.Title>
      <Descriptions
        items={[
          {
            key: "direction",
            label: "Направление",
            children: matrix.directionName,
          },
          {
            key: "scale",
            label: "Шкала",
            children: version.entries.scale.name,
          },
        ]}
      />
      <Flex gap="middle" align="center" wrap>
        <Select
          aria-label="Версия"
          value={version.id}
          options={[...(versionsRead.data ?? [])]
            .sort((a, b) => b.number - a.number)
            .map((item) => ({
              value: item.id,
              label: `Версия ${item.number}`,
            }))}
          onChange={setChosenId}
        />
        <Tag color={STATUS_COLORS[version.status]}>
          {VERSION_STATUS_LABELS[version.status]}
        </Tag>
        <MatrixActions
          matrix={matrix}
          version={version}
          hasHistory={!!history}
          isNextVersionOffered={isNextVersionOffered}
          isBusy={isBusy}
          hasUnsaved={hasUnsaved}
          onTransit={(transition) => transit.mutate({ version, transition })}
          onReturn={() => setDialog("return")}
          onNewVersion={() => startVersion.mutate()}
          onHistory={() => setDialog("history")}
          onDeleted={() => void navigate("/career")}
        />
      </Flex>
      {isHeldBack && (
        <Typography.Text type="secondary">
          Сначала сохраните изменения
        </Typography.Text>
      )}
      {transit.error && (
        <MatrixErrorAlert
          title="Не удалось выполнить действие"
          error={transit.error}
        />
      )}
      {startVersion.error && (
        <MatrixErrorAlert
          title="Не удалось выполнить действие"
          error={startVersion.error}
        />
      )}
      <RouteSteps version={version} decisions={history?.decisions ?? []} />
      <MatrixEditor
        key={`${version.id}:${version.status}:${session}`}
        version={version}
        diff={diffRead.data}
        onSaved={() => setSession((previous) => previous + 1)}
        onUnsavedChange={setHasUnsaved}
      />
      {dialog === "return" && (
        <ReturnModal
          isPending={transit.isPending}
          onReturn={(comment) => transit.mutate({ version, comment })}
          onClose={() => setDialog(null)}
        />
      )}
      {dialog === "history" && history && (
        <MatrixHistoryModal
          version={version}
          history={history}
          onClose={() => setDialog(null)}
        />
      )}
    </Space>
  );
}
