import { useQuery } from "@tanstack/react-query";
import { BlockModal } from "../career/BlockModal";
import { CatalogModal } from "../career/CatalogModal";
import { CompetencyModal } from "../career/CompetencyModal";
import {
  blockQuery,
  type CatalogDraft,
  competencyQuery,
  entryQuery,
  SEGMENT_OF_KIND,
  scaleQuery,
  trackQuery,
} from "../career/catalogApi";
import { NameModal } from "../career/NameModal";
import { ScaleModal } from "../career/ScaleModal";
import { TrackModal } from "../career/TrackModal";

/** The entry as a save left it, which the draft is approved under. */
export interface SavedEntry {
  id: string;
  version: number;
  name: string;
}

interface EditDraftModalProps {
  draft: CatalogDraft;
  onSaved: (saved: SavedEntry) => void;
  onClose: () => void;
}

function ReadFailure({
  error,
  onClose,
}: {
  error: unknown;
  onClose: () => void;
}) {
  return (
    <CatalogModal
      title="Не удалось загрузить запись"
      error={error}
      fields={[]}
      onClose={onClose}
    >
      {null}
    </CatalogModal>
  );
}

/** The dialog that edits a draft entry, which is the entry's own dialog of the career model section. */
export function EditDraftModal({
  draft,
  onSaved,
  onClose,
}: EditDraftModalProps) {
  const { kind } = draft;
  if (kind === "Competency") {
    return <EditCompetency draft={draft} onSaved={onSaved} onClose={onClose} />;
  }
  if (kind === "Scale") {
    return <EditScale draft={draft} onSaved={onSaved} onClose={onClose} />;
  }
  if (kind === "Track") {
    return <EditTrack draft={draft} onSaved={onSaved} onClose={onClose} />;
  }
  if (kind === "Block") {
    return <EditBlock draft={draft} onSaved={onSaved} onClose={onClose} />;
  }
  return <EditNamed draft={draft} onSaved={onSaved} onClose={onClose} />;
}

function EditNamed({ draft, onSaved, onClose }: EditDraftModalProps) {
  const segment = SEGMENT_OF_KIND[draft.kind];
  const read = useQuery(entryQuery(segment, draft.id));
  if (read.error) return <ReadFailure error={read.error} onClose={onClose} />;
  if (!read.data) return null;
  return (
    <NameModal
      segment={segment === "directions" ? "directions" : "categories"}
      title={
        segment === "directions" ? "Изменение области" : "Изменение категории"
      }
      entry={read.data}
      onSaved={onSaved}
      onClose={onClose}
    />
  );
}

function EditCompetency({ draft, onSaved, onClose }: EditDraftModalProps) {
  const read = useQuery(competencyQuery(draft.id));
  if (read.error) return <ReadFailure error={read.error} onClose={onClose} />;
  if (!read.data) return null;
  return (
    <CompetencyModal
      competency={read.data}
      onSaved={onSaved}
      onClose={onClose}
    />
  );
}

function EditScale({ draft, onSaved, onClose }: EditDraftModalProps) {
  const read = useQuery(scaleQuery(draft.id));
  if (read.error) return <ReadFailure error={read.error} onClose={onClose} />;
  if (!read.data) return null;
  return <ScaleModal scale={read.data} onSaved={onSaved} onClose={onClose} />;
}

function EditBlock({ draft, onSaved, onClose }: EditDraftModalProps) {
  const read = useQuery(blockQuery(draft.id));
  if (read.error) return <ReadFailure error={read.error} onClose={onClose} />;
  if (!read.data) return null;
  return <BlockModal block={read.data} onSaved={onSaved} onClose={onClose} />;
}

function EditTrack({ draft, onSaved, onClose }: EditDraftModalProps) {
  const read = useQuery(trackQuery(draft.id));
  if (read.error) return <ReadFailure error={read.error} onClose={onClose} />;
  if (!read.data) return null;
  return (
    <TrackModal
      directionId={read.data.directionId}
      track={read.data}
      onSaved={onSaved}
      onClose={onClose}
    />
  );
}
