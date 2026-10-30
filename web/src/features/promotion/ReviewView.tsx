import { ChairReviewView } from "./ChairReviewView";
import { MemberReviewView } from "./MemberReviewView";
import type { RankingItem, Review, ReviewInput } from "./promotionApi";
import { STAGE } from "./promotionTexts";

const EDITABLE_STATUSES: readonly Review["status"][] = [
  "UnderReview",
  "Decided",
];

interface ReviewViewProps {
  review: Review;
  /** The candidates of the unit's commission in the order of the ranking. */
  ranking: readonly RankingItem[];
  isRankingLoading: boolean;
  /** Saves the final scores and the decision; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (input: ReviewInput) => Promise<unknown>;
  /** Opens the card of another candidate of the ranking. */
  onOpen: (applicationId: string) => void;
}

/** The card of a candidate for the commission: the chair, while the scores can still change, gets the form for the final scores and the decision; every other reader gets the card under the ranking. */
export function ReviewView({
  review,
  ranking,
  isRankingLoading,
  onSubmit,
  onOpen,
}: ReviewViewProps) {
  const isChair = review.roles.includes("Chair");
  const canEdit =
    isChair &&
    review.stage <= STAGE.review &&
    EDITABLE_STATUSES.includes(review.status);
  return canEdit ? (
    <ChairReviewView
      review={review}
      ranking={ranking}
      onSubmit={onSubmit}
      onOpen={onOpen}
    />
  ) : (
    <MemberReviewView
      review={review}
      ranking={ranking}
      isRankingLoading={isRankingLoading}
      isChair={isChair}
    />
  );
}
