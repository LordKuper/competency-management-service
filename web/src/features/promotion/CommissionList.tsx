import { Flex, Menu, Tag, Typography } from "antd";
import type { CommissionSummary } from "./promotionApi";

interface CommissionListProps {
  commissions: readonly CommissionSummary[];
  /** The unit whose commission is open. */
  selectedId?: string;
  onSelect: (unitId: string) => void;
}

/** The units whose commissions the viewer manages, each with the number of members and a note when it has no chair. */
export function CommissionList({
  commissions,
  selectedId,
  onSelect,
}: CommissionListProps) {
  return (
    <Menu
      mode="inline"
      aria-label="Подразделения"
      selectedKeys={selectedId ? [selectedId] : []}
      onClick={({ key }) => onSelect(key)}
      items={commissions.map((commission) => ({
        key: commission.unitId,
        label: (
          <Flex justify="space-between" align="center" gap="small">
            <span>{commission.unitName}</span>
            {commission.hasChair ? (
              <Typography.Text type="secondary">
                {commission.memberCount}
              </Typography.Text>
            ) : (
              <Tag color="processing">Нет председателя</Tag>
            )}
          </Flex>
        ),
      }))}
    />
  );
}
