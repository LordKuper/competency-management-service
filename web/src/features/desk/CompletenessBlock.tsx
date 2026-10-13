import { CheckCircleOutlined, WarningOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
  Alert,
  Button,
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
import { PROBLEM_STEP, remarksText, stepText } from "./scenarioTexts";

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
}

/** The check of a version: the problems with where to put each right, or the word that there are none. */
export function CompletenessBlock({
  problems,
  trackId,
  warning,
  returnLabel,
  onStep,
}: CompletenessBlockProps) {
  if (!problems) return <Skeleton active />;
  if (problems.length === 0) {
    return (
      <Alert
        type="success"
        showIcon
        icon={<CheckCircleOutlined />}
        title="Версия полна"
        description="Замечаний нет."
      />
    );
  }
  const columns: TableColumnsType<MatrixProblem> = [
    {
      title: "Что исправить",
      key: "message",
      render: (_, problem) =>
        problem.code === "LevelWithoutActivePosition" ? (
          <>
            <NoPositionTag /> {problem.message}
            {" — "}
            <PositionGapAction trackId={trackId} returnLabel={returnLabel} />
          </>
        ) : (
          problem.message
        ),
    },
    {
      title: "Где",
      key: "where",
      render: (_, problem) => {
        const step = PROBLEM_STEP[problem.code];
        if (step === null) return null;
        return onStep ? (
          <Button type="link" onClick={() => onStep(step)}>
            {stepText(step)}
          </Button>
        ) : (
          stepText(step)
        );
      },
    },
  ];
  return (
    <>
      <Alert
        type="warning"
        showIcon
        title={warning(problems.length)}
        description={`Исправьте ${remarksText(problems.length)} и вернитесь к проверке. Проверка идёт по сохранённому черновику.`}
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
    </>
  );
}
