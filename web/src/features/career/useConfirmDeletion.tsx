import { useQueryClient } from "@tanstack/react-query";
import { App } from "antd";
import type { ReactNode } from "react";
import { ApiError } from "../../api/ApiError";
import { describeApiError } from "../../app/apiErrors";
import { destructiveConfirm } from "../../app/destructiveConfirm";
import { invalidateCatalogs } from "./catalogApi";
import { invalidateMatrices } from "./matrixApi";

interface Deletion {
  /** The kind of record in the accusative: «направление», «матрицу». */
  kind: string;
  name: string;
  /** Extra line under the question. */
  content?: ReactNode;
  run: () => Promise<unknown>;
  onDeleted?: () => void;
}

type Blocker = ApiError["blockers"][number];

const KIND_LABELS: Record<Blocker["kind"], string> = {
  Track: "Трек",
  Competency: "Компетенция",
  CompetencyVersion: "Версия компетенции",
  Matrix: "Матрица",
};

const PLURAL = new Intl.PluralRules("ru-RU");

const MORE_BLOCKERS: Record<string, string> = {
  one: "другая связанная запись",
  few: "других связанных записи",
  many: "других связанных записей",
};

/** «и 1 другая связанная запись» / «и 3 других связанных записи» / «и 5 других связанных записей», by the Russian plural rules: 21 is «другая». */
export function moreBlockersText(count: number): string {
  return `и ${count} ${MORE_BLOCKERS[PLURAL.select(count)] ?? MORE_BLOCKERS.many}`;
}

function blockerText({ kind, name, context }: Blocker): string {
  const label = `${KIND_LABELS[kind]} «${name}»`;
  if (!context) return label;
  if (kind === "Matrix") return `${label} — ${context}`;
  return `${label} (${context})`;
}

function Refusal({ error }: { error: ApiError }) {
  return (
    <>
      {error.blockers.length > 0 && (
        <ul>
          {error.blockers.map((blocker) => (
            <li key={blocker.id}>{blockerText(blocker)}</li>
          ))}
        </ul>
      )}
      {error.moreCount > 0 && <p>{moreBlockersText(error.moreCount)}</p>}
    </>
  );
}

/**
 * Asks to confirm a deletion, runs it and rereads the lists. A refusal (409) shows the server's own explanation;
 * the lists are reread after a failure too, since the refusal may mean they are stale.
 */
export function useConfirmDeletion() {
  const { modal, message } = App.useApp();
  const queryClient = useQueryClient();
  const reread = () =>
    Promise.all([
      invalidateCatalogs(queryClient),
      invalidateMatrices(queryClient),
    ]);
  return ({ kind, name, content, run, onDeleted }: Deletion) =>
    modal.confirm({
      title: `Удалить ${kind} «${name}»?`,
      content,
      okText: "Удалить",
      ...destructiveConfirm(true),
      cancelText: "Отмена",
      onOk: () =>
        run().then(
          () => {
            onDeleted?.();
            return reread();
          },
          (failure) => {
            if (
              failure instanceof ApiError &&
              failure.status === 409 &&
              failure.detail &&
              (failure.blockers.length > 0 || failure.moreCount > 0)
            ) {
              modal.error({
                title: failure.detail,
                content: <Refusal error={failure} />,
              });
              void reread();
              return;
            }
            message.error(
              failure instanceof ApiError &&
                failure.status === 409 &&
                failure.detail
                ? failure.detail
                : describeApiError(failure),
            );
            void reread();
          },
        ),
    });
}
