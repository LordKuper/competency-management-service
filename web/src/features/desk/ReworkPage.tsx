import { useQuery } from "@tanstack/react-query";
import {
  Alert,
  App,
  theme as antdTheme,
  Button,
  Card,
  Flex,
  Skeleton,
  Tag,
  Typography,
} from "antd";
import { Link, useNavigate, useParams } from "react-router";
import {
  type MatrixTransition,
  type MatrixVersion,
  matrixCompletenessQuery,
  matrixDiffQuery,
  matrixHistoryQuery,
  matrixPublicationQuery,
  matrixQuery,
  matrixVersionQuery,
} from "../career/matrixApi";
import { scaleOfCompetency } from "../career/matrixGroups";
import {
  APPROVAL_STEP_LABELS,
  matrixName,
  VERSION_STATUS_LABELS,
} from "../career/matrixLabels";
import { useBlockNames } from "../career/useBlockNames";
import { draftRecords } from "../career/usePublishEntry";
import { ChangesBlock } from "./ChangesBlock";
import { CompletenessBlock } from "./CompletenessBlock";
import { DecisionError } from "./DecisionError";
import { deskPaths, formatDate, relativeTime, shortName } from "./deskTexts";
import { ContextLine, OtherVersionMenu } from "./ScenarioContext";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";
import { outcomeText, remarksText } from "./scenarioTexts";
import { useVersionTransition } from "./useVersionTransition";
import { useVersionNames } from "./versionNames";

/** «Доработать возвращённую версию»: the approver's comment and changes, and the author's three ways on. */
export function ReworkPage() {
  const { versionId = "" } = useParams();
  const read = useQuery(matrixVersionQuery(versionId));
  return (
    <ScenarioPage title="Доработать возвращённую версию">
      {read.error ? (
        <LoadFailure
          title="Не удалось загрузить возвращённую версию"
          error={read.error}
          onRetry={() => void read.refetch()}
        />
      ) : read.data ? (
        <ReworkContent version={read.data} />
      ) : (
        <Skeleton active />
      )}
    </ScenarioPage>
  );
}

interface DecisionCardProps {
  title: string;
  consequence: string;
  button: string;
  isDanger?: boolean;
  /** Why the decision cannot be taken now; the button is off while this is given. */
  unavailable?: string;
  isPending: boolean;
  onClick: () => void;
}

function DecisionCard({
  title,
  consequence,
  button,
  isDanger,
  unavailable,
  isPending,
  onClick,
}: DecisionCardProps) {
  return (
    <Card size="small" style={{ flex: 1, minWidth: 0 }}>
      <Flex vertical gap="small" align="flex-start">
        <Typography.Title level={4} style={{ margin: 0 }}>
          {title}
        </Typography.Title>
        <Typography.Text>{consequence}</Typography.Text>
        <Button
          danger={isDanger}
          loading={isPending}
          disabled={unavailable !== undefined || undefined}
          onClick={onClick}
        >
          {button}
        </Button>
        {unavailable && (
          <Typography.Text type="secondary">{unavailable}</Typography.Text>
        )}
      </Flex>
    </Card>
  );
}

function ReworkContent({ version }: { version: MatrixVersion }) {
  const { token } = antdTheme.useToken();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const matrix = useQuery(matrixQuery(version.matrixId));
  const completeness = useQuery(matrixCompletenessQuery(version.id));
  const publication = useQuery(matrixPublicationQuery(version.id));
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
    approverDiff.data?.before.competencies.map((item) => item.competencyId),
  );
  const blockNames = useBlockNames(version, approverDiff.data);
  const transition = useVersionTransition();

  const can = (action: MatrixVersion["actions"][number]) =>
    version.actions.includes(action);
  const isWaitingForUser =
    can("AcceptChanges") || can("AcceptAndRevise") || can("RollbackChanges");
  const returned = history.data?.decisions
    .filter((decision) => decision.kind === "Returned")
    .at(-1);
  const problems = completeness.data?.problems ?? null;
  const changeCount = approverDiff.data?.changes.length ?? 0;
  const unitName = matrix.data?.orgUnitName ?? "";
  const stepName = returned?.step ? APPROVAL_STEP_LABELS[returned.step] : "";
  const actorName = shortName(returned?.actorName ?? "");
  const hrName = version.hr?.name ? shortName(version.hr.name) : "";
  const nextStep =
    returned?.step === "Superior"
      ? `шаг HR${hrName ? ` (${hrName})` : ""}. Ему придёт письмо`
      : "Публикация";

  const historyBlock = history.error
    ? "Недоступно: не загрузилась история версии."
    : undefined;
  const acceptBlock =
    history.error || completeness.error || publication.error
      ? "Недоступно: не загрузились история версии, проверка полноты или перечень публикации."
      : changeCount === 0
        ? "Недоступно: согласующий вернул версию только с комментарием, принимать нечего."
        : problems && problems.length > 0
          ? `Недоступно: ${remarksText(problems.length)} полноты. Устраните его (ссылка выше) или выберите другое решение.`
          : publication.isPending
            ? "Проверяем, что будет опубликовано…"
            : undefined;

  function decide(
    transitionName: MatrixTransition,
    after: (saved: MatrixVersion, count: number) => void,
    questions: Pick<
      Parameters<typeof transition.decide>[0],
      "plain" | "publishing"
    >,
  ) {
    void transition.decide({
      version,
      transition: transitionName,
      preview: publication.data,
      ...questions,
      onDone: after,
    });
  }

  function toBuilder(step: number) {
    return () => {
      void navigate(`${deskPaths.matrixBuilderOf(version.id)}?step=${step}`);
    };
  }

  function accept() {
    decide(
      "accept-changes",
      (saved, count) => {
        message.success(outcomeText(saved, count, "Правки приняты"));
        void navigate(deskPaths.desk);
      },
      {
        plain: {
          title: "Принять правки?",
          okText: "Принять правки",
          lead: `Шаг «${stepName}» (${actorName}) считается согласованным. Версия ${version.number} пойдёт дальше: ${nextStep}. Ничего не публикуется.`,
        },
        publishing: {
          isAskedFirst: true,
          title: "Принять правки?",
          okText: "Принять правки",
          lead: (count) =>
            `Шаг «${stepName}» (${actorName}) считается согласованным. Версия ${version.number} пойдёт дальше: ${nextStep}. ${
              count > 0
                ? `Вместе с ней станут действующими ${draftRecords(count)}:`
                : "Черновых записей нет: публикуется только версия."
            }`,
        },
      },
    );
  }

  return (
    <Flex vertical gap="middle">
      <ContextLine>
        <Typography.Text strong>
          {`${matrixName({ trackName: version.entries.track?.name, positionName: version.entries.position?.name })} — ${unitName}`}
        </Typography.Text>
        <Tag>{`Версия ${version.number}`}</Tag>
        <Tag color="warning">{VERSION_STATUS_LABELS[version.status]}</Tag>
        <OtherVersionMenu
          group="ReturnedVersions"
          currentId={version.id}
          pathOf={deskPaths.rework}
          isLocked={transition.isPending}
        />
      </ContextLine>
      {!isWaitingForUser && (
        <Alert
          type="info"
          showIcon
          title={
            <>
              <strong>Версия уже не ждёт вашего решения.</strong>
              {` Сейчас она «${VERSION_STATUS_LABELS[version.status]}». `}
              <Link to={deskPaths.desk}>К рабочему столу</Link>
            </>
          }
        />
      )}
      {history.error ? (
        <LoadFailure
          title="Не удалось загрузить комментарий и правки согласующего"
          error={history.error}
          onRetry={() => void history.refetch()}
        />
      ) : (
        <>
          <section data-tour="rework-comment">
            <Typography.Title level={4}>
              Комментарий согласующего
            </Typography.Title>
            {returned ? (
              <Flex vertical gap={token.marginXXS}>
                <Typography.Text type="secondary">
                  {[
                    shortName(returned.actorName ?? ""),
                    returned.step && `${APPROVAL_STEP_LABELS[returned.step]}`,
                  ]
                    .filter(Boolean)
                    .join(", ")}
                  {` · ${relativeTime(returned.at)}, ${formatDate(returned.at)}`}
                </Typography.Text>
                <Typography.Paragraph style={{ whiteSpace: "pre-wrap" }}>
                  {returned.comment || "Комментария нет."}
                </Typography.Paragraph>
              </Flex>
            ) : (
              <Skeleton active />
            )}
          </section>
          <section>
            <Typography.Title level={4}>
              Правки согласующего относительно вашего варианта
            </Typography.Title>
            {approverDiff.data ? (
              <>
                {changeCount === 0 && (
                  <Typography.Paragraph>
                    Правок нет: согласующий оставил только комментарий. Учтите
                    его в доработке или откатите версию к своему варианту.
                  </Typography.Paragraph>
                )}
                {changeCount > 0 && (
                  <ChangesBlock
                    diff={approverDiff.data}
                    levelNames={levelNames}
                    competencyNames={competencyNames}
                    blockNames={blockNames}
                    pointsOf={(id) => scaleOfCompetency(version, id).points}
                    columns={{
                      before: "Ваш вариант",
                      after: "Правка согласующего",
                    }}
                    emptyText="Правок нет."
                  />
                )}
              </>
            ) : approverDiff.error ? (
              <LoadFailure
                title="Не удалось загрузить правки"
                error={approverDiff.error}
                onRetry={() => void approverDiff.refetch()}
              />
            ) : lastVariant && lastVariant.kind !== "Approver" ? (
              <Typography.Paragraph>
                Правок нет: согласующий оставил только комментарий. Учтите его в
                доработке или откатите версию к своему варианту.
              </Typography.Paragraph>
            ) : (
              <Skeleton active />
            )}
          </section>
        </>
      )}
      {completeness.error ? (
        <LoadFailure
          title="Не удалось выполнить проверку полноты"
          error={completeness.error}
          onRetry={() => void completeness.refetch()}
        />
      ) : (
        <CompletenessBlock
          problems={problems}
          trackId={version.entries.track?.id ?? ""}
          warning={(count) =>
            `Проверка полноты с учётом правок: ${remarksText(count)}, повторно отправить версию нельзя.`
          }
          originName="Доработать возвращённую версию"
        />
      )}
      {publication.error && (
        <LoadFailure
          title="Не удалось загрузить перечень публикации"
          error={publication.error}
          onRetry={() => void publication.refetch()}
        />
      )}
      {transition.error != null && (
        <DecisionError
          title="Не удалось выполнить решение"
          error={transition.error}
        />
      )}
      {isWaitingForUser && (
        <section data-tour="rework-decision">
          <Typography.Title level={4}>Ваше решение</Typography.Title>
          <Flex gap={token.marginSM} wrap>
            {can("AcceptChanges") && (
              <DecisionCard
                title="Принять правки"
                consequence={`Шаг «${stepName}» считается согласованным, версия идёт дальше по маршруту: следующий шаг — «${returned?.step === "Superior" ? "HR" : "Публикация"}».`}
                button="Принять правки"
                unavailable={acceptBlock}
                isPending={transition.isPending}
                onClick={accept}
              />
            )}
            {can("AcceptAndRevise") && (
              <DecisionCard
                title="Принять и доработать"
                consequence="Версия становится черновиком с принятыми правками. После доработки она снова идёт на согласование с первого шага."
                button="Принять и доработать"
                unavailable={historyBlock}
                isPending={transition.isPending}
                onClick={() =>
                  decide("accept-and-revise", toBuilder(3), {
                    plain: {
                      title: "Принять и доработать?",
                      okText: "Принять и доработать",
                      lead: `Версия ${version.number} станет черновиком с принятыми правками. Откроется мастер на шаге «Требования»; после доработки версия снова пойдёт на согласование с первого шага.`,
                    },
                  })
                }
              />
            )}
            {can("RollbackChanges") && (
              <DecisionCard
                title="Откатить правки"
                consequence="Версия возвращается к вашему последнему варианту и становится черновиком. Правки согласующего пропадут. Затем версия снова идёт на согласование с первого шага."
                button="Откатить правки"
                unavailable={historyBlock}
                isDanger
                isPending={transition.isPending}
                onClick={() =>
                  decide("rollback-changes", toBuilder(5), {
                    plain: {
                      title: "Откатить правки согласующего?",
                      okText: "Откатить правки",
                      isDanger: true,
                      lead: `Правки ${actorName} будут отменены, вернуть их нельзя. Версия ${version.number} вернётся к вашему последнему варианту и станет черновиком; откроется мастер на шаге «Проверка полноты».`,
                    },
                  })
                }
              />
            )}
          </Flex>
        </section>
      )}
    </Flex>
  );
}
