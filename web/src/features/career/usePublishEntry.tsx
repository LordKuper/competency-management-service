import { useQueryClient } from "@tanstack/react-query";
import { Alert, App, Space } from "antd";
import { useState } from "react";
import { Link } from "react-router";
import { ApiError } from "../../api/ApiError";
import { describeApiError } from "../../app/apiErrors";
import {
  type CatalogSegment,
  type CatalogStatus,
  changeCatalogStatus,
  invalidateCatalogs,
  type PublicationBlocker,
  type PublicationEntry,
  type PublicationPreview,
  type PublishableSegment,
  publicationPreview,
} from "./catalogApi";
import { invalidateMatrices } from "./matrixApi";
import { moreBlockersText } from "./useConfirmDeletion";

/** What each catalogue's record is called in a question about it, in the accusative. */
export const KIND_ACCUSATIVE: Record<CatalogSegment, string> = {
  directions: "направление",
  tracks: "трек",
  levels: "уровень",
  categories: "категорию",
  competencies: "компетенцию",
  scales: "шкалу",
};

const CREATED_AS_DRAFT: Record<PublishableSegment, string> = {
  directions: "Направление",
  tracks: "Трек",
  categories: "Категория",
  competencies: "Компетенция",
  scales: "Шкала",
};

const CREATED_ENDING: Record<PublishableSegment, string> = {
  directions: "создано",
  tracks: "создан",
  categories: "создана",
  competencies: "создана",
  scales: "создана",
};

const KIND_LABELS: Record<PublicationEntry["kind"], string> = {
  Direction: "Направление",
  Track: "Трек",
  Level: "Уровень",
  Category: "Категория",
  Competency: "Компетенция",
  Scale: "Шкала",
};

const REASON_TEXTS: Record<PublicationBlocker["reason"], string> = {
  NoRight: "нет права утвердить",
  NoLevels: "у трека нет уровней",
  NoActivePosition: "нет действующей должности",
  NameTaken: "такое название уже занято",
};

const PLURAL = new Intl.PluralRules("ru-RU");

/** «1 черновая запись» / «2 черновые записи» / «5 черновых записей». */
function draftRecords(count: number): string {
  const forms: Record<string, string> = {
    one: "черновая запись",
    few: "черновые записи",
  };
  return `${count} ${forms[PLURAL.select(count)] ?? "черновых записей"}`;
}

/** «1 записью» / «2 записями»: the tail of «Утвердить вместе с …». */
function recordsInstrumental(count: number): string {
  return `${count} ${PLURAL.select(count) === "one" ? "записью" : "записями"}`;
}

/** What is published together with an entry, and why it may not be. */
export interface PublishTarget {
  segment: PublishableSegment;
  entry: { id: string; version: number; name: string };
}

type PublishAction = "approve" | "activate";

/** Why the list is shown again: it was there and changed, or the action looked harmless and was not. */
type ChangedNotice = "changed" | "appeared";

const NOTICE_TEXTS: Record<ChangedNotice, string> = {
  changed:
    "Пока вы подтверждали, состав публикуемого изменился. Ничего не опубликовано. Проверьте актуальный перечень.",
  appeared:
    "Пока вы подтверждали, у записи появились черновые связанные записи. Ничего не опубликовано. Проверьте актуальный перечень.",
};

function EntryList({ entries }: { entries: readonly PublicationEntry[] }) {
  return (
    <ul>
      {entries.map((item) => (
        <li key={`${item.kind}:${item.id}`}>
          {`${KIND_LABELS[item.kind]} ${item.name}`}
          {item.context ? ` (${item.context})` : ""}
        </li>
      ))}
    </ul>
  );
}

interface ConfirmContentProps {
  action: PublishAction;
  preview: PublicationPreview;
  notice?: ChangedNotice;
  /** The text of an offer made right after the entry was created as a draft; omitted when the entry is asked about. */
  isOffer?: boolean;
}

function ConfirmContent({
  action,
  preview,
  notice,
  isOffer,
}: ConfirmContentProps) {
  const { entries } = preview;
  const lead = isOffer
    ? "Запись ссылается на черновые записи, поэтому сразу действующей не стала. "
    : "";
  return (
    <Space orientation="vertical" size="small" style={{ display: "flex" }}>
      {notice && (
        <Alert
          type="warning"
          showIcon
          title="Перечень изменился — подтвердите заново"
          description={NOTICE_TEXTS[notice]}
        />
      )}
      {entries.length === 0 ? (
        <p>
          {action === "approve"
            ? "Запись станет действующей."
            : "Запись вернётся в работу."}
        </p>
      ) : (
        <>
          <p>
            {isOffer
              ? `${lead}Утвердить её можно сейчас: вместе с ней станут действующими ${draftRecords(entries.length)}:`
              : `${action === "approve" ? "Запись станет действующей" : "Запись вернётся в работу"}. Вместе с ней станут действующими ${draftRecords(entries.length)}:`}
          </p>
          <EntryList entries={entries} />
          <p>Всё публикуется сразу или не публикуется ничего.</p>
          {isOffer && (
            <p>
              Если не утверждать сейчас, запись останется в очереди «Утвердить
              черновые записи».
            </p>
          )}
        </>
      )}
    </Space>
  );
}

function blockerText({ kind, name, reason, context }: PublicationBlocker) {
  return `${KIND_LABELS[kind]} «${name}»: ${REASON_TEXTS[reason]}${context ? ` (${context})` : ""}`;
}

interface RefusalContentProps {
  target: PublishTarget;
  blockers: readonly PublicationBlocker[];
  moreCount: number;
  onNavigate: () => void;
}

/** The refusal in its parts: what blocks, then what to do next. */
function RefusalContent({
  target,
  blockers,
  moreCount,
  onNavigate,
}: RefusalContentProps) {
  const reasons = new Set(blockers.map((blocker) => blocker.reason));
  const trackId =
    blockers.find((blocker) => blocker.kind === "Track")?.id ??
    (target.segment === "tracks" ? target.entry.id : undefined);
  return (
    <>
      {blockers.length > 0 && (
        <ul>
          {blockers.map((blocker) => (
            <li key={`${blocker.kind}:${blocker.id}:${blocker.reason}`}>
              {blockerText(blocker)}
            </li>
          ))}
        </ul>
      )}
      {moreCount > 0 && <p>{moreBlockersText(moreCount)}</p>}
      {reasons.has("NoActivePosition") && (
        <p>
          Что дальше: свяжите должности с уровнями
          {trackId && (
            <>
              {" ("}
              <Link
                to={`/desk/track-positions/${trackId}`}
                onClick={onNavigate}
              >
                Связать должности с треком
              </Link>
              {")"}
            </>
          )}{" "}
          и повторите.
        </p>
      )}
      {reasons.has("NoLevels") && (
        <p>Что дальше: добавьте треку уровни и повторите.</p>
      )}
      {reasons.has("NameTaken") && (
        <p>Что дальше: измените название записи и повторите.</p>
      )}
      {reasons.has("NoRight") && (
        <p>
          Что дальше: такие записи утверждает ответственный HR подразделения.
        </p>
      )}
    </>
  );
}

function publicationBlockers(error: ApiError) {
  return error.blockers as unknown as readonly PublicationBlocker[];
}

/**
 * Approving and returning to work an entry of the catalogues, and the offer made right after an entry was created as a
 * draft. The list of what is published with the entry is read first; an empty one needs no question, otherwise a window
 * names the entries. The action carries the tag of the list the user saw. A refusal (409) is explained with what blocks;
 * a 412 that names a fresh preview shows the new list for confirmation again, with no retry of its own, while a 412 on
 * the entry's version only rereads the tables.
 */
export function usePublishEntry() {
  const { modal, message } = App.useApp();
  const queryClient = useQueryClient();
  const [isBusy, setIsBusy] = useState(false);
  const reread = () =>
    Promise.all([
      invalidateCatalogs(queryClient),
      invalidateMatrices(queryClient),
    ]);

  function refuse(
    target: PublishTarget,
    title: string,
    blockers: readonly PublicationBlocker[],
    moreCount: number,
  ) {
    const instance = modal.error({
      title,
      content: (
        <RefusalContent
          target={target}
          blockers={blockers}
          moreCount={moreCount}
          onNavigate={() => instance.destroy()}
        />
      ),
      okText: "OK",
    });
  }

  function refusalTitle(target: PublishTarget, action: PublishAction) {
    const isTrack = target.segment === "tracks";
    const done = isTrack ? "действующим" : "действующей";
    return `${isTrack ? "Трек" : "Запись"} «${target.entry.name}» нельзя ${action === "approve" ? `сделать ${done}` : "вернуть в работу"}`;
  }

  /** Runs the action under the confirmed tag; true when the list changed meanwhile and must be confirmed again. */
  async function execute(
    target: PublishTarget,
    action: PublishAction,
    preview: PublicationPreview,
  ): Promise<ChangedNotice | undefined> {
    const count = preview.entries.length;
    try {
      await changeCatalogStatus(
        target.segment,
        target.entry,
        action,
        preview.cascadeTag,
      );
      if (count > 0) {
        message.success(
          `Запись «${target.entry.name}» ${action === "approve" ? "утверждена" : "возвращена в работу"}; вместе с ней опубликовано записей: ${count}`,
        );
      }
    } catch (failure) {
      if (failure instanceof ApiError && failure.publication) {
        return count > 0 ? "changed" : "appeared";
      }
      if (
        failure instanceof ApiError &&
        failure.status === 409 &&
        (failure.blockers.length > 0 || failure.moreCount > 0)
      ) {
        refuse(
          target,
          failure.detail ?? refusalTitle(target, action),
          publicationBlockers(failure),
          failure.moreCount,
        );
      } else {
        message.error(describeApiError(failure));
      }
    } finally {
      void reread();
    }
    return undefined;
  }

  function confirm(
    target: PublishTarget,
    action: PublishAction,
    preview: PublicationPreview,
    notice?: ChangedNotice,
    isOffer?: boolean,
  ) {
    const count = preview.entries.length;
    const verb = action === "approve" ? "Утвердить" : "Вернуть в работу";
    modal.confirm({
      title: isOffer
        ? `${CREATED_AS_DRAFT[target.segment]} «${target.entry.name}» ${CREATED_ENDING[target.segment]} черновиком`
        : `${verb} ${KIND_ACCUSATIVE[target.segment]} «${target.entry.name}»?`,
      content: (
        <ConfirmContent
          action={action}
          preview={preview}
          notice={notice}
          isOffer={isOffer}
        />
      ),
      okText:
        count > 0 && action === "approve"
          ? `Утвердить вместе с ${recordsInstrumental(count)}`
          : verb,
      cancelText: isOffer ? "Не сейчас" : "Отмена",
      onOk: async () => {
        const again = await execute(target, action, preview);
        if (again) void publish(target, action, again);
      },
    });
  }

  /** Approves or returns to work an entry, after the list of what is published with it was shown if there is one. */
  async function publish(
    target: PublishTarget,
    action: PublishAction,
    notice?: ChangedNotice,
  ) {
    setIsBusy(true);
    try {
      const preview = await publicationPreview(target.segment, target.entry.id);
      if (preview.blockers.length > 0 || preview.moreCount > 0) {
        refuse(
          target,
          refusalTitle(target, action),
          preview.blockers,
          preview.moreCount,
        );
        return;
      }
      if (preview.entries.length === 0 && !notice) {
        const again = await execute(target, action, preview);
        if (again) await publish(target, action, again);
        return;
      }
      confirm(target, action, preview, notice);
    } catch (failure) {
      message.error(describeApiError(failure));
      void reread();
    } finally {
      setIsBusy(false);
    }
  }

  /**
   * After an entry was created: one made a draft only for lack of the right to approve it, or not referring to drafts,
   * gets no offer; one the user may approve is offered the approval with the list of what goes with it.
   */
  async function offerApproval(target: PublishTarget, status: CatalogStatus) {
    if (status !== "Draft") return;
    try {
      const preview = await publicationPreview(target.segment, target.entry.id);
      confirm(target, "approve", preview, undefined, true);
    } catch (failure) {
      if (
        !(failure instanceof ApiError && [403, 404].includes(failure.status))
      ) {
        message.error(describeApiError(failure));
      }
    }
  }

  return { publish, offerApproval, isBusy };
}
