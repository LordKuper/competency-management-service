import { Button, Flex, Tag, Typography } from "antd";
import type { CommissionSummary } from "./promotionApi";

interface CommissionListProps {
  commissions: readonly CommissionSummary[];
  /** The unit whose commission is open. */
  selectedId?: string;
  onSelect: (unitId: string) => void;
}

/** The units whose commissions the viewer manages, each with the number of members and a note when it has no chair; a long name wraps and the note stays beside it. */
export function CommissionList({
  commissions,
  selectedId,
  onSelect,
}: CommissionListProps) {
  return (
    <Flex
      component="nav"
      data-tour="commission-list"
      aria-label="Подразделения"
      vertical
      gap="small"
    >
      {commissions.map((commission) => {
        const isSelected = commission.unitId === selectedId;
        return (
          <Button
            key={commission.unitId}
            block
            color={isSelected ? "primary" : "default"}
            variant={isSelected ? "filled" : "text"}
            aria-current={isSelected ? "true" : undefined}
            style={{
              height: "auto",
              whiteSpace: "normal",
              textAlign: "start",
              justifyContent: "flex-start",
            }}
            onClick={() => onSelect(commission.unitId)}
          >
            <Flex
              justify="space-between"
              align="center"
              gap="small"
              style={{ width: "100%" }}
            >
              <span style={{ flex: "1 1 auto", minWidth: 0 }}>
                {commission.unitName}
              </span>
              {commission.hasChair ? (
                <Typography.Text type="secondary" style={{ flex: "none" }}>
                  {commission.memberCount}
                </Typography.Text>
              ) : (
                <Tag color="processing" style={{ flex: "none", margin: 0 }}>
                  Нет председателя
                </Tag>
              )}
            </Flex>
          </Button>
        );
      })}
    </Flex>
  );
}
