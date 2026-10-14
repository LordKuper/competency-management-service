import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, App, Button, Flex, Skeleton, Tag, Typography } from "antd";
import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { MatrixEditor } from "../career/MatrixEditor";
import { ReturnModal } from "../career/MatrixPage";
import {
  invalidateMatrices,
  type MatrixVersion,
  matrixChangesQuery,
  matrixCompletenessQuery,
  matrixDiffQuery,
  matrixHistoryQuery,
  matrixPublicationQuery,
  matrixQuery,
  matrixVersionQuery,
  returnMatrixVersion,
} from "../career/matrixApi";
import { VERSION_STATUS_LABELS } from "../career/matrixLabels";
import { draftRecords, RefusalContent } from "../career/usePublishEntry";
import { CascadeTable } from "./CascadeTable";
import { ChangesBlock } from "./ChangesBlock";
import { CompletenessBlock } from "./CompletenessBlock";
import { DecisionError } from "./DecisionError";
import { deskPaths, formatDate, relativeTime, shortName } from "./deskTexts";
import { LinkButton } from "./LinkButton";
import { ActionBar, ContextLine, OtherVersionMenu } from "./ScenarioContext";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";
import { outcomeText, remarksText, WAITING_STEP_TEXTS } from "./scenarioTexts";
import { useVersionTransition } from "./useVersionTransition";
import { useVersionNames } from "./versionNames";

/** «Согласовать версию»: the decision on a version that waits for the user, with what it changes and publishes. */
export function ReviewPage() {
  const { versionId = "" } = useParams();
  const read = useQuery(matrixVersionQuery(versionId));
  return (
    <ScenarioPage title="Согласовать версию">
      {read.error ? (
        <LoadFailure
          title="Не удалось загрузить версию"
          error={read.error}
          onRetry={() => void read.refetch()}
        />
      ) : read.data ? (
        <ReviewContent version={read.data} />
      ) : (
        <Skeleton active />
      )}
    </ScenarioPage>
  );
}

function ReviewContent({ version }: { version: MatrixVersion }) {
  const { message } = App.useApp();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [isReturning, setIsReturning] = useState(false);
  const matrix = useQuery(matrixQuery(version.matrixId));
  const completeness = useQuery(matrixCompletenessQuery(version.id));
  const publication = useQuery(matrixPublicationQuery(version.id));
  const changes = useQuery(matrixChangesQuery(version.id));
  const history = useQuery(matrixHistoryQuery(version.id));
  const lastVariant = [...(history.data?.variants ?? [])].sort(
    (a, b) => b.number - a.number,
  )[0];
  const approverDiff = useQuery({
    ...matrixDiffQuery(version.id, lastVariant?.number ?? 0),
    enabled: lastVariant?.kind === "Approver",
  });
  const { levelNames, competencyNames } = useVersionNames(
    version,
    changes.data?.before.competencies.map((item) => item.competencyId),
  );
  const transition = useVersionTransition();
  const sendBack = useMutation({
    mutationFn: (comment: string) => returnMatrixVersion(version, comment),
    onSuccess: () => {
      message.success("Версия возвращена автору");
      void navigate(deskPaths.desk);
    },
    onSettled: () => invalidateMatrices(queryClient),
  });

  const can = (action: MatrixVersion["actions"][number]) =>
    version.actions.includes(action);
  const isWaitingForUser = can("Approve") || can("Return");
  const problems = completeness.data?.problems ?? null;
  const isPublishing = publication.data?.publishingActions.includes("Approve");
  const entries = publication.data?.entries ?? [];
  const approverChanges =
    lastVariant?.kind === "Approver" ? approverDiff.data : undefined;
  const isFirstVersion = changes.data?.previousNumber === null;
  const submittedAt = history.data?.decisions
    .filter((decision) => decision.kind === "Submitted")
    .at(-1)?.at;
  const hasProblems = problems !== null && problems.length > 0;
  const approveBlock = !can("Approve")
    ? "Вы внесли правки: версию можно только вернуть"
    : hasProblems
      ? `Согласовать недоступно: ${remarksText(problems.length)} полноты`
      : null;
  const unitName = matrix.data?.orgUnitName ?? "";
  const trackName =
    version.entries.track?.name ?? version.entries.position?.name ?? "";

  function approve() {
    void transition.decide({
      version,
      transition: "approve",
      preview: publication.data,
      publishing: {
        isAskedFirst: true,
        title: "Согласовать и опубликовать версию?",
        okText: "Согласовать и опубликовать",
        lead: (count) =>
          `Версия ${version.number} станет действующей матрицей подразделения «${unitName}». ${
            count > 0
              ? `Вместе с ней станут действующими ${draftRecords(count)}:`
              : "Черновых записей нет: публикуется только версия."
          }`,
      },
      onDone: (saved, count) => {
        message.success(outcomeText(saved, count, "Версия согласована"));
        void navigate(deskPaths.desk);
      },
    });
  }

  return (
    <Flex vertical gap="middle">
      <ContextLine>
        <Typography.Text strong>
          {`Матрица «${trackName}» — ${unitName}`}
        </Typography.Text>
        <Tag>{`Версия ${version.number}`}</Tag>
        <Tag color={isWaitingForUser ? "processing" : undefined}>
          {isWaitingForUser && version.step
            ? `Ждёт вас: ${WAITING_STEP_TEXTS[version.step]}`
            : VERSION_STATUS_LABELS[version.status]}
        </Tag>
        <Typography.Text type="secondary">
          {`Автор: ${shortName(version.authorName ?? "")}`}
          {submittedAt &&
            ` · отправлена ${relativeTime(submittedAt)}, ${formatDate(submittedAt)}`}
        </Typography.Text>
        <OtherVersionMenu
          group="VersionsToApprove"
          currentId={version.id}
          pathOf={deskPaths.review}
          isLocked={transition.isPending || isReturning}
        />
      </ContextLine>
      {!isWaitingForUser && (
        <Alert
          type="info"
          showIcon
          title={
            <>
              <strong>Версия уже не ждёт вашего решения.</strong>
              {` Сейчас она «${VERSION_STATUS_LABELS[version.status]}»: содержимое доступно для просмотра, решений нет. `}
              <Link to={deskPaths.desk}>К рабочему столу</Link>
            </>
          }
        />
      )}
      <section>
        <Typography.Title level={4}>
          {isFirstVersion || !matrix.data?.publishedVersion
            ? "Версия целиком"
            : `Что изменилось относительно опубликованной версии ${matrix.data.publishedVersion.number}`}
        </Typography.Title>
        {changes.error ? (
          <LoadFailure
            title="Не удалось загрузить изменения"
            error={changes.error}
            onRetry={() => void changes.refetch()}
          />
        ) : !changes.data ? (
          <Skeleton active />
        ) : isFirstVersion ? (
          <Flex vertical gap="middle">
            <Alert
              type="info"
              showIcon
              title="Это первая версия матрицы подразделения: показана вся версия, а не разница."
            />
            <MatrixEditor
              isCompact
              version={{
                ...version,
                actions: version.actions.filter((action) => action !== "Edit"),
              }}
            />
          </Flex>
        ) : (
          <ChangesBlock
            diff={changes.data}
            levelNames={levelNames}
            competencyNames={competencyNames}
            scalePoints={version.entries.scale.points}
            columns={{ before: "Действует", after: "В этой версии" }}
            emptyText="Отличий от опубликованной версии нет."
          />
        )}
      </section>
      {approverChanges && approverChanges.changes.length > 0 && (
        <section>
          <Typography.Title level={4}>Изменено вами</Typography.Title>
          <ChangesBlock
            diff={approverChanges}
            levelNames={levelNames}
            competencyNames={competencyNames}
            scalePoints={version.entries.scale.points}
            columns={{ before: "Было", after: "Стало" }}
            emptyText="Правок нет."
          />
        </section>
      )}
      <CompletenessBlock
        problems={problems}
        trackId={version.entries.track?.id ?? ""}
        warning={(count) =>
          isPublishing
            ? `Проверка полноты: опубликовать версию нельзя, ${remarksText(count)}`
            : `Проверка полноты: согласовать версию нельзя, ${remarksText(count)}`
        }
        originName="Согласовать версию"
      />
      {isPublishing && publication.data && (
        <section>
          <Typography.Title level={4}>
            Будет опубликовано вместе с версией
          </Typography.Title>
          <Flex vertical gap="small">
            {entries.length > 0 ? (
              <>
                <Typography.Text>
                  Эти черновые записи станут действующими одновременно с
                  версией. Право утвердить их проверяется у ответственного HR
                  подразделения матрицы; отказать по одной нельзя.
                </Typography.Text>
                <CascadeTable entries={entries} />
              </>
            ) : (
              <Typography.Text>
                Черновых записей нет: публикуется только версия.
              </Typography.Text>
            )}
            {(publication.data.blockers.length > 0 ||
              publication.data.moreCount > 0) && (
              <Alert
                type="warning"
                showIcon
                title="Часть записей утвердить нельзя"
                description={
                  <RefusalContent
                    blockers={publication.data.blockers}
                    moreCount={publication.data.moreCount}
                  />
                }
              />
            )}
          </Flex>
        </section>
      )}
      {transition.error != null && (
        <DecisionError
          title="Версию не удалось согласовать"
          error={transition.error}
        />
      )}
      {isWaitingForUser && (
        <ActionBar
          start={
            <>
              {can("Edit") && (
                <LinkButton to={`/career/matrices/${version.matrixId}`}>
                  Внести правки
                </LinkButton>
              )}
              {can("Return") && (
                <Button
                  disabled={transition.isPending}
                  onClick={() => setIsReturning(true)}
                >
                  Вернуть с правками
                </Button>
              )}
            </>
          }
        >
          {approveBlock && (
            <Typography.Text type="secondary">{approveBlock}</Typography.Text>
          )}
          <Button
            type="primary"
            loading={transition.isPending}
            disabled={approveBlock !== null || !publication.data}
            onClick={approve}
          >
            {isPublishing ? "Согласовать и опубликовать" : "Согласовать"}
          </Button>
        </ActionBar>
      )}
      {isReturning && (
        <ReturnModal
          isPending={sendBack.isPending}
          error={sendBack.error}
          onReturn={(comment) => sendBack.mutate(comment)}
          onClose={() => {
            sendBack.reset();
            setIsReturning(false);
          }}
        />
      )}
    </Flex>
  );
}
