import React, { useEffect } from "react";
import { useTranslation } from "react-i18next";
import { inject, observer } from "mobx-react";

import Section from "@docspace/common/components/Section";

import SectionBodyContent from "./Section/Body/index";
import SectionHeaderContent from "./Section/Header/index";

const NotificationComponent = (props) => {
  const { history, setSelectedNode } = props;
  const { t, ready } = useTranslation("Notifications");

  useEffect(() => {
    setSelectedNode(["accounts"]);
  }, []);

  return (
    <Section>
      <Section.SectionHeader>
        <SectionHeaderContent history={history} t={t} />
      </Section.SectionHeader>

      <Section.SectionBody>
        <SectionBodyContent t={t} ready={ready} />
      </Section.SectionBody>
    </Section>
  );
};

export default inject(({ treeFoldersStore }) => {
  const { setSelectedNode } = treeFoldersStore;

  return {
    setSelectedNode,
  };
})(observer(NotificationComponent));
