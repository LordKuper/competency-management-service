import { useQueryClient } from "@tanstack/react-query";
import { App, Space } from "antd";
import { type ReactNode, useState } from "react";
import { ApiError } from "../../api/ApiError";
import { deskQueryKey } from "../../api/queryClient";
import { destructiveConfirm } from "../../app/destructiveConfirm";
import {
  invalidateMatrices,
  type MatrixAction,
  type MatrixPublication,
  type MatrixTransition,
  type MatrixVersion,
  readMatrixPublication,
  transitMatrixVersion,
} from "../career/matrixApi";
import {
  type ChangedNotice,
  ChangedNoticeAlert,
  publishedCount,
} from "../career/usePublishEntry";
import { CascadeTable } from "./CascadeTable";

const PUBLISHING_ACTION: Partial<Record<MatrixTransition, MatrixAction>> = {
  submit: "Submit",
  approve: "Approve",
  "accept-changes": "AcceptChanges",
};

const DEFAULT_QUESTION: PublishingQuestion = {
  title: "Подтвердите публикацию",
  okText: "Подтвердить",
  lead: (count) => `Вместе с версией станут действующими записи: ${count}.`,
  isAskedFirst: true,
};

/** A question that confirms a decision before it is taken. */
interface Question {
  title: string;
  okText: string;
}

/** The question about a decision that publishes nothing. */
export interface PlainQuestion extends Question {
  /** What the decision does. */
  lead: ReactNode;
  isDanger?: boolean;
}

/** The question about a decision that publishes records with the version. */
export interface PublishingQuestion extends Question {
  /** What the decision does, given the number of records it publishes. */
  lead: (count: number) => ReactNode;
  /** Off where the screen has shown the list already and the user decides under it. */
  isAskedFirst: boolean;
}

/** A decision on a version: the transition, and how the user is asked about it. */
export interface Decision {
  version: MatrixVersion;
  transition: MatrixTransition;
  /** What the actions of the version publish, as the screen shows it; the decision is confirmed under its tag. */
  preview?: MatrixPublication;
  /** Asked first when the decision publishes nothing; without it such a decision is taken at once. */
  plain?: PlainQuestion;
  /** Asked when the decision publishes, with the list. */
  publishing?: PublishingQuestion;
  /** Called with the saved version and the number of records published with it. */
  onDone: (saved: MatrixVersion, publishedCount: number) => void;
}

interface PublishingContentProps {
  question: PublishingQuestion;
  preview: MatrixPublication;
  notice?: ChangedNotice;
}

function PublishingContent({
  question,
  preview,
  notice,
}: PublishingContentProps) {
  const count = publishedCount(preview);
  return (
    <Space orientation="vertical" size="small" style={{ display: "flex" }}>
      {notice && <ChangedNoticeAlert notice={notice} />}
      <div>{question.lead(count)}</div>
      {count > 0 && (
        <>
          <CascadeTable drafts={preview} />
          <p>Всё публикуется сразу или не публикуется ничего.</p>
        </>
      )}
    </Space>
  );
}

/**
 * Takes the decisions on a version that move it along its route. One that publishes is taken under the tag of the list
 * of what it publishes, which the user has seen; if the list changed meanwhile, or the decision turned out to publish,
 * the server refuses it, and the fresh list is shown for confirmation again with nothing done and no retry of its own.
 * A refusal or a stale version is kept in `error` for the screen to show.
 */
export function useVersionTransition() {
  const { modal } = App.useApp();
  const queryClient = useQueryClient();
  const [isPending, setIsPending] = useState(false);
  const [error, setError] = useState<unknown>(null);

  async function run(
    decision: Decision,
    confirmed?: { tag: string; count: number },
  ) {
    setIsPending(true);
    setError(null);
    try {
      const saved = await transitMatrixVersion(
        decision.version,
        decision.transition,
        confirmed?.tag,
      );
      decision.onDone(saved, confirmed?.count ?? 0);
    } catch (failure) {
      if (failure instanceof ApiError && failure.publication) {
        await askAgain(decision, confirmed !== undefined);
      } else {
        setError(failure);
      }
    } finally {
      setIsPending(false);
      void invalidateMatrices(queryClient);
      void queryClient.invalidateQueries({ queryKey: deskQueryKey });
    }
  }

  async function askAgain(decision: Decision, wasPublishing: boolean) {
    try {
      const fresh = await readMatrixPublication(decision.version.id);
      confirmPublishing(
        decision,
        fresh,
        wasPublishing ? "changed" : "published",
      );
    } catch (failure) {
      setError(failure);
    }
  }

  function confirmPublishing(
    decision: Decision,
    preview: MatrixPublication,
    notice?: ChangedNotice,
  ) {
    const question = decision.publishing ?? DEFAULT_QUESTION;
    modal.confirm({
      title: question.title,
      content: (
        <PublishingContent
          question={question}
          preview={preview}
          notice={notice}
        />
      ),
      okText: question.okText,
      cancelText: "Отмена",
      onOk: () =>
        run(decision, {
          tag: preview.cascadeTag,
          count: publishedCount(preview),
        }),
    });
  }

  /** Takes the decision, asking first where the screen has not shown what it does. */
  async function decide(decision: Decision) {
    setError(null);
    const { preview, publishing, plain } = decision;
    const action = PUBLISHING_ACTION[decision.transition];
    const isPublishing =
      preview !== undefined &&
      action !== undefined &&
      preview.publishingActions.includes(action);
    if (isPublishing) {
      if (publishing?.isAskedFirst) {
        confirmPublishing(decision, preview);
        return;
      }
      await run(decision, {
        tag: preview.cascadeTag,
        count: publishedCount(preview),
      });
      return;
    }
    if (plain) {
      modal.confirm({
        title: plain.title,
        content: plain.lead,
        okText: plain.okText,
        cancelText: "Отмена",
        ...destructiveConfirm(plain.isDanger === true),
        onOk: () => run(decision),
      });
      return;
    }
    await run(decision);
  }

  return { decide, isPending, error };
}
