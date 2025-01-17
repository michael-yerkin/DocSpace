﻿import InfoReactSvgUrl from "PUBLIC_DIR/images/info.react.svg?url";
import React from "react";
import Text from "@docspace/components/text";
import HelpButton from "@docspace/components/help-button";
import Link from "@docspace/components/link";
import { Base } from "@docspace/components/themes";
import { StyledCategoryWrapper, StyledTooltip } from "../StyledSecurity";

const CategoryWrapper = (props) => {
  const {
    t,
    title,
    tooltipTitle,
    tooltipUrl,
    theme,
    currentColorScheme,
  } = props;

  const tooltip = () => (
    <StyledTooltip>
      <Text className={tooltipUrl ? "subtitle" : ""} fontSize="12px">
        {tooltipTitle}
      </Text>
      {tooltipUrl && (
        <Link
          fontSize="12px"
          target="_blank"
          isHovered
          href={tooltipUrl}
          color={currentColorScheme.main.accent}
        >
          {t("Common:LearnMore")}
        </Link>
      )}
    </StyledTooltip>
  );

  return (
    <StyledCategoryWrapper>
      <Text fontSize="16px" fontWeight="700">
        {title}
      </Text>
      <HelpButton
        iconName={InfoReactSvgUrl}
        displayType="dropdown"
        place="right"
        offsetRight={0}
        getContent={tooltip}
        tooltipColor={theme.client.settings.security.owner.tooltipColor}
      />
    </StyledCategoryWrapper>
  );
};

CategoryWrapper.defaultProps = {
  theme: Base,
};

export default CategoryWrapper;
