import { useQuery } from "@tanstack/react-query";
import { Button, Skeleton, Typography, theme } from "antd";
import { useState } from "react";
import { AppModal } from "../../app/AppModal";
import { ErrorAlert } from "../../app/ErrorAlert";
import { MatrixEditor } from "./MatrixEditor";
import {
  type MatrixHistory as History,
  type MatrixDecision,
  type MatrixVersion,
  matrixDiffQuery,
  matrixVariantQuery,
} from "./matrixApi";
import { DECISION_TEXTS } from "./matrixLabels";

const DATE_TIME = new Intl.DateTimeFormat("ru-RU", {
  dateStyle: "short",
  timeStyle: "short",
});

type Variant = History["variants"][number];

const VARIANT_KINDS: Record<Variant["kind"], string> = {
  Author: "Автор",
  Approver: "Согласующий",
};

function decisionLine(decision: MatrixDecision) {
  const who = decision.actorName ? `, ${decision.actorName}` : "";
  return `${DATE_TIME.format(new Date(decision.at))}${who}: ${DECISION_TEXTS[decision.kind]}`;
}

function variantLine(variant: Variant) {
  const who = variant.createdByName ? `, ${variant.createdByName}` : "";
  return `${DATE_TIME.format(new Date(variant.createdAt))}${who}: ${VARIANT_KINDS[variant.kind]}`;
}

interface VariantModalProps {
  version: MatrixVersion;
  variant: Variant;
  onClose: () => void;
}

/** The content of one variant, read-only, and for an approver's variant what it changed against the variant before. */
function VariantModal({ version, variant, onClose }: VariantModalProps) {
  const { token } = theme.useToken();
  const read = useQuery(matrixVariantQuery(version.id, variant.number));
  const diff = useQuery({
    ...matrixDiffQuery(version.id, variant.number),
    enabled: variant.kind === "Approver",
  });

  return (
    <AppModal
      open
      title={`Вариант ${variant.number}`}
      width={token.screenLG}
      onCancel={onClose}
      footer={null}
    >
      <Typography.Paragraph>{variantLine(variant)}</Typography.Paragraph>
      {read.error && (
        <ErrorAlert
          title="Не удалось загрузить вариант"
          error={read.error}
          onRetry={() => void read.refetch()}
        />
      )}
      {read.isPending && !read.error && <Skeleton active />}
      {read.data && (
        <MatrixEditor
          version={{ ...version, content: read.data.content, actions: [] }}
          diff={diff.data}
        />
      )}
    </AppModal>
  );
}

interface MatrixHistoryModalProps {
  /** The version whose history it is; a variant is shown with its entries. */
  version: MatrixVersion;
  history: History;
  onClose: () => void;
}

/** The decisions taken on a version in order, and the variants of its content they refer to, each of which opens read-only. */
export function MatrixHistoryModal({
  version,
  history,
  onClose,
}: MatrixHistoryModalProps) {
  const [opened, setOpened] = useState<Variant>();
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
            <Button type="link" onClick={() => setOpened(variant)}>
              {variantLine(variant)}
            </Button>
          </li>
        ))}
      </ul>
      {opened && (
        <VariantModal
          key={opened.number}
          version={version}
          variant={opened}
          onClose={() => setOpened(undefined)}
        />
      )}
    </AppModal>
  );
}
