import { Typography } from "antd";
import { AppModal } from "../../app/AppModal";
import type { MatrixHistory as History, MatrixDecision } from "./matrixApi";
import { DECISION_TEXTS } from "./matrixLabels";

const DATE_TIME = new Intl.DateTimeFormat("ru-RU", {
  dateStyle: "short",
  timeStyle: "short",
});

const VARIANT_KINDS: Record<History["variants"][number]["kind"], string> = {
  Author: "Автор",
  Approver: "Согласующий",
};

function decisionLine(decision: MatrixDecision) {
  const who = decision.actorName ? `, ${decision.actorName}` : "";
  return `${DATE_TIME.format(new Date(decision.at))}${who}: ${DECISION_TEXTS[decision.kind]}`;
}

interface MatrixHistoryModalProps {
  history: History;
  onClose: () => void;
}

/** The decisions taken on a version in order, and the variants of its content they refer to. */
export function MatrixHistoryModal({
  history,
  onClose,
}: MatrixHistoryModalProps) {
  return (
    <AppModal open title="История" onCancel={onClose} footer={null}>
      <ul>
        {history.decisions.map((decision) => (
          <li key={decision.sequence}>
            {decisionLine(decision)}
            {decision.comment && (
              <Typography.Paragraph type="secondary">
                {decision.comment}
              </Typography.Paragraph>
            )}
          </li>
        ))}
      </ul>
      <Typography.Title level={3}>Варианты</Typography.Title>
      <ul>
        {history.variants.map((variant) => (
          <li key={variant.number}>
            {DATE_TIME.format(new Date(variant.createdAt))}
            {variant.createdByName ? `, ${variant.createdByName}` : ""}:{" "}
            {VARIANT_KINDS[variant.kind]}
          </li>
        ))}
      </ul>
    </AppModal>
  );
}
