import { CheckCircleOutlined, WarningOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
  Alert,
  Button,
  Flex,
  Skeleton,
  Table,
  type TableColumnsType,
  Tag,
} from "antd";
import { Link, useLocation } from "react-router";
import { effectiveHr, unitHrQuery } from "../career/careerApi";
import { trackQuery } from "../career/catalogApi";
import type { MatrixProblem } from "../career/matrixApi";
import { deskPaths } from "./deskTexts";
import { type ReturnLink, returnState } from "./returnLink";
import { CompactAlert } from "./ScenarioContext";
import { PROBLEM_STEP, stepText } from "./scenarioTexts";

interface PositionGapActionProps {
  trackId: string;
  /** Name of the button that brings the user back from linking positions. */
  returnLabel: string;
  /** Why linking is not possible now; the link is replaced by this text. */
  unavailable?: string;
}

/** What can be done about a level without an active position: the link to put it right, or who can. */
export function PositionGapAction({
  trackId,
  returnLabel,
  unavailable,
}: PositionGapActionProps) {
  const location = useLocation();
  const track = useQuery(trackQuery(trackId));
  const ownerUnitId = track.data?.ownerOrgUnitId;
  const unitHr = useQuery({
    ...unitHrQuery(ownerUnitId ?? ""),
    enabled: ownerUnitId !== undefined,
  });
  const hr = unitHr.data && effectiveHr(unitHr.data);
  const back: ReturnLink = {
    to: location.pathname + location.search,
    label: returnLabel,
  };
  if (track.data?.canEdit !== true) {
    return hr ? `Обратитесь к ответственному HR: ${hr.name}` : null;
  }
  return unavailable ? (
    unavailable
  ) : (
    <Link to={deskPaths.trackPositionsOf(trackId)} state={returnState(back)}>
      Связать должности с треком
    </Link>
  );
}

/** The word that a level has no active position, as a tag. */
export function NoPositionTag() {
  return (
    <Tag icon={<WarningOutlined />} color="warning">
      Нет действующей должности
    </Tag>
  );
}

const sentence = (text: string) => (text.endsWith(".") ? text : `${text}.`);

interface CompletenessBlockProps {
  /** Null while the check is read. */
  problems: readonly MatrixProblem[] | null;
  trackId: string;
  /** Title of the warning over the problems, given their number. */
  warning: (count: number) => string;
  /** Name of the button that brings the user back from linking positions. */
  returnLabel: string;
  /** Takes the user to the wizard step where a problem is put right; without it the step is only named. */
  onStep?: (step: number) => void;
  /** One short explanation after the warning, such as what the check was made on. */
  note?: string;
}

/**
 * The check of a version: one line when it passes, a warning with the single problem inside it, or a warning over
 * a table of problems with where to put each right.
 */
export function CompletenessBlock({
  problems,
  trackId,
  warning,
  returnLabel,
  onStep,
  note,
}: CompletenessBlockProps) {
  if (!problems) return <Skeleton active />;
  if (problems.length === 0) {
    return (
      <CompactAlert
        type="success"
        icon={<CheckCircleOutlined />}
        lead="Проверка полноты:"
        text="версия полна, замечаний нет."
      />
    );
  }
  const what = (problem: MatrixProblem) =>
    problem.code === "LevelWithoutActivePosition" ? (
      <>
        <NoPositionTag /> {problem.message}
        {" — "}
        <PositionGapAction trackId={trackId} returnLabel={returnLabel} />
      </>
    ) : (
      problem.message
    );
  const where = (problem: MatrixProblem) => {
    const step = PROBLEM_STEP[problem.code];
    if (step === null) return null;
    return onStep ? (
      <Button type="link" onClick={() => onStep(step)}>
        {stepText(step)}
      </Button>
    ) : (
      stepText(step)
    );
  };
  const [only] = problems;
  if (problems.length === 1 && only) {
    return (
      <Alert
        type="warning"
        showIcon
        title={
          <>
            <strong>{sentence(warning(1))}</strong>
            {note && ` ${note}`}
            <div>
              {what(only)} {where(only)}
            </div>
          </>
        }
      />
    );
  }
  const columns: TableColumnsType<MatrixProblem> = [
    { title: "Что исправить", key: "message", render: (_, p) => what(p) },
    { title: "Где", key: "where", render: (_, p) => where(p) },
  ];
  return (
    <Flex vertical gap="middle">
      <CompactAlert
        type="warning"
        lead={sentence(warning(problems.length))}
        text={note}
      />
      <Table<MatrixProblem>
        size="small"
        rowKey={(problem) =>
          `${problem.code}:${problem.levelId}:${problem.competencyId}:${problem.message}`
        }
        columns={columns}
        dataSource={[...problems]}
        pagination={false}
        scroll={{ x: "max-content" }}
      />
    </Flex>
  );
}
